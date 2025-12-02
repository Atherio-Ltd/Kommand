using Kommand.Abstractions;
using Kommand.Sample.Api.Infrastructure;
using Kommand.Sample.Api.Models;
using Kommand.Sample.Api.Queries;

namespace Kommand.Sample.Api.Handlers;

/// <summary>
/// Handler for GetUserByIdQuery.
/// Demonstrates a query handler returning a single object (or null).
/// </summary>
public class GetUserByIdQueryHandler : IQueryHandler<GetUserByIdQuery, User?>
{
    private readonly IUserRepository _repository;

    public GetUserByIdQueryHandler(IUserRepository repository)
    {
        _repository = repository;
    }

    public async Task<User?> HandleAsync(GetUserByIdQuery query, CancellationToken cancellationToken)
    {
        return await _repository.GetByIdAsync(query.UserId, cancellationToken);
    }
}

/// <summary>
/// Handler for GetUserByEmailQuery.
/// </summary>
public class GetUserByEmailQueryHandler : IQueryHandler<GetUserByEmailQuery, User?>
{
    private readonly IUserRepository _repository;

    public GetUserByEmailQueryHandler(IUserRepository repository)
    {
        _repository = repository;
    }

    public async Task<User?> HandleAsync(GetUserByEmailQuery query, CancellationToken cancellationToken)
    {
        return await _repository.GetByEmailAsync(query.Email, cancellationToken);
    }
}

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
