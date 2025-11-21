namespace Kommand.Sample.Interceptors;

using Kommand;
using Kommand.Abstractions;
using Microsoft.Extensions.Logging;

/// <summary>
/// Example interceptor that logs request execution with timing information.
/// This demonstrates the basic pattern for implementing custom interceptors.
/// </summary>
/// <typeparam name="TRequest">The type of request being handled</typeparam>
/// <typeparam name="TResponse">The type of response returned by the handler</typeparam>
/// <remarks>
/// <para>
/// This interceptor demonstrates several important concepts:
/// <list type="bullet">
/// <item><description>How to inject dependencies (ILogger) into interceptors</description></item>
/// <item><description>How to execute logic before handler execution (pre-processing)</description></item>
/// <item><description>How to call the next handler in the pipeline using <c>await next()</c></description></item>
/// <item><description>How to execute logic after handler execution (post-processing)</description></item>
/// <item><description>How to handle exceptions from handlers</description></item>
/// <item><description>How to measure execution duration</description></item>
/// </list>
/// </para>
/// <para>
/// <strong>Registration:</strong><br/>
/// To use this interceptor, register it when configuring Kommand:
/// <code>
/// services.AddKommand(config =>
/// {
///     config.RegisterHandlersFromAssembly(typeof(Program).Assembly);
///     config.AddInterceptor&lt;LoggingInterceptor&lt;,&gt;&gt;(); // Register as open generic
/// });
/// </code>
/// </para>
/// <para>
/// <strong>Execution Order:</strong><br/>
/// If multiple interceptors are registered, they execute in registration order.<br/>
/// First registered = outermost (executes first on entry, last on exit).<br/>
/// Last registered = innermost (executes last on entry, first on exit, closest to handler).
/// </para>
/// </remarks>
public class LoggingInterceptor<TRequest, TResponse> : IInterceptor<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingInterceptor<TRequest, TResponse>> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="LoggingInterceptor{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="logger">
    /// Logger instance for writing log messages.
    /// Injected by the DI container when the interceptor is resolved.
    /// </param>
    /// <remarks>
    /// Interceptors can inject any services registered in the DI container, just like handlers.
    /// Common dependencies include:
    /// <list type="bullet">
    /// <item><description>ILogger - For logging</description></item>
    /// <item><description>IMetricsService - For recording metrics</description></item>
    /// <item><description>ICurrentUserService - For accessing user context</description></item>
    /// <item><description>DbContext - For database operations (use Scoped lifetime)</description></item>
    /// </list>
    /// </remarks>
    public LoggingInterceptor(ILogger<LoggingInterceptor<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Handles the request by logging before and after handler execution.
    /// </summary>
    /// <param name="request">The request instance being handled</param>
    /// <param name="next">
    /// Delegate representing the next handler in the pipeline.
    /// Call <c>await next()</c> to continue execution to the next interceptor or handler.
    /// </param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>The response from the handler</returns>
    public async Task<TResponse> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Get request type name for logging
        var requestName = typeof(TRequest).Name;

        // Determine if this is a command or query for better log messages
        var requestType = request is ICommand ? "Command" : "Query";

        // Record start time to measure execution duration
        var startTime = DateTime.UtcNow;

        // PRE-PROCESSING: Log before handler execution
        _logger.LogInformation(
            "Executing {RequestType}: {RequestName}",
            requestType,
            requestName);

        try
        {
            // CALL NEXT: Invoke the next interceptor or handler in the pipeline
            // This is the critical part - calling next() continues the pipeline
            var response = await next();

            // POST-PROCESSING: Log after successful handler execution
            var duration = DateTime.UtcNow - startTime;
            _logger.LogInformation(
                "{RequestType} {RequestName} completed successfully in {Duration}ms",
                requestType,
                requestName,
                duration.TotalMilliseconds);

            // Return the response from the handler
            return response;
        }
        catch (Exception ex)
        {
            // EXCEPTION HANDLING: Log when handler throws an exception
            var duration = DateTime.UtcNow - startTime;
            _logger.LogError(
                ex,
                "{RequestType} {RequestName} failed after {Duration}ms: {ErrorMessage}",
                requestType,
                requestName,
                duration.TotalMilliseconds,
                ex.Message);

            // Re-throw the exception so it propagates to the caller
            // If you DON'T re-throw, the exception will be swallowed and the caller won't know about the failure
            throw;
        }
    }
}

/*
 * USAGE EXAMPLES:
 *
 * Example 1: Basic registration
 * -------------------------------
 * services.AddKommand(config =>
 * {
 *     config.RegisterHandlersFromAssembly(typeof(Program).Assembly);
 *     config.AddInterceptor<LoggingInterceptor<,>>(); // Open generic registration
 * });
 *
 *
 * Example 2: Multiple interceptors with execution order
 * -------------------------------------------------------
 * services.AddKommand(config =>
 * {
 *     config.RegisterHandlersFromAssembly(typeof(Program).Assembly);
 *     config.AddInterceptor<LoggingInterceptor<,>>();      // Executes FIRST (outermost)
 *     config.AddInterceptor<ValidationInterceptor<,>>();   // Executes SECOND
 *     config.AddInterceptor<MetricsInterceptor<,>>();      // Executes THIRD (innermost, closest to handler)
 * });
 *
 * Execution flow:
 * → LoggingInterceptor (enter) ----+
 *   → ValidationInterceptor (enter) |
 *     → MetricsInterceptor (enter)  |
 *       → Handler executes          | All wrapped in try-catch of LoggingInterceptor
 *     ← MetricsInterceptor (exit)   |
 *   ← ValidationInterceptor (exit)  |
 * ← LoggingInterceptor (exit) -----+
 *
 *
 * Example 3: Short-circuiting (not calling next)
 * -----------------------------------------------
 * public class AuthorizationInterceptor<TRequest, TResponse> : IInterceptor<TRequest, TResponse>
 *     where TRequest : IRequest<TResponse>
 * {
 *     private readonly IAuthService _authService;
 *
 *     public async Task<TResponse> HandleAsync(
 *         TRequest request,
 *         RequestHandlerDelegate<TResponse> next,
 *         CancellationToken cancellationToken)
 *     {
 *         // Check authorization BEFORE calling next()
 *         if (!await _authService.IsAuthorizedAsync(request))
 *         {
 *             throw new UnauthorizedException();
 *             // next() is NEVER called - handler doesn't execute
 *         }
 *
 *         // Authorized - continue to handler
 *         return await next();
 *     }
 * }
 *
 *
 * Example 4: Caching interceptor (short-circuits on cache hit)
 * -------------------------------------------------------------
 * public class CachingInterceptor<TQuery, TResponse> : IQueryInterceptor<TQuery, TResponse>
 *     where TQuery : IQuery<TResponse>
 * {
 *     private readonly ICache _cache;
 *
 *     public async Task<TResponse> HandleAsync(
 *         TQuery query,
 *         RequestHandlerDelegate<TResponse> next,
 *         CancellationToken cancellationToken)
 *     {
 *         var cacheKey = GenerateCacheKey(query);
 *
 *         // Try to get from cache first
 *         var cachedResult = await _cache.GetAsync<TResponse>(cacheKey);
 *         if (cachedResult != null)
 *         {
 *             return cachedResult; // SHORT-CIRCUIT: Don't call next(), return cached value
 *         }
 *
 *         // Cache miss - execute query handler
 *         var response = await next();
 *
 *         // Cache the result for future requests
 *         await _cache.SetAsync(cacheKey, response, TimeSpan.FromMinutes(5));
 *
 *         return response;
 *     }
 * }
 */
