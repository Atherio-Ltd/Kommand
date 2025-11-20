namespace Kommand.Abstractions;

/// <summary>
/// Marker interface for queries (read-only operations that don't change state).
/// Queries retrieve data without causing side effects or modifying system state.
/// </summary>
/// <typeparam name="TResponse">The type of data returned by the query</typeparam>
/// <remarks>
/// <para>
/// Queries are the other half of the CQRS (Command Query Responsibility Segregation) pattern.
/// They represent operations that read data without modifying it. Queries should be idempotent
/// and have no observable side effects on the system state.
/// </para>
/// <para>
/// Use queries when you need to:
/// <list type="bullet">
/// <item><description>Retrieve a single entity by ID (e.g., GetUserByIdQuery)</description></item>
/// <item><description>Fetch a list of entities (e.g., GetAllProductsQuery)</description></item>
/// <item><description>Search or filter data (e.g., SearchOrdersQuery)</description></item>
/// <item><description>Generate reports (e.g., GetSalesReportQuery)</description></item>
/// <item><description>Perform calculations on existing data (e.g., CalculateTotalRevenueQuery)</description></item>
/// </list>
/// </para>
/// <para>
/// Queries should be named with nouns or questions that describe what data is being retrieved,
/// such as GetUser, ListProducts, SearchOrders, etc. They should never modify state.
/// </para>
/// <para>
/// <strong>CQRS Principle:</strong> The separation between commands (write) and queries (read)
/// allows for different optimization strategies, scalability patterns, and even separate data stores
/// for reads vs writes in advanced scenarios.
/// </para>
/// <example>
/// Example query that retrieves a user by ID:
/// <code>
/// public record GetUserByIdQuery(Guid UserId) : IQuery&lt;User&gt;;
///
/// public class GetUserByIdQueryHandler : IQueryHandler&lt;GetUserByIdQuery, User&gt;
/// {
///     private readonly IUserRepository _repository;
///
///     public async Task&lt;User&gt; HandleAsync(GetUserByIdQuery query, CancellationToken ct)
///     {
///         return await _repository.GetByIdAsync(query.UserId, ct);
///     }
/// }
/// </code>
/// </example>
/// <example>
/// Example query that returns a list:
/// <code>
/// public record GetProductsByCategoryQuery(int CategoryId) : IQuery&lt;List&lt;Product&gt;&gt;;
///
/// public class GetProductsByCategoryQueryHandler
///     : IQueryHandler&lt;GetProductsByCategoryQuery, List&lt;Product&gt;&gt;
/// {
///     private readonly IProductRepository _repository;
///
///     public async Task&lt;List&lt;Product&gt;&gt; HandleAsync(
///         GetProductsByCategoryQuery query,
///         CancellationToken ct)
///     {
///         return await _repository.GetByCategoryAsync(query.CategoryId, ct);
///     }
/// }
/// </code>
/// </example>
/// </remarks>
public interface IQuery<out TResponse> : IRequest<TResponse>
{
}
