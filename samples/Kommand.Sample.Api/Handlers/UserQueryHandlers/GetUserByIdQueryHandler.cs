using Kommand.Abstractions;
using Kommand.Sample.Api.Infrastructure;
using Kommand.Sample.Api.Models;
using Kommand.Sample.Api.Queries.UserQueries;

namespace Kommand.Sample.Api.Handlers.UserQueryHandlers;

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
