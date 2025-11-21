using Kommand;
using Kommand.Abstractions;
using Kommand.Sample.Commands;
using Kommand.Sample.Infrastructure;

namespace Kommand.Sample.Handlers;

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
        // Retrieve existing user
        var user = await _repository.GetByIdAsync(command.UserId, cancellationToken);

        if (user == null)
        {
            throw new InvalidOperationException($"User with ID {command.UserId} not found");
        }

        // Update properties
        user.Name = command.Name;
        user.UpdatedAt = DateTime.UtcNow;

        // Save changes
        await _repository.UpdateAsync(user, cancellationToken);

        // Return Unit to indicate completion (void command)
        return Unit.Value;
    }
}
