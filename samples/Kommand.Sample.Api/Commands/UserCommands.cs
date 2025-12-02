using Kommand.Abstractions;
using Kommand.Sample.Api.Models;

namespace Kommand.Sample.Api.Commands;

/// <summary>
/// Command to create a new user.
/// Demonstrates a command that returns a result (User).
/// </summary>
public record CreateUserCommand(
    string Email,
    string Name,
    string? PhoneNumber) : ICommand<User>;

/// <summary>
/// Command to update an existing user.
/// Demonstrates a void command using Unit (no return value).
/// </summary>
public record UpdateUserCommand(
    Guid UserId,
    string Name,
    string? PhoneNumber) : ICommand;

/// <summary>
/// Command to deactivate (soft-delete) a user.
/// Demonstrates a void command that triggers notifications.
/// </summary>
public record DeactivateUserCommand(Guid UserId) : ICommand;
