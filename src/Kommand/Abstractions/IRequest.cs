namespace Kommand.Abstractions;

using Kommand;

/// <summary>
/// Marker interface for all requests with a response type.
/// This is the base abstraction that all commands and queries inherit from.
/// </summary>
/// <typeparam name="TResponse">The response type returned by the request handler</typeparam>
/// <remarks>
/// <para>
/// IRequest represents an operation that will be handled asynchronously and returns a result.
/// It uses a covariant type parameter to allow handlers to return more derived types.
/// </para>
/// <para>
/// This interface should not be implemented directly. Instead, use ICommand or IQuery
/// for write operations that change state or read-only operations respectively.
/// </para>
/// <example>
/// Example request that returns a User entity:
/// <code>
/// public record CreateUserRequest(string Email, string Name) : IRequest&lt;User&gt;;
/// </code>
/// Example request that returns a list of products:
/// <code>
/// public record GetProductsRequest(int CategoryId) : IRequest&lt;List&lt;Product&gt;&gt;;
/// </code>
/// </example>
/// </remarks>
public interface IRequest<out TResponse>
{
}

/// <summary>
/// Marker interface for requests without a response (void operations).
/// This interface inherits from <see cref="IRequest{TResponse}"/> using <see cref="Unit"/> as the response type.
/// </summary>
/// <remarks>
/// <para>
/// Use this interface when your command or query does not need to return any data.
/// The operation will still be asynchronous, but the result will be a <see cref="Unit"/> value
/// representing successful completion.
/// </para>
/// <para>
/// This is semantically equivalent to a void method, but since async methods cannot return void,
/// we use the Unit type as a placeholder.
/// </para>
/// <example>
/// Example void command:
/// <code>
/// public record DeleteUserCommand(Guid UserId) : ICommand;
///
/// public class DeleteUserCommandHandler : ICommandHandler&lt;DeleteUserCommand, Unit&gt;
/// {
///     public async Task&lt;Unit&gt; HandleAsync(DeleteUserCommand command, CancellationToken ct)
///     {
///         // Delete user logic
///         await _repository.DeleteAsync(command.UserId, ct);
///         return Unit.Value; // Return Unit.Value to indicate completion
///     }
/// }
/// </code>
/// </example>
/// </remarks>
public interface IRequest : IRequest<Unit>
{
}
