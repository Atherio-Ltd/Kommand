namespace Kommand;

using Kommand.Abstractions;

/// <summary>
/// Interceptor interface specifically for commands (write operations that change state).
/// Implement this interface to add cross-cutting concerns that only apply to commands.
/// </summary>
/// <typeparam name="TCommand">The type of command being handled (must implement ICommand&lt;TResponse&gt;)</typeparam>
/// <typeparam name="TResponse">The type of response returned by the command handler</typeparam>
/// <remarks>
/// <para>
/// Use <see cref="ICommandInterceptor{TCommand, TResponse}"/> when you need to intercept
/// only commands (not queries). This is useful for concerns that only apply to write operations:
/// <list type="bullet">
/// <item><description><strong>Audit Logging:</strong> Log who executed which commands and when</description></item>
/// <item><description><strong>Transaction Management:</strong> Wrap command execution in database transactions</description></item>
/// <item><description><strong>Authorization:</strong> Verify user has permission to execute specific commands</description></item>
/// <item><description><strong>Domain Events:</strong> Publish events after successful command execution</description></item>
/// <item><description><strong>Change Tracking:</strong> Track what data was modified by commands</description></item>
/// </list>
/// </para>
/// <para>
/// <strong>Commands vs Queries:</strong><br/>
/// Commands modify system state and may have side effects (create, update, delete operations).<br/>
/// Queries read data without side effects and should be idempotent.<br/>
/// Use <see cref="ICommandInterceptor{TCommand, TResponse}"/> for command-specific logic.<br/>
/// Use <see cref="IQueryInterceptor{TQuery, TResponse}"/> for query-specific logic (e.g., caching).<br/>
/// Use <see cref="IInterceptor{TRequest, TResponse}"/> for logic that applies to both.
/// </para>
/// <para>
/// <strong>Execution Order:</strong><br/>
/// Command interceptors execute in registration order alongside generic interceptors.<br/>
/// Both <see cref="IInterceptor{TRequest, TResponse}"/> and <see cref="ICommandInterceptor{TCommand, TResponse}"/>
/// registered for the same command type will execute in the order they were added via <c>AddInterceptor()</c>.
/// </para>
/// <para>
/// <strong>Why Separate Interface?</strong><br/>
/// Having a dedicated <see cref="ICommandInterceptor{TCommand, TResponse}"/> interface allows:
/// <list type="bullet">
/// <item><description>Type safety - ensures you only intercept commands</description></item>
/// <item><description>Clearer intent - signals command-specific behavior to other developers</description></item>
/// <item><description>Selective registration - easily register interceptors for commands only</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// Example audit logging interceptor for commands:
/// <code>
/// public class AuditLoggingInterceptor&lt;TCommand, TResponse&gt; : ICommandInterceptor&lt;TCommand, TResponse&gt;
///     where TCommand : ICommand&lt;TResponse&gt;
/// {
///     private readonly IAuditLogger _auditLogger;
///     private readonly ICurrentUserService _currentUser;
///
///     public AuditLoggingInterceptor(
///         IAuditLogger auditLogger,
///         ICurrentUserService currentUser)
///     {
///         _auditLogger = auditLogger;
///         _currentUser = currentUser;
///     }
///
///     public async Task&lt;TResponse&gt; HandleAsync(
///         TCommand command,
///         RequestHandlerDelegate&lt;TResponse&gt; next,
///         CancellationToken cancellationToken)
///     {
///         var commandName = typeof(TCommand).Name;
///         var userId = _currentUser.GetUserId();
///
///         // Log before execution
///         await _auditLogger.LogCommandExecutionAsync(
///             commandName,
///             userId,
///             command,
///             cancellationToken);
///
///         try
///         {
///             var response = await next(); // Execute command handler
///
///             // Log successful execution
///             await _auditLogger.LogCommandSuccessAsync(
///                 commandName,
///                 userId,
///                 response,
///                 cancellationToken);
///
///             return response;
///         }
///         catch (Exception ex)
///         {
///             // Log failure
///             await _auditLogger.LogCommandFailureAsync(
///                 commandName,
///                 userId,
///                 ex,
///                 cancellationToken);
///             throw;
///         }
///     }
/// }
/// </code>
/// Example transaction management interceptor:
/// <code>
/// public class TransactionInterceptor&lt;TCommand, TResponse&gt; : ICommandInterceptor&lt;TCommand, TResponse&gt;
///     where TCommand : ICommand&lt;TResponse&gt;
/// {
///     private readonly ApplicationDbContext _dbContext;
///
///     public TransactionInterceptor(ApplicationDbContext dbContext)
///     {
///         _dbContext = dbContext;
///     }
///
///     public async Task&lt;TResponse&gt; HandleAsync(
///         TCommand command,
///         RequestHandlerDelegate&lt;TResponse&gt; next,
///         CancellationToken cancellationToken)
///     {
///         // Commands that don't modify data don't need transactions
///         if (command is IReadOnlyCommand)
///         {
///             return await next();
///         }
///
///         // Start transaction
///         await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
///
///         try
///         {
///             var response = await next(); // Execute command handler
///
///             // Commit transaction on success
///             await transaction.CommitAsync(cancellationToken);
///
///             return response;
///         }
///         catch
///         {
///             // Transaction automatically rolls back on exception
///             throw;
///         }
///     }
/// }
/// </code>
/// </example>
public interface ICommandInterceptor<in TCommand, TResponse> : IInterceptor<TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    // Inherits HandleAsync from IInterceptor<TCommand, TResponse>
    // No additional members needed - this is a marker interface that provides type safety
}
