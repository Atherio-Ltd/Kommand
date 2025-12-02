using Kommand;
using Kommand.Sample.Api.Endpoints;
using Kommand.Sample.Api.Infrastructure;
using Kommand.Sample.Api.Interceptors;
using Kommand.Sample.Api.Middleware;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// ============================================================================
// Configure Services
// ============================================================================

// Kommand with all features
builder.Services.AddKommand(config =>
{
    config.RegisterHandlersFromAssembly(typeof(Program).Assembly);
    config.AddInterceptor(typeof(LoggingInterceptor<,>));
    config.WithValidation();
});

// Application services
builder.Services.AddScoped<IUserRepository, InMemoryUserRepository>();
builder.Services.AddScoped<IProductRepository, InMemoryProductRepository>();

// OpenTelemetry
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("Kommand.Sample.Api", "1.0.0"))
    .WithTracing(tracing => tracing
        .AddSource("Kommand")
        .AddAspNetCoreInstrumentation()
        .AddConsoleExporter())
    .WithMetrics(metrics => metrics
        .AddMeter("Kommand")
        .AddAspNetCoreInstrumentation()
        .AddConsoleExporter());

// OpenAPI
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();

var app = builder.Build();

// ============================================================================
// Configure Middleware
// ============================================================================

app.MapOpenApi();
app.MapScalarApiReference(options =>
{
    options.Title = "Kommand Sample API";
    options.Theme = ScalarTheme.BluePlanet;
});
app.UseKommandExceptionHandler();

// ============================================================================
// Map Endpoints
// ============================================================================

app.MapUserEndpoints();
app.MapProductEndpoints();

app.Run();
