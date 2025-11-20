namespace Kommand.Abstractions;

/// <summary>
/// Handler interface for processing queries.
/// Implement this interface to define the logic for retrieving data in response to a query.
/// </summary>
/// <typeparam name="TQuery">The type of query to handle</typeparam>
/// <typeparam name="TResponse">The type of data returned by the query</typeparam>
/// <remarks>
/// <para>
/// Query handlers contain the logic for retrieving and returning data without modifying state.
/// Each query should have exactly one handler. Query handlers should be idempotent and have
/// no observable side effects.
/// </para>
/// <para>
/// Handlers are automatically discovered and registered in the dependency injection container
/// when using <c>RegisterHandlersFromAssembly()</c>. By default, handlers are registered with
/// a <strong>Scoped</strong> lifetime, which allows them to share database contexts and other
/// resources within a single request scope.
/// </para>
/// <para>
/// <strong>Best Practices:</strong>
/// <list type="bullet">
/// <item><description>Keep query handlers purely read-only - no state modifications</description></item>
/// <item><description>Inject repositories or read-only data access layers</description></item>
/// <item><description>Consider using optimized read models or projections for performance</description></item>
/// <item><description>Respect the cancellation token for responsive cancellation</description></item>
/// <item><description>Use async/await for database or I/O operations</description></item>
/// <item><description>Return DTOs or view models rather than domain entities when appropriate</description></item>
/// </list>
/// </para>
/// <example>
/// Example query handler that retrieves a user by ID:
/// <code>
/// public class GetUserByIdQueryHandler : IQueryHandler&lt;GetUserByIdQuery, UserDto&gt;
/// {
///     private readonly IUserRepository _repository;
///     private readonly IMapper _mapper;
///
///     public GetUserByIdQueryHandler(IUserRepository repository, IMapper mapper)
///     {
///         _repository = repository;
///         _mapper = mapper;
///     }
///
///     public async Task&lt;UserDto&gt; HandleAsync(
///         GetUserByIdQuery query,
///         CancellationToken cancellationToken)
///     {
///         var user = await _repository.GetByIdAsync(query.UserId, cancellationToken);
///
///         if (user == null)
///             throw new NotFoundException($"User with ID {query.UserId} not found");
///
///         return _mapper.Map&lt;UserDto&gt;(user);
///     }
/// }
/// </code>
/// </example>
/// <example>
/// Example query handler that returns a list with filtering:
/// <code>
/// public class SearchProductsQueryHandler
///     : IQueryHandler&lt;SearchProductsQuery, List&lt;ProductDto&gt;&gt;
/// {
///     private readonly IProductRepository _repository;
///
///     public SearchProductsQueryHandler(IProductRepository repository)
///     {
///         _repository = repository;
///     }
///
///     public async Task&lt;List&lt;ProductDto&gt;&gt; HandleAsync(
///         SearchProductsQuery query,
///         CancellationToken cancellationToken)
///     {
///         return await _repository
///             .SearchAsync(query.SearchTerm, query.CategoryId, cancellationToken);
///     }
/// }
/// </code>
/// </example>
/// </remarks>
public interface IQueryHandler<in TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    /// <summary>
    /// Handles the query asynchronously and returns the requested data.
    /// </summary>
    /// <param name="query">The query instance containing the parameters for data retrieval</param>
    /// <param name="cancellationToken">
    /// Cancellation token that should be observed to allow graceful cancellation of long-running operations.
    /// Always pass this token to async methods like repository calls, database queries, etc.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous operation, containing the result of type <typeparamref name="TResponse"/>.
    /// </returns>
    Task<TResponse> HandleAsync(TQuery query, CancellationToken cancellationToken);
}
