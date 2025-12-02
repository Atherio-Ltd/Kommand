using System.Diagnostics;
using Kommand;
using Kommand.Abstractions;
using Microsoft.Extensions.Logging;

namespace Kommand.Sample.Api.Interceptors;

/// <summary>
/// Custom interceptor that logs request execution with timing information.
/// Demonstrates the interceptor pattern for cross-cutting concerns.
/// </summary>
/// <remarks>
/// Interceptors wrap handler execution and can:
/// - Execute logic before the handler (pre-processing)
/// - Execute logic after the handler (post-processing)
/// - Modify or replace the response
/// - Short-circuit execution (skip the handler)
/// - Handle exceptions from handlers
/// </remarks>
public class LoggingInterceptor<TRequest, TResponse> : IInterceptor<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingInterceptor<TRequest, TResponse>> _logger;

    public LoggingInterceptor(ILogger<LoggingInterceptor<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var requestType = request is ICommand ? "Command" : "Query";
        var requestId = Guid.NewGuid().ToString("N")[..8]; // Short ID for correlation

        // PRE-PROCESSING: Log before handler execution
        _logger.LogInformation(
            "[{RequestId}] --> Executing {RequestType}: {RequestName}",
            requestId,
            requestType,
            requestName);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            // CALL NEXT: Invoke the next interceptor or handler in the pipeline
            var response = await next();
            stopwatch.Stop();

            // POST-PROCESSING: Log successful completion
            _logger.LogInformation(
                "[{RequestId}] <-- {RequestType} {RequestName} completed in {Duration}ms",
                requestId,
                requestType,
                requestName,
                stopwatch.ElapsedMilliseconds);

            return response;
        }
        catch (ValidationException ex)
        {
            stopwatch.Stop();

            // Log validation failures at warning level
            _logger.LogWarning(
                "[{RequestId}] <-- {RequestType} {RequestName} validation failed after {Duration}ms: {ErrorCount} error(s)",
                requestId,
                requestType,
                requestName,
                stopwatch.ElapsedMilliseconds,
                ex.Errors.Count);

            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            // Log errors at error level
            _logger.LogError(
                ex,
                "[{RequestId}] <-- {RequestType} {RequestName} failed after {Duration}ms: {ErrorMessage}",
                requestId,
                requestType,
                requestName,
                stopwatch.ElapsedMilliseconds,
                ex.Message);

            throw;
        }
    }
}
