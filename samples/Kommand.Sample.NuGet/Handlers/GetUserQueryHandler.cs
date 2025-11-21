using Kommand.Abstractions;
using Kommand.Sample.Infrastructure;
using Kommand.Sample.Models;
using Kommand.Sample.Queries;

namespace Kommand.Sample.Handlers;

/// <summary>
/// Handler for GetUserQuery.
/// Demonstrates a query handler returning a single object (or null).
/// </summary>
public class GetUserQueryHandler : IQueryHandler<GetUserQuery, User?>
{
    private readonly IUserRepository _repository;

    public GetUserQueryHandler(IUserRepository repository)
    {
        _repository = repository;
    }

    public async Task<User?> HandleAsync(GetUserQuery query, CancellationToken cancellationToken)
    {
        return await _repository.GetByIdAsync(query.UserId, cancellationToken);
    }
}
