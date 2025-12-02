using Kommand.Abstractions;

namespace Kommand.Sample.Api.Commands.UserCommands;

/// <summary>
/// Command to deactivate (soft-delete) a user.
/// Demonstrates a void command that triggers notifications.
/// </summary>
public record DeactivateUserCommand(Guid UserId) : ICommand;
