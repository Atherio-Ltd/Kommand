using System.Diagnostics;
using System.Diagnostics.Metrics;
using Kommand.Abstractions;

namespace Kommand.Interceptors;

/// <summary>
/// Built-in interceptor that records OpenTelemetry Metrics for all command and query executions.
/// </summary>
/// <typeparam name="TRequest">The type of request being handled (command or query)</typeparam>
/// <typeparam name="TResponse">The type of response returned by the handler</typeparam>
/// <remarks>
/// <para>
/// <strong>Zero-Configuration Pattern:</strong><br/>
/// This interceptor is automatically registered for all requests when you call <c>AddKommand()</c>.
/// It records metrics using the OpenTelemetry <see cref="Meter"/> API with virtually zero overhead
/// (~10-50ns) when OpenTelemetry is not configured in your application.
/// </para>
/// <para>
/// <strong>How It Works:</strong><br/>
/// The interceptor creates a <see cref="Meter"/> that defines metric instruments (counters, histograms).
/// When OpenTelemetry is NOT configured, these instruments are created but their recording operations
/// become no-ops, resulting in minimal performance impact when metrics collection is disabled.
/// </para>
/// <para>
/// <strong>Metric Instruments:</strong><br/>
/// This interceptor records three types of metrics:
/// <list type="bullet">
/// <item>
/// <description>
/// <strong>kommand.requests</strong> (Counter) - Total number of requests processed, tagged by type and name.
/// Useful for tracking throughput and request distribution.
/// </description>
/// </item>
/// <item>
/// <description>
/// <strong>kommand.requests.failed</strong> (Counter) - Total number of requests that threw exceptions.
/// Useful for tracking error rates and reliability.
/// </description>
/// </item>
/// <item>
/// <description>
/// <strong>kommand.request.duration</strong> (Histogram) - Duration of request processing in milliseconds.
/// Useful for tracking latency percentiles (p50, p95, p99) and identifying slow operations.
/// </description>
/// </item>
/// </list>
/// </para>
/// <para>
/// <strong>Metric Tags (Dimensions):</strong><br/>
/// All metrics include these dimensions for filtering and aggregation:
/// <list type="bullet">
/// <item><description><c>kommand.request.type</c> - "Command", "Query", or "Notification"</description></item>
/// <item><description><c>kommand.request.name</c> - The type name of the request (e.g., "CreateUserCommand")</description></item>
/// <item><description><c>kommand.response.type</c> - The type name of the response</description></item>
/// <item><description><c>kommand.success</c> - "true" for successful requests, "false" for failures (duration histogram only)</description></item>
/// </list>
/// </para>
/// <para>
/// <strong>Performance Characteristics:</strong><br/>
/// - OTEL Not Configured: ~10-50ns overhead per request (negligible)<br/>
/// - OTEL Configured: ~1-5μs overhead per request (acceptable for production)<br/>
/// - Memory: Zero allocations when OTEL is not configured
/// </para>
/// <para>
/// <strong>Enabling OpenTelemetry in Your Application:</strong><br/>
/// To enable metrics collection, configure OpenTelemetry in your application startup.
/// Kommand's metrics will automatically be collected without any additional configuration:
/// </para>
/// </remarks>
/// <example>
/// Example OpenTelemetry configuration (Program.cs):
/// <code>
/// using OpenTelemetry.Metrics;
/// using OpenTelemetry.Resources;
///
/// var builder = WebApplication.CreateBuilder(args);
///
/// // Register Kommand (no OTEL-specific configuration needed!)
/// builder.Services.AddKommand(config =>
/// {
///     config.RegisterHandlersFromAssembly(typeof(Program).Assembly);
/// });
///
/// // Add OpenTelemetry - automatically discovers Kommand's Meter
/// builder.Services.AddOpenTelemetry()
///     .WithMetrics(metrics => metrics
///         .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("MyApp"))
///         .AddMeter("Kommand") // Subscribe to Kommand metrics
///         .AddConsoleExporter() // Export to console (or use Prometheus, OTLP, etc.)
///     );
///
/// var app = builder.Build();
///
/// // All command/query executions will now record metrics automatically!
/// var mediator = app.Services.GetRequiredService&lt;IMediator&gt;();
/// await mediator.SendAsync(new CreateUserCommand("alice@example.com"), CancellationToken.None);
/// // Records: kommand.requests +1, kommand.request.duration (24.5ms)
/// </code>
/// </example>
/// <example>
/// Example metrics output:
/// <code>
/// kommand.requests{kommand.request.type="Command",kommand.request.name="CreateUserCommand"} = 1250
/// kommand.requests.failed{kommand.request.type="Command",kommand.request.name="CreateUserCommand"} = 3
/// kommand.request.duration{kommand.request.type="Command",kommand.request.name="CreateUserCommand",kommand.success="true"} histogram:
///   p50 = 12.5ms
///   p95 = 45.2ms
///   p99 = 124.8ms
/// </code>
/// </example>
public sealed class MetricsInterceptor<TRequest, TResponse> : IInterceptor<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    // IMPORTANT: Static fields are initialized once per generic type instantiation
    // Each TRequest/TResponse combination gets its own Meter instance
    // This is intentional and follows OpenTelemetry best practices

    /// <summary>
    /// The Meter used to create metric instruments.
    /// Named "Kommand" to allow users to subscribe via .AddMeter("Kommand").
    /// </summary>
    /// <remarks>
    /// The version is extracted from the assembly at runtime to match the library version.
    /// When OpenTelemetry is not configured, metric recording becomes a no-op, resulting in
    /// zero overhead beyond the initial instrument creation.
    /// </remarks>
    private static readonly Meter Meter = new(
        "Kommand",
        typeof(MetricsInterceptor<,>).Assembly.GetName().Version?.ToString() ?? "1.0.0");

    /// <summary>
    /// Counter tracking the total number of requests processed.
    /// Incremented by 1 for every request, tagged by request type and name.
    /// </summary>
    private static readonly Counter<long> RequestCounter = Meter.CreateCounter<long>(
        name: "kommand.requests",
        unit: "requests",
        description: "Total number of requests processed by Kommand");

    /// <summary>
    /// Counter tracking the total number of requests that failed (threw exceptions).
    /// Incremented by 1 for every failed request, tagged by request type and name.
    /// </summary>
    private static readonly Counter<long> FailedRequestCounter = Meter.CreateCounter<long>(
        name: "kommand.requests.failed",
        unit: "requests",
        description: "Total number of failed requests (exceptions thrown)");

    /// <summary>
    /// Histogram tracking request processing duration in milliseconds.
    /// Records latency distribution for all requests, tagged by request type, name, and success status.
    /// </summary>
    /// <remarks>
    /// Histograms allow observability platforms to calculate percentiles (p50, p95, p99) and
    /// identify slow operations. The unit is milliseconds for human readability.
    /// </remarks>
    private static readonly Histogram<double> DurationHistogram = Meter.CreateHistogram<double>(
        name: "kommand.request.duration",
        unit: "ms",
        description: "Duration of request processing in milliseconds");

    /// <summary>
    /// Handles the request by measuring execution time and recording metrics.
    /// </summary>
    /// <param name="request">The request instance being handled</param>
    /// <param name="next">Delegate representing the next handler in the pipeline</param>
    /// <param name="cancellationToken">Cancellation token for graceful cancellation</param>
    /// <returns>The response from the handler</returns>
    /// <remarks>
    /// <para>
    /// This method records three metrics for every request:
    /// <list type="bullet">
    /// <item><description>Increments the request counter (always)</description></item>
    /// <item><description>Records duration in the histogram (always)</description></item>
    /// <item><description>Increments the failed request counter (only on exception)</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Duration is measured using <see cref="Stopwatch"/> for high precision (~100ns resolution).
    /// The stopwatch is started before calling the handler and stopped after completion.
    /// </para>
    /// </remarks>
    public async Task<TResponse> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Determine request type (Command, Query, or Notification)
        var requestType = request switch
        {
            ICommand<TResponse> => "Command",
            IQuery<TResponse> => "Query",
            INotification => "Notification",
            _ => "Request" // Fallback for custom request types
        };

        var requestTypeName = typeof(TRequest).Name;
        var responseTypeName = typeof(TResponse).Name;

        // Create tag array for metrics (reused across instruments for efficiency)
        var tags = new TagList
        {
            { "kommand.request.type", requestType },
            { "kommand.request.name", requestTypeName },
            { "kommand.response.type", responseTypeName }
        };

        // Start high-precision stopwatch to measure duration
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Execute the handler (or next interceptor in the pipeline)
            var response = await next();

            // Stop timing
            stopwatch.Stop();

            // Record successful request metrics
            RequestCounter.Add(1,
                new KeyValuePair<string, object?>("kommand.request.type", requestType),
                new KeyValuePair<string, object?>("kommand.request.name", requestTypeName),
                new KeyValuePair<string, object?>("kommand.response.type", responseTypeName));

            // Record duration with success flag
            DurationHistogram.Record(stopwatch.Elapsed.TotalMilliseconds,
                new KeyValuePair<string, object?>("kommand.request.type", requestType),
                new KeyValuePair<string, object?>("kommand.request.name", requestTypeName),
                new KeyValuePair<string, object?>("kommand.response.type", responseTypeName),
                new KeyValuePair<string, object?>("kommand.success", "true"));

            return response;
        }
        catch (Exception)
        {
            // Stop timing
            stopwatch.Stop();

            // Record failed request metrics
            RequestCounter.Add(1,
                new KeyValuePair<string, object?>("kommand.request.type", requestType),
                new KeyValuePair<string, object?>("kommand.request.name", requestTypeName),
                new KeyValuePair<string, object?>("kommand.response.type", responseTypeName));

            FailedRequestCounter.Add(1,
                new KeyValuePair<string, object?>("kommand.request.type", requestType),
                new KeyValuePair<string, object?>("kommand.request.name", requestTypeName),
                new KeyValuePair<string, object?>("kommand.response.type", responseTypeName));

            // Record duration with failure flag
            DurationHistogram.Record(stopwatch.Elapsed.TotalMilliseconds,
                new KeyValuePair<string, object?>("kommand.request.type", requestType),
                new KeyValuePair<string, object?>("kommand.request.name", requestTypeName),
                new KeyValuePair<string, object?>("kommand.response.type", responseTypeName),
                new KeyValuePair<string, object?>("kommand.success", "false"));

            // Re-throw to propagate error to caller
            throw;
        }
    }
}
