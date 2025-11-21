namespace Kommand;

/// <summary>
/// Represents the next handler in the interceptor pipeline.
/// Invoke this delegate to continue execution to the next interceptor or the final handler.
/// </summary>
/// <typeparam name="TResponse">The type of response returned by the handler</typeparam>
/// <returns>A task representing the asynchronous operation that produces the response</returns>
/// <remarks>
/// <para>
/// This delegate is passed to each interceptor's <c>HandleAsync</c> method and represents
/// the continuation of the pipeline. When an interceptor calls <c>await next()</c>, it
/// invokes the next interceptor in the chain (or the final handler if no more interceptors exist).
/// </para>
/// <para>
/// <strong>Pipeline Execution Order:</strong><br/>
/// If interceptors are registered in order: [Logging, Validation, Metrics]<br/>
/// The execution flows as:
/// <code>
/// → Logging interceptor (enter)
///   → Validation interceptor (enter)
///     → Metrics interceptor (enter)
///       → Handler executes
///     ← Metrics interceptor (exit)
///   ← Validation interceptor (exit)
/// ← Logging interceptor (exit)
/// </code>
/// </para>
/// <para>
/// <strong>Short-Circuiting:</strong><br/>
/// An interceptor can choose not to call <c>next()</c> to prevent further pipeline execution.
/// This is useful for scenarios like authorization, caching, or validation where you want to
/// return early without invoking the handler.
/// </para>
/// <example>
/// Example interceptor that calls next:
/// <code>
/// public async Task&lt;TResponse&gt; HandleAsync(
///     TRequest request,
///     RequestHandlerDelegate&lt;TResponse&gt; next,
///     CancellationToken cancellationToken)
/// {
///     // Pre-processing logic
///     Console.WriteLine("Before handler");
///
///     // Call next interceptor or handler
///     var response = await next();
///
///     // Post-processing logic
///     Console.WriteLine("After handler");
///
///     return response;
/// }
/// </code>
/// Example interceptor that short-circuits:
/// <code>
/// public async Task&lt;TResponse&gt; HandleAsync(
///     TRequest request,
///     RequestHandlerDelegate&lt;TResponse&gt; next,
///     CancellationToken cancellationToken)
/// {
///     // Check authorization
///     if (!_authService.IsAuthorized(request))
///     {
///         throw new UnauthorizedException();
///         // next() is never called - handler doesn't execute
///     }
///
///     // Authorized - continue pipeline
///     return await next();
/// }
/// </code>
/// </example>
/// </remarks>
public delegate Task<TResponse> RequestHandlerDelegate<TResponse>();
