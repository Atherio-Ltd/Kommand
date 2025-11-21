using Kommand.Abstractions;
using Kommand.Sample.Infrastructure;
using Kommand.Sample.Models;
using Kommand.Sample.Queries;

namespace Kommand.Sample.Handlers;

/// <summary>
/// Handler for ListUsersQuery.
/// Demonstrates a query handler returning a collection.
/// </summary>
public class ListUsersQueryHandler : IQueryHandler<ListUsersQuery, List<User>>
{
    private readonly IUserRepository _repository;

    public ListUsersQueryHandler(IUserRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<User>> HandleAsync(ListUsersQuery query, CancellationToken cancellationToken)
    {
        return await _repository.GetAllAsync(cancellationToken);
    }
}
