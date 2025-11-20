namespace Kommand.Abstractions;

/// <summary>
/// Mediator for dispatching commands, queries, and notifications.
/// This is the primary interface that applications inject and use to send requests.
/// </summary>
/// <remarks>
/// <para>
/// The mediator pattern decouples the sender of a request from its handler, enabling
/// loose coupling, testability, and separation of concerns. IMediator provides a single
/// entry point for all application requests.
/// </para>
/// <para>
/// <strong>Method Overview:</strong>
/// <list type="bullet">
/// <item><description><see cref="SendAsync{TResponse}(ICommand{TResponse}, CancellationToken)"/> - Execute commands that return a value</description></item>
/// <item><description><see cref="SendAsync(ICommand, CancellationToken)"/> - Execute commands without a return value</description></item>
/// <item><description><see cref="QueryAsync{TResponse}(IQuery{TResponse}, CancellationToken)"/> - Execute queries to retrieve data</description></item>
/// <item><description><see cref="PublishAsync{TNotification}(TNotification, CancellationToken)"/> - Publish notifications to all handlers</description></item>
/// </list>
/// </para>
/// <para>
/// <strong>Semantic Differences:</strong><br/>
/// - <strong>SendAsync (Commands)</strong>: For write operations that change state. Exactly one handler executes.
///   Throws if no handler is registered.<br/>
/// - <strong>QueryAsync (Queries)</strong>: For read-only operations. Exactly one handler executes.
///   Throws if no handler is registered.<br/>
/// - <strong>PublishAsync (Notifications)</strong>: For domain events. Zero or more handlers execute.
///   Does NOT throw if no handlers are registered.
/// </para>
/// <para>
/// <strong>Dependency Injection:</strong><br/>
/// Register IMediator using the AddKommand extension method:
/// <code>
/// services.AddKommand(config =>
/// {
///     config.RegisterHandlersFromAssembly(typeof(Program).Assembly);
/// });
/// </code>
/// Then inject IMediator where needed (controllers, services, etc.).
/// </para>
/// <example>
/// Example usage in an ASP.NET Core controller:
/// <code>
/// public class UsersController : ControllerBase
/// {
///     private readonly IMediator _mediator;
///
///     public UsersController(IMediator mediator)
///     {
///         _mediator = mediator;
///     }
///
///     [HttpPost]
///     public async Task&lt;IActionResult&gt; CreateUser(
///         [FromBody] CreateUserRequest request,
///         CancellationToken cancellationToken)
///     {
///         var command = new CreateUserCommand(request.Email, request.Name);
///         var user = await _mediator.SendAsync(command, cancellationToken);
///
///         // Publish domain event
///         await _mediator.PublishAsync(
///             new UserCreatedNotification(user.Id, user.Email, DateTime.UtcNow),
///             cancellationToken);
///
///         return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
///     }
///
///     [HttpGet("{id}")]
///     public async Task&lt;IActionResult&gt; GetUser(
///         Guid id,
///         CancellationToken cancellationToken)
///     {
///         var query = new GetUserByIdQuery(id);
///         var user = await _mediator.QueryAsync(query, cancellationToken);
///         return Ok(user);
///     }
///
///     [HttpDelete("{id}")]
///     public async Task&lt;IActionResult&gt; DeleteUser(
///         Guid id,
///         CancellationToken cancellationToken)
///     {
///         var command = new DeleteUserCommand(id);
///         await _mediator.SendAsync(command, cancellationToken); // Void command
///         return NoContent();
///     }
/// }
/// </code>
/// </example>
/// </remarks>
public interface IMediator
{
    /// <summary>
    /// Sends a command with a response and waits for the result.
    /// </summary>
    /// <typeparam name="TResponse">The type of value returned by the command handler</typeparam>
    /// <param name="command">The command to execute</param>
    /// <param name="cancellationToken">
    /// Optional cancellation token. Defaults to <see cref="CancellationToken.None"/> if not provided.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous operation, containing the result from the handler.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no handler is registered for the command type.
    /// </exception>
    /// <remarks>
    /// Use this method for commands that modify state and return a value (e.g., CreateUser returns User entity).
    /// The command will be routed to exactly one handler. If no handler is found, an exception is thrown.
    /// </remarks>
    /// <example>
    /// <code>
    /// var command = new CreateUserCommand("user@example.com", "John Doe");
    /// var user = await _mediator.SendAsync(command, cancellationToken);
    /// </code>
    /// </example>
    Task<TResponse> SendAsync<TResponse>(
        ICommand<TResponse> command,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a command without a response (void command).
    /// </summary>
    /// <param name="command">The command to execute</param>
    /// <param name="cancellationToken">
    /// Optional cancellation token. Defaults to <see cref="CancellationToken.None"/> if not provided.
    /// </param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no handler is registered for the command type.
    /// </exception>
    /// <remarks>
    /// Use this method for commands that modify state but don't need to return data (e.g., DeleteUser, UpdateSettings).
    /// The command will be routed to exactly one handler. If no handler is found, an exception is thrown.
    /// </remarks>
    /// <example>
    /// <code>
    /// var command = new DeleteUserCommand(userId);
    /// await _mediator.SendAsync(command, cancellationToken);
    /// </code>
    /// </example>
    Task SendAsync(
        ICommand command,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a query and returns the result.
    /// </summary>
    /// <typeparam name="TResponse">The type of data returned by the query handler</typeparam>
    /// <param name="query">The query to execute</param>
    /// <param name="cancellationToken">
    /// Optional cancellation token. Defaults to <see cref="CancellationToken.None"/> if not provided.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous operation, containing the data from the handler.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no handler is registered for the query type.
    /// </exception>
    /// <remarks>
    /// Use this method for read-only operations that retrieve data without modifying state.
    /// The query will be routed to exactly one handler. If no handler is found, an exception is thrown.
    /// </remarks>
    /// <example>
    /// <code>
    /// var query = new GetUserByIdQuery(userId);
    /// var user = await _mediator.QueryAsync(query, cancellationToken);
    /// </code>
    /// </example>
    Task<TResponse> QueryAsync<TResponse>(
        IQuery<TResponse> query,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes a notification to all registered handlers.
    /// </summary>
    /// <typeparam name="TNotification">The type of notification to publish</typeparam>
    /// <param name="notification">The notification instance containing event data</param>
    /// <param name="cancellationToken">
    /// Optional cancellation token. Defaults to <see cref="CancellationToken.None"/> if not provided.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous operation. Completes when all handlers have executed.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Use this method to publish domain events and trigger side effects. Unlike SendAsync and QueryAsync,
    /// PublishAsync does NOT throw an exception if no handlers are registered - it simply completes silently.
    /// </para>
    /// <para>
    /// All registered handlers will execute sequentially. If one handler fails, others will still execute.
    /// Exceptions from handlers are caught, logged, and swallowed to ensure resilience.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var notification = new UserCreatedNotification(userId, email, DateTime.UtcNow);
    /// await _mediator.PublishAsync(notification, cancellationToken);
    /// // Multiple handlers may execute: email sender, audit logger, cache invalidator, etc.
    /// </code>
    /// </example>
    Task PublishAsync<TNotification>(
        TNotification notification,
        CancellationToken cancellationToken = default)
        where TNotification : INotification;
}
