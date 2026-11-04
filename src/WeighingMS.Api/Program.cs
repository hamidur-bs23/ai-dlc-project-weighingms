using Scalar.AspNetCore;
using Serilog;

// WeighingMS.Api: the only service the UI calls. Business endpoints arrive with bolts 003+;
// this bolt only scaffolds the host (health, OpenAPI, ProblemDetails, Serilog).
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSerilog((_, logger) => logger
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console());

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapHealthChecks("/health/live");
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
