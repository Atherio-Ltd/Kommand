namespace Kommand;

using Kommand.Abstractions;

/// <summary>
/// Interceptor interface for all requests (commands and queries).
/// Implement this interface to add cross-cutting concerns that apply to both commands and queries.
/// </summary>
/// <typeparam name="TRequest">The type of request being handled (must implement IRequest&lt;TResponse&gt;)</typeparam>
/// <typeparam name="TResponse">The type of response returned by the handler</typeparam>
/// <remarks>
/// <para>
/// Interceptors provide a powerful mechanism for implementing cross-cutting concerns such as:
/// <list type="bullet">
/// <item><description><strong>Logging:</strong> Log all requests with execution time and outcomes</description></item>
/// <item><description><strong>Validation:</strong> Validate request data before handler execution</description></item>
/// <item><description><strong>Authorization:</strong> Check user permissions before allowing operations</description></item>
/// <item><description><strong>Caching:</strong> Cache query results to improve performance</description></item>
/// <item><description><strong>Metrics:</strong> Track performance metrics and operation counts</description></item>
/// <item><description><strong>Error Handling:</strong> Wrap exceptions or implement retry logic</description></item>
/// <item><description><strong>Transaction Management:</strong> Begin/commit transactions around handlers</description></item>
/// <item><description><strong>OpenTelemetry:</strong> Create distributed tracing spans automatically</description></item>
/// </list>
/// </para>
/// <para>
/// <strong>Execution Order:</strong><br/>
/// Interceptors execute in the order they are registered via <c>AddInterceptor()</c>.<br/>
/// The first registered interceptor is the outermost (executes first on entry, last on exit).<br/>
/// The last registered interceptor is the innermost (executes last on entry, first on exit).
/// </para>
/// <para>
/// <strong>Registration Example:</strong>
/// <code>
/// services.AddKommand(config =>
/// {
///     config.RegisterHandlersFromAssembly(typeof(Program).Assembly);
///     config.AddInterceptor&lt;LoggingInterceptor&gt;();      // Outermost
///     config.AddInterceptor&lt;ValidationInterceptor&gt;();   // Middle
///     config.AddInterceptor&lt;MetricsInterceptor&gt;();      // Innermost (closest to handler)
/// });
/// </code>
/// </para>
/// <para>
/// <strong>Interceptor Lifetime:</strong><br/>
/// Interceptors are resolved from the DI container for each request and should typically be
/// registered with Scoped lifetime to allow access to request-scoped services like DbContext.
/// </para>
/// <para>
/// <strong>Generic vs Specific Interceptors:</strong><br/>
/// Use <see cref="IInterceptor{TRequest, TResponse}"/> when you want to intercept ALL requests
/// (both commands and queries). Use <see cref="ICommandInterceptor{TCommand, TResponse}"/> or
/// <see cref="IQueryInterceptor{TQuery, TResponse}"/> if you need command-only or query-only
/// interception.
/// </para>
/// </remarks>
/// <example>
/// Example logging interceptor that measures execution time:
/// <code>
/// public class LoggingInterceptor&lt;TRequest, TResponse&gt; : IInterceptor&lt;TRequest, TResponse&gt;
///     where TRequest : IRequest&lt;TResponse&gt;
/// {
///     private readonly ILogger&lt;LoggingInterceptor&lt;TRequest, TResponse&gt;&gt; _logger;
///
///     public LoggingInterceptor(ILogger&lt;LoggingInterceptor&lt;TRequest, TResponse&gt;&gt; logger)
///     {
///         _logger = logger;
///     }
///
///     public async Task&lt;TResponse&gt; HandleAsync(
///         TRequest request,
///         RequestHandlerDelegate&lt;TResponse&gt; next,
///         CancellationToken cancellationToken)
///     {
///         var requestName = typeof(TRequest).Name;
///         var startTime = DateTime.UtcNow;
///
///         _logger.LogInformation("Executing request {RequestName}", requestName);
///
///         try
///         {
///             var response = await next(); // Call next interceptor or handler
///
///             var duration = DateTime.UtcNow - startTime;
///             _logger.LogInformation(
///                 "Request {RequestName} completed in {Duration}ms",
///                 requestName,
///                 duration.TotalMilliseconds);
///
///             return response;
///         }
///         catch (Exception ex)
///         {
///             var duration = DateTime.UtcNow - startTime;
///             _logger.LogError(
///                 ex,
///                 "Request {RequestName} failed after {Duration}ms",
///                 requestName,
///                 duration.TotalMilliseconds);
///             throw;
///         }
///     }
/// }
/// </code>
/// </example>
public interface IInterceptor<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <summary>
    /// Handles the request by executing custom logic before and/or after calling the next handler in the pipeline.
    /// </summary>
    /// <param name="request">The request instance being handled</param>
    /// <param name="next">
    /// Delegate representing the next handler in the pipeline.
    /// Call <c>await next()</c> to continue pipeline execution.
    /// Do not call <c>next()</c> to short-circuit the pipeline (e.g., for caching or authorization).
    /// </param>
    /// <param name="cancellationToken">
    /// Cancellation token that should be observed to allow graceful cancellation of long-running operations.
    /// Pass this token to async methods called within the interceptor.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous operation, containing the response of type <typeparamref name="TResponse"/>.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method is called for every request that matches <typeparamref name="TRequest"/>.
    /// You can execute logic before calling <c>next()</c> (pre-processing) and after
    /// <c>next()</c> returns (post-processing).
    /// </para>
    /// <para>
    /// <strong>Important:</strong> Always await the <c>next()</c> delegate to ensure proper
    /// async flow. Never use <c>.Result</c> or <c>.Wait()</c> as this can cause deadlocks.
    /// </para>
    /// <para>
    /// <strong>Exception Handling:</strong> Exceptions thrown by handlers or downstream interceptors
    /// will propagate through this method. You can catch them for logging or retry logic, but must
    /// re-throw if you want the caller to be aware of the failure.
    /// </para>
    /// </remarks>
    Task<TResponse> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken);
}
