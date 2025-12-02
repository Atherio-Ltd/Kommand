using Kommand;
using Kommand.Abstractions;
using Kommand.Sample.Api.Commands;
using Kommand.Sample.Api.Infrastructure;
using Kommand.Sample.Api.Models;
using Kommand.Sample.Api.Notifications;

namespace Kommand.Sample.Api.Handlers;

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

/// <summary>
/// Handler for UpdateUserCommand.
/// Demonstrates a void command handler returning Unit.
/// </summary>
public class UpdateUserCommandHandler : ICommandHandler<UpdateUserCommand, Unit>
{
    private readonly IUserRepository _repository;

    public UpdateUserCommandHandler(IUserRepository repository)
    {
        _repository = repository;
    }

    public async Task<Unit> HandleAsync(UpdateUserCommand command, CancellationToken cancellationToken)
    {
        var user = await _repository.GetByIdAsync(command.UserId, cancellationToken)
            ?? throw new InvalidOperationException($"User with ID {command.UserId} not found");

        user.Name = command.Name;
        user.PhoneNumber = command.PhoneNumber;
        user.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(user, cancellationToken);

        return Unit.Value;
    }
}

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
