using Kommand.Abstractions;
using Kommand.Sample.Api.Infrastructure;
using Kommand.Sample.Api.Models;
using Kommand.Sample.Api.Queries.UserQueries;

namespace Kommand.Sample.Api.Handlers.UserQueryHandlers;

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
