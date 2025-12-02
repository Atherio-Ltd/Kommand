using Kommand;
using Kommand.Abstractions;
using Kommand.Sample.Api.Commands.UserCommands;
using Kommand.Sample.Api.Infrastructure;
using Kommand.Sample.Api.Notifications;

namespace Kommand.Sample.Api.Handlers.UserCommandHandlers;

/// <summary>
/// Handler for DeactivateUserCommand.
/// Demonstrates soft-delete with notification publishing.
/// </summary>
public class DeactivateUserCommandHandler : ICommandHandler<DeactivateUserCommand, Unit>
{
    private readonly IUserRepository _repository;
    private readonly IMediator _mediator;

    public DeactivateUserCommandHandler(IUserRepository repository, IMediator mediator)
    {
        _repository = repository;
        _mediator = mediator;
    }

    public async Task<Unit> HandleAsync(DeactivateUserCommand command, CancellationToken cancellationToken)
    {
        var user = await _repository.GetByIdAsync(command.UserId, cancellationToken)
            ?? throw new InvalidOperationException($"User with ID {command.UserId} not found");

        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(user, cancellationToken);

        // Publish domain event for user deactivation
        await _mediator.PublishAsync(
            new UserDeactivatedNotification(user.Id, user.Email),
            cancellationToken);

        return Unit.Value;
    }
}
