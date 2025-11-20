namespace Kommand;

/// <summary>
/// Represents a void return type for requests that don't return data.
/// This type is used as a placeholder for asynchronous operations that would
/// otherwise return void, since async methods cannot return void.
/// </summary>
/// <remarks>
/// <para>
/// Unit is a functional programming concept that represents "no value" or "void".
/// It is implemented as a struct with no fields, making it extremely lightweight (0 bytes).
/// </para>
/// <para>
/// The <see cref="Value"/> field provides a singleton instance that should be returned
/// by handlers that don't produce a meaningful result.
/// </para>
/// <para>
/// This pattern allows void commands to be treated uniformly with value-returning commands
/// in the mediator pipeline, simplifying the internal implementation.
/// </para>
/// <example>
/// Using Unit in a handler:
/// <code>
/// public class DeleteUserCommandHandler : ICommandHandler&lt;DeleteUserCommand, Unit&gt;
/// {
///     private readonly IUserRepository _repository;
///
///     public async Task&lt;Unit&gt; HandleAsync(DeleteUserCommand command, CancellationToken ct)
///     {
///         await _repository.DeleteAsync(command.UserId, ct);
///         return Unit.Value; // Return the singleton Unit value
///     }
/// }
/// </code>
/// </example>
/// </remarks>
public readonly struct Unit
{
    /// <summary>
    /// Gets the singleton Unit value.
    /// This is the only instance of Unit that should be used throughout the application.
    /// </summary>
    public static readonly Unit Value = default;
}
