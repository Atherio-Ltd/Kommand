using Kommand.Abstractions;
using Kommand.Sample.Api.Commands.UserCommands;
using Kommand.Sample.Api.Infrastructure;
using Kommand.Sample.Api.Models;
using Kommand.Sample.Api.Notifications;

namespace Kommand.Sample.Api.Handlers.UserCommandHandlers;

/// <summary>
/// Handler for CreateUserCommand.
/// Demonstrates:
/// - Injecting scoped dependencies (repository, mediator)
/// - Publishing notifications after successful creation
/// - Returning a result from a command
/// </summary>
public class CreateUserCommandHandler : ICommandHandler<CreateUserCommand, User>
{
    private readonly IUserRepository _repository;
    private readonly IMediator _mediator;

    public CreateUserCommandHandler(IUserRepository repository, IMediator mediator)
    {
        _repository = repository;
        _mediator = mediator;
    }

    public async Task<User> HandleAsync(CreateUserCommand command, CancellationToken cancellationToken)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = command.Email,
            Name = command.Name,
            PhoneNumber = command.PhoneNumber,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(user, cancellationToken);

        // Publish domain event - multiple handlers will react
        await _mediator.PublishAsync(
            new UserCreatedNotification(user.Id, user.Email, user.Name),
            cancellationToken);

        return user;
    }
}
