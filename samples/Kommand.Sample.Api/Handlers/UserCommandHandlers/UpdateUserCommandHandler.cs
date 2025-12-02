using Kommand;
using Kommand.Abstractions;
using Kommand.Sample.Api.Commands.UserCommands;
using Kommand.Sample.Api.Infrastructure;

namespace Kommand.Sample.Api.Handlers.UserCommandHandlers;

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
