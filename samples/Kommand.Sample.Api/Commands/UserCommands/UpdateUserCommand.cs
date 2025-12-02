using Kommand.Abstractions;

namespace Kommand.Sample.Api.Commands.UserCommands;

/// <summary>
/// Command to update an existing user.
/// Demonstrates a void command using Unit (no return value).
/// </summary>
public record UpdateUserCommand(
    Guid UserId,
    string Name,
    string? PhoneNumber) : ICommand;
