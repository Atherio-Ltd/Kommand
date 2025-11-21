using Kommand.Abstractions;
using Kommand.Sample.Commands;
using Kommand.Sample.Infrastructure;
using Kommand.Sample.Models;
using Kommand.Sample.Notifications;

namespace Kommand.Sample.Handlers;

/// <summary>
/// Handler for CreateUserCommand.
/// Demonstrates:
/// - Injecting scoped dependencies (repository)
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
        // Create user entity
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = command.Email,
            Name = command.Name,
            CreatedAt = DateTime.UtcNow
        };

        // Save to repository
        await _repository.AddAsync(user, cancellationToken);

        // Publish domain event (notification) - multiple handlers can listen
        await _mediator.PublishAsync(
            new UserCreatedNotification(user.Id, user.Email, user.Name),
            cancellationToken);

        return user;
    }
}
