using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Scalar.AspNetCore;
using Serilog;
using WeighingMS.Simulators.Host.Common;
using WeighingMS.Simulators.Host.Faults;
using WeighingMS.Simulators.Host.OrgaTex;
using WeighingMS.Simulators.Host.Printer;
using WeighingMS.Simulators.Host.Scales;
using WeighingMS.Simulators.Host.Scanner;
using WeighingMS.Simulators.Host.Seed;

// WeighingMS.Simulators.Host: training substitutes for OrgaTEX and the station devices (ADR-003).
// It never references WeighingMS.Domain/Application/Infrastructure: the dummies stay separate from business logic.
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSerilog((_, logger) => logger
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console());

builder.Services.AddOptions<SimulatorOptions>()
    .Bind(builder.Configuration.GetSection(SimulatorOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton(sp =>
{
    var options = sp.GetRequiredService<IOptions<SimulatorOptions>>().Value;
    var env = sp.GetRequiredService<IWebHostEnvironment>();
    return SeedLoader.Load(Path.Combine(env.ContentRootPath, options.SeedPath));
});
builder.Services.AddSingleton<ConsumptionStore>();
builder.Services.AddSingleton<ScaleSimulator>();
builder.Services.AddSingleton<ScanBuffer>();
builder.Services.AddSingleton<PrintStore>();
builder.Services.AddSingleton<FaultRegistry>();

var simulationControlEnabled = builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("Test");
builder.Services
    .AddControllers()
    .ConfigureApplicationPartManager(m => m.FeatureProviders.Add(new SimulationControlFeatureProvider(simulationControlEnabled)))
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)));
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

var app = builder.Build();

// Load and validate the seed at start-up: an invalid seed stops the host.
_ = app.Services.GetRequiredService<SimulatorSeed>();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

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
