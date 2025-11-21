using Kommand.Abstractions;
using Kommand.Sample.Models;

namespace Kommand.Sample.Commands;

/// <summary>
/// Command to create a new user.
/// This demonstrates a command that returns a result (User).
/// </summary>
public record CreateUserCommand(string Email, string Name) : ICommand<User>;
