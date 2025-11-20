namespace Kommand.Abstractions;

/// <summary>
/// Handler interface for processing commands.
/// Implement this interface to define the business logic for handling a specific command.
/// </summary>
/// <typeparam name="TCommand">The type of command to handle</typeparam>
/// <typeparam name="TResponse">The type of value returned after handling the command</typeparam>
/// <remarks>
/// <para>
/// Command handlers contain the business logic that executes when a command is dispatched
/// through the mediator. Each command should have exactly one handler.
/// </para>
/// <para>
/// Handlers are automatically discovered and registered in the dependency injection container
/// when using <c>RegisterHandlersFromAssembly()</c>. By default, handlers are registered with
/// a <strong>Scoped</strong> lifetime (not Transient like in MediatR), which allows them to
/// participate in database transactions and maintain state within a single request scope.
/// </para>
/// <para>
/// <strong>Best Practices:</strong>
/// <list type="bullet">
/// <item><description>Keep handlers focused on a single responsibility</description></item>
/// <item><description>Inject dependencies through the constructor (repositories, services, etc.)</description></item>
/// <item><description>Respect the cancellation token to allow graceful cancellation</description></item>
/// <item><description>Let exceptions bubble up - they will be handled by the mediator pipeline</description></item>
/// <item><description>Use async/await properly - don't block synchronously</description></item>
/// </list>
/// </para>
/// <example>
/// Example command handler that creates a user:
/// <code>
/// public class CreateUserCommandHandler : ICommandHandler&lt;CreateUserCommand, User&gt;
/// {
///     private readonly IUserRepository _repository;
///     private readonly IEmailService _emailService;
///     private readonly ILogger&lt;CreateUserCommandHandler&gt; _logger;
///
///     public CreateUserCommandHandler(
///         IUserRepository repository,
///         IEmailService emailService,
///         ILogger&lt;CreateUserCommandHandler&gt; logger)
///     {
///         _repository = repository;
///         _emailService = emailService;
///         _logger = logger;
///     }
///
///     public async Task&lt;User&gt; HandleAsync(
///         CreateUserCommand command,
///         CancellationToken cancellationToken)
///     {
///         _logger.LogInformation("Creating user with email {Email}", command.Email);
///
///         var user = new User
///         {
///             Email = command.Email,
///             Name = command.Name,
///             CreatedAt = DateTime.UtcNow
///         };
///
///         await _repository.AddAsync(user, cancellationToken);
///         await _emailService.SendWelcomeEmailAsync(user.Email, cancellationToken);
///
///         return user;
///     }
/// }
/// </code>
/// </example>
/// </remarks>
public interface ICommandHandler<in TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    /// <summary>
    /// Handles the command asynchronously and returns a result.
    /// </summary>
    /// <param name="command">The command instance to handle</param>
    /// <param name="cancellationToken">
    /// Cancellation token that should be observed to allow graceful cancellation of long-running operations.
    /// Always pass this token to async methods like repository calls, HTTP requests, etc.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous operation, containing the result of type <typeparamref name="TResponse"/>.
    /// </returns>
    Task<TResponse> HandleAsync(TCommand command, CancellationToken cancellationToken);
}
