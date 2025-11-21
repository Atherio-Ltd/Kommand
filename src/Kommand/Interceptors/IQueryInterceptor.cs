namespace Kommand;

using Kommand.Abstractions;

/// <summary>
/// Interceptor interface specifically for queries (read operations that don't change state).
/// Implement this interface to add cross-cutting concerns that only apply to queries.
/// </summary>
/// <typeparam name="TQuery">The type of query being handled (must implement IQuery&lt;TResponse&gt;)</typeparam>
/// <typeparam name="TResponse">The type of response returned by the query handler</typeparam>
/// <remarks>
/// <para>
/// Use <see cref="IQueryInterceptor{TQuery, TResponse}"/> when you need to intercept
/// only queries (not commands). This is useful for concerns that only apply to read operations:
/// <list type="bullet">
/// <item><description><strong>Result Caching:</strong> Cache query results to improve performance</description></item>
/// <item><description><strong>Performance Monitoring:</strong> Track slow queries for optimization</description></item>
/// <item><description><strong>Read Replicas:</strong> Route queries to read-only database replicas</description></item>
/// <item><description><strong>Data Projection:</strong> Apply common transformations to query results</description></item>
/// <item><description><strong>Access Logging:</strong> Log what data users are viewing</description></item>
/// </list>
/// </para>
/// <para>
/// <strong>Queries vs Commands:</strong><br/>
/// Queries read data without side effects and should be idempotent (can be called multiple times safely).<br/>
/// Commands modify system state and may have side effects (create, update, delete operations).<br/>
/// Use <see cref="IQueryInterceptor{TQuery, TResponse}"/> for query-specific logic (e.g., caching).<br/>
/// Use <see cref="ICommandInterceptor{TCommand, TResponse}"/> for command-specific logic (e.g., transactions).<br/>
/// Use <see cref="IInterceptor{TRequest, TResponse}"/> for logic that applies to both.
/// </para>
/// <para>
/// <strong>Execution Order:</strong><br/>
/// Query interceptors execute in registration order alongside generic interceptors.<br/>
/// Both <see cref="IInterceptor{TRequest, TResponse}"/> and <see cref="IQueryInterceptor{TQuery, TResponse}"/>
/// registered for the same query type will execute in the order they were added via <c>AddInterceptor()</c>.
/// </para>
/// <para>
/// <strong>Why Separate Interface?</strong><br/>
/// Having a dedicated <see cref="IQueryInterceptor{TQuery, TResponse}"/> interface allows:
/// <list type="bullet">
/// <item><description>Type safety - ensures you only intercept queries</description></item>
/// <item><description>Clearer intent - signals query-specific behavior to other developers</description></item>
/// <item><description>Selective registration - easily register interceptors for queries only</description></item>
/// <item><description>Caching optimization - queries are safe to cache since they don't modify state</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// Example caching interceptor for queries:
/// <code>
/// public class CachingInterceptor&lt;TQuery, TResponse&gt; : IQueryInterceptor&lt;TQuery, TResponse&gt;
///     where TQuery : IQuery&lt;TResponse&gt;
/// {
///     private readonly IDistributedCache _cache;
///     private readonly ILogger&lt;CachingInterceptor&lt;TQuery, TResponse&gt;&gt; _logger;
///
///     public CachingInterceptor(
///         IDistributedCache cache,
///         ILogger&lt;CachingInterceptor&lt;TQuery, TResponse&gt;&gt; logger)
///     {
///         _cache = cache;
///         _logger = logger;
///     }
///
///     public async Task&lt;TResponse&gt; HandleAsync(
///         TQuery query,
///         RequestHandlerDelegate&lt;TResponse&gt; next,
///         CancellationToken cancellationToken)
///     {
///         var queryName = typeof(TQuery).Name;
///         var cacheKey = GenerateCacheKey(query);
///
///         // Try to get from cache
///         var cachedResult = await _cache.GetAsync&lt;TResponse&gt;(cacheKey, cancellationToken);
///         if (cachedResult != null)
///         {
///             _logger.LogInformation("Cache hit for query {QueryName}", queryName);
///             return cachedResult; // Short-circuit - don't call next()
///         }
///
///         _logger.LogInformation("Cache miss for query {QueryName}", queryName);
///
///         // Execute query handler
///         var response = await next();
///
///         // Cache the result
///         await _cache.SetAsync(
///             cacheKey,
///             response,
///             TimeSpan.FromMinutes(5),
///             cancellationToken);
///
///         return response;
///     }
///
///     private string GenerateCacheKey(TQuery query)
///     {
///         var queryType = typeof(TQuery).Name;
///         var queryHash = JsonSerializer.Serialize(query).GetHashCode();
///         return $"query:{queryType}:{queryHash}";
///     }
/// }
/// </code>
/// Example slow query monitoring interceptor:
/// <code>
/// public class SlowQueryMonitorInterceptor&lt;TQuery, TResponse&gt; : IQueryInterceptor&lt;TQuery, TResponse&gt;
///     where TQuery : IQuery&lt;TResponse&gt;
/// {
///     private readonly ILogger&lt;SlowQueryMonitorInterceptor&lt;TQuery, TResponse&gt;&gt; _logger;
///     private readonly IMetricsService _metrics;
///     private const int SlowQueryThresholdMs = 1000;
///
///     public SlowQueryMonitorInterceptor(
///         ILogger&lt;SlowQueryMonitorInterceptor&lt;TQuery, TResponse&gt;&gt; logger,
///         IMetricsService metrics)
///     {
///         _logger = logger;
///         _metrics = metrics;
///     }
///
///     public async Task&lt;TResponse&gt; HandleAsync(
///         TQuery query,
///         RequestHandlerDelegate&lt;TResponse&gt; next,
///         CancellationToken cancellationToken)
///     {
///         var queryName = typeof(TQuery).Name;
///         var startTime = DateTime.UtcNow;
///
///         var response = await next(); // Execute query handler
///
///         var duration = DateTime.UtcNow - startTime;
///
///         // Track metrics
///         _metrics.RecordQueryDuration(queryName, duration.TotalMilliseconds);
///
///         // Log slow queries
///         if (duration.TotalMilliseconds &gt; SlowQueryThresholdMs)
///         {
///             _logger.LogWarning(
///                 "Slow query detected: {QueryName} took {Duration}ms (threshold: {Threshold}ms)",
///                 queryName,
///                 duration.TotalMilliseconds,
///                 SlowQueryThresholdMs);
///         }
///
///         return response;
///     }
/// }
/// </code>
/// </example>
public interface IQueryInterceptor<in TQuery, TResponse> : IInterceptor<TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    // Inherits HandleAsync from IInterceptor<TQuery, TResponse>
    // No additional members needed - this is a marker interface that provides type safety
    // and makes it clear this interceptor is specifically for queries
}
