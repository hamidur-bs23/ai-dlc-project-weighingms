using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Scalar.AspNetCore;
using Serilog;
using WeighingMS.Api.Common;
using WeighingMS.Application;
using WeighingMS.Application.Access;
using WeighingMS.Application.Batches;
using WeighingMS.Application.Devices;
using WeighingMS.Application.Weighing;
using WeighingMS.Application.WriteBack;
using WeighingMS.Infrastructure;

// WeighingMS.Api: the only service the UI calls (B0). Cookie auth + antiforgery + rate limiting (ADR-009),
// ErrorOr → ProblemDetails, PostgreSQL via EF Core. Migrations are applied outside start-up (data-stack).
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSerilog((_, logger) => logger
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console());

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddOptions<StationAccessOptions>()
    .Bind(builder.Configuration.GetSection(StationAccessOptions.SectionName))
    .Validate(o => o.MaxFailedPinAttempts > 0 && o.LockoutDuration > TimeSpan.Zero && o.StepUpGrantLifetime > TimeSpan.Zero, "Station access limits must be positive.")
    .Validate(o => !string.IsNullOrWhiteSpace(o.StationId) && o.StepUpActions.Count > 0, "Station:StationId and Station:StepUpActions are required.")
    .ValidateOnStart();
builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<StationAccessOptions>>().Value);

builder.Services.AddOptions<DevicesOptions>()
    .Bind(builder.Configuration.GetSection(DevicesOptions.SectionName))
    .Validate(o => o.ScalePollInterval >= TimeSpan.FromMilliseconds(50), "Devices:ScalePollInterval must be at least 50 ms.")
    .ValidateOnStart();
builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<DevicesOptions>>().Value);

builder.Services.AddOptions<ToleranceSettings>()
    .Bind(builder.Configuration.GetSection(ToleranceSettings.SectionName))
    .Validate(o => o.Validate().Count == 0, "Tolerances: each entry needs a chemicalId, a positive plusMinus, unit g or kg, scaleClass precision or heavyDuty, and no duplicates.")
    .ValidateOnStart();

builder.Services.AddOptions<WriteBackOptions>()
    .Bind(builder.Configuration.GetSection(WriteBackOptions.SectionName))
    .Validate(o => o.Validate().Count == 0, "WriteBack: MaxAttempts >= 1, BaseDelay > 0, MaxDelay >= BaseDelay and PollInterval > 0 are required.")
    .ValidateOnStart();
builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<WriteBackOptions>>().Value);

builder.Services.AddOptions<ScaleSelectionOptions>()
    .Bind(builder.Configuration.GetSection(ScaleSelectionOptions.SectionName))
    .Validate(o => o.ThresholdKg > 0m, "Weighing:ScaleSelection:ThresholdKg must be greater than zero.")
    .Validate(o => !string.IsNullOrWhiteSpace(o.Assignments.Precision) && !string.IsNullOrWhiteSpace(o.Assignments.HeavyDuty), "Weighing:ScaleSelection:Assignments needs a Precision and a HeavyDuty scale.")
    .ValidateOnStart();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Cookies are Secure with the __Host- prefix everywhere except local Development, which runs over plain HTTP
// (http://localhost:5080). Test and Production require HTTPS.
var secureCookies = !builder.Environment.IsDevelopment();
var cookiePolicy = secureCookies ? CookieSecurePolicy.Always : CookieSecurePolicy.SameAsRequest;
var cookiePrefix = secureCookies ? "__Host-" : string.Empty;

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.Cookie.Name = cookiePrefix + "wms.session";
        o.Cookie.HttpOnly = true;
        o.Cookie.SecurePolicy = cookiePolicy;
        o.Cookie.SameSite = SameSiteMode.Strict;
        o.SlidingExpiration = true;
        o.ExpireTimeSpan = TimeSpan.FromHours(12);
        o.Events.OnRedirectToLogin = ctx => ProblemResponses.WriteAsync(ctx.HttpContext, StatusCodes.Status401Unauthorized, "Auth.SignInRequired", "Sign in with your badge first.");
        o.Events.OnRedirectToAccessDenied = ctx => ProblemResponses.WriteAsync(ctx.HttpContext, StatusCodes.Status403Forbidden, "Auth.SupervisorRequired", "A supervisor is required.");
    });
builder.Services.AddAuthorizationBuilder()
    .AddPolicy(StationPolicies.RequireOperator, p => p.RequireAuthenticatedUser().RequireClaim(StationClaims.Badge))
    .AddPolicy(StationPolicies.RequireSupervisor, p => p.RequireAuthenticatedUser().RequireRole("Supervisor"));

builder.Services.AddAntiforgery(o =>
{
    o.HeaderName = "X-XSRF-TOKEN";
    o.Cookie.Name = cookiePrefix + "wms.af";
    o.Cookie.SecurePolicy = cookiePolicy;
    o.Cookie.SameSite = SameSiteMode.Strict;
});

var authPermitLimit = builder.Configuration.GetValue("RateLimits:AuthPermitLimitPerMinute", 10);
builder.Services.AddRateLimiter(o =>
{
    o.AddPolicy(StationPolicies.AuthRateLimit, ctx => RateLimitPartition.GetFixedWindowLimiter(
        ctx.Connection.RemoteIpAddress?.ToString() ?? "local",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = authPermitLimit, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    o.OnRejected = (ctx, _) => new ValueTask(ProblemResponses.WriteAsync(ctx.HttpContext, StatusCodes.Status429TooManyRequests, "Auth.TooManyAttempts", "Too many attempts; wait a minute."));
});

builder.Services
    .AddControllers(o => o.Filters.Add<AntiforgeryValidationFilter>())
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));

        // Numbers are numbers on the wire: no quoted numbers in or out, so the OpenAPI document (and the
        // generated Angular types) describe them as plain numbers.
        o.JsonSerializerOptions.NumberHandling = JsonNumberHandling.Strict;
    });
builder.Services.AddProblemDetails(o => o.CustomizeProblemDetails = ctx =>
    ctx.ProblemDetails.Extensions.TryAdd("traceId", TraceIds.Of(ctx.HttpContext)));

// The OpenAPI generator reads the minimal-API JSON options, so they carry the same strict number rule.
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.NumberHandling = JsonNumberHandling.Strict);
builder.Services.AddOpenApi();
builder.Services.AddWeighingTelemetry(builder.Configuration);
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("Db")!, name: "postgres", tags: ["ready"]);

var app = builder.Build();

if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Test"))
{
    await DevelopmentSeeding.SeedUsersAsync(app);
}

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseSerilogRequestLogging(o => o.GetLevel = RequestLogLevels.For);
app.Use(async (ctx, next) =>
{
    if (ctx.Request.Path.StartsWithSegments("/api", StringComparison.Ordinal))
    {
        ctx.Response.OnStarting(() =>
        {
            ctx.Response.Headers.CacheControl = "no-store";
            return Task.CompletedTask;
        });
    }

    await next();
});

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapHealthChecks("/health/live", new() { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new() { Predicate = c => c.Tags.Contains("ready") });
app.MapControllers();
await app.RunAsync();

/// <summary>Entry point; public so integration tests can host it with WebApplicationFactory.</summary>
public partial class Program
{
    /// <summary>Not instantiated; exists for <c>WebApplicationFactory&lt;Program&gt;</c>.</summary>
    protected Program()
    {
    }
}
