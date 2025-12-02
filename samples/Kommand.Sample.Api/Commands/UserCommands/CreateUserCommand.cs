using Kommand.Abstractions;
using Kommand.Sample.Api.Models;

namespace Kommand.Sample.Api.Commands.UserCommands;

/// <summary>
/// Command to create a new user.
/// Demonstrates a command that returns a result (User).
/// </summary>
public record CreateUserCommand(
    string Email,
    string Name,
    string? PhoneNumber) : ICommand<User>;
