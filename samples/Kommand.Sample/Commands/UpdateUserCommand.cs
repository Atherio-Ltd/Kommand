using Kommand;
using Kommand.Abstractions;

namespace Kommand.Sample.Commands;

/// <summary>
/// Command to update an existing user.
/// This demonstrates a void command using Unit (no return value).
/// </summary>
public record UpdateUserCommand(Guid UserId, string Name) : ICommand;
