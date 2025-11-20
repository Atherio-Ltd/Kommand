namespace Kommand.Abstractions;

using Kommand;

/// <summary>
/// Marker interface for commands (write operations that change state).
/// Commands represent intent to modify system state and may return a result.
/// </summary>
/// <typeparam name="TResponse">The type of value returned after the command is executed</typeparam>
/// <remarks>
/// <para>
/// Commands are one half of the CQRS (Command Query Responsibility Segregation) pattern.
/// They represent operations that have side effects and change the state of the system.
/// </para>
/// <para>
/// Use commands when you need to:
/// <list type="bullet">
/// <item><description>Create new entities (e.g., CreateUserCommand)</description></item>
/// <item><description>Update existing data (e.g., UpdateProductPriceCommand)</description></item>
/// <item><description>Delete records (e.g., DeleteOrderCommand)</description></item>
/// <item><description>Perform business operations with side effects (e.g., ProcessPaymentCommand)</description></item>
/// </list>
/// </para>
/// <para>
/// Commands should be named with imperative verbs that clearly express the user's intent,
/// such as CreateUser, UpdateProfile, DeleteAccount, ProcessOrder, etc.
/// </para>
/// <example>
/// Example command that creates a user and returns the created entity:
/// <code>
/// public record CreateUserCommand(string Email, string Name) : ICommand&lt;User&gt;;
///
/// public class CreateUserCommandHandler : ICommandHandler&lt;CreateUserCommand, User&gt;
/// {
///     private readonly IUserRepository _repository;
///
///     public async Task&lt;User&gt; HandleAsync(CreateUserCommand command, CancellationToken ct)
///     {
///         var user = new User { Email = command.Email, Name = command.Name };
///         await _repository.AddAsync(user, ct);
///         return user;
///     }
/// }
/// </code>
/// </example>
/// </remarks>
public interface ICommand<out TResponse> : IRequest<TResponse>
{
}

/// <summary>
/// Marker interface for commands that do not return a value (void commands).
/// These commands perform operations for their side effects only.
/// </summary>
/// <remarks>
/// <para>
/// Use this interface when your command modifies state but doesn't need to return any data.
/// The handler will still return <see cref="Unit"/> to maintain a consistent async pattern,
/// but this is just a placeholder representing "no meaningful value".
/// </para>
/// <example>
/// Example void command:
/// <code>
/// public record DeleteUserCommand(Guid UserId) : ICommand;
///
/// public class DeleteUserCommandHandler : ICommandHandler&lt;DeleteUserCommand, Unit&gt;
/// {
///     private readonly IUserRepository _repository;
///
///     public async Task&lt;Unit&gt; HandleAsync(DeleteUserCommand command, CancellationToken ct)
///     {
///         await _repository.DeleteAsync(command.UserId, ct);
///         return Unit.Value;
///     }
/// }
/// </code>
/// </example>
/// </remarks>
public interface ICommand : ICommand<Unit>
{
}
