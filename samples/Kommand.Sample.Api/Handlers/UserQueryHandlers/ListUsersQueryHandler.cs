using Kommand.Abstractions;
using Kommand.Sample.Api.Infrastructure;
using Kommand.Sample.Api.Models;
using Kommand.Sample.Api.Queries;
using Kommand.Sample.Api.Queries.UserQueries;

namespace Kommand.Sample.Api.Handlers.UserQueryHandlers;

/// <summary>
/// Handler for ListUsersQuery.
/// Demonstrates a query handler returning a paginated collection.
/// </summary>
public class ListUsersQueryHandler : IQueryHandler<ListUsersQuery, PagedResult<User>>
{
    private readonly IUserRepository _repository;

    public ListUsersQueryHandler(IUserRepository repository)
    {
        _repository = repository;
    }

    public async Task<PagedResult<User>> HandleAsync(ListUsersQuery query, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _repository.GetAllAsync(
            query.ActiveOnly,
            query.Page,
            query.PageSize,
            cancellationToken);

        return new PagedResult<User>(items, totalCount, query.Page, query.PageSize);
    }
}
