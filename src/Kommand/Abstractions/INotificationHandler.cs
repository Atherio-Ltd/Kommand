namespace Kommand.Abstractions;

/// <summary>
/// Handler interface for processing notifications.
/// Multiple handlers can subscribe to the same notification type.
/// </summary>
/// <typeparam name="TNotification">The type of notification to handle</typeparam>
/// <remarks>
/// <para>
/// Notification handlers implement the observer pattern for domain events. Unlike command
/// and query handlers which have a 1:1 relationship with their requests, notification handlers
/// have a many-to-many relationship - one notification can trigger multiple handlers, and
/// one handler can listen to multiple notification types.
/// </para>
/// <para>
/// Handlers are automatically discovered and registered in the dependency injection container
/// when using <c>RegisterHandlersFromAssembly()</c>. All handlers are registered with
/// <strong>Scoped</strong> lifetime by default.
/// </para>
/// <para>
/// <strong>Execution Model:</strong>
/// <list type="bullet">
/// <item><description>Handlers execute sequentially (not in parallel)</description></item>
/// <item><description>Execution order is not guaranteed - don't depend on ordering</description></item>
/// <item><description>One handler's failure doesn't stop others from executing</description></item>
/// <item><description>Exceptions are caught, logged, and swallowed by the mediator</description></item>
/// <item><description>If no handlers are registered, PublishAsync completes silently</description></item>
/// </list>
/// </para>
/// <para>
/// <strong>Best Practices:</strong>
/// <list type="bullet">
/// <item><description>Keep handlers idempotent - they might be retried</description></item>
/// <item><description>Avoid long-running operations - consider background jobs instead</description></item>
/// <item><description>Don't throw exceptions for expected failure cases - log and continue</description></item>
/// <item><description>Don't assume handler execution order</description></item>
/// <item><description>Use notifications for side effects, not critical business logic</description></item>
/// </list>
/// </para>
/// <example>
/// Example: Send welcome email when user is created
/// <code>
/// public class SendWelcomeEmailHandler : INotificationHandler&lt;UserCreatedNotification&gt;
/// {
///     private readonly IEmailService _emailService;
///     private readonly ILogger&lt;SendWelcomeEmailHandler&gt; _logger;
///
///     public SendWelcomeEmailHandler(
///         IEmailService emailService,
///         ILogger&lt;SendWelcomeEmailHandler&gt; logger)
///     {
///         _emailService = emailService;
///         _logger = logger;
///     }
///
///     public async Task HandleAsync(
///         UserCreatedNotification notification,
///         CancellationToken cancellationToken)
///     {
///         try
///         {
///             await _emailService.SendWelcomeEmailAsync(
///                 notification.Email,
///                 cancellationToken);
///
///             _logger.LogInformation(
///                 "Welcome email sent to {Email}",
///                 notification.Email);
///         }
///         catch (Exception ex)
///         {
///             // Log but don't throw - email failure shouldn't break user creation
///             _logger.LogError(ex,
///                 "Failed to send welcome email to {Email}",
///                 notification.Email);
///         }
///     }
/// }
/// </code>
/// </example>
/// <example>
/// Example: Multiple handlers for the same event
/// <code>
/// // Handler 1: Audit log
/// public class AuditUserCreationHandler : INotificationHandler&lt;UserCreatedNotification&gt;
/// {
///     private readonly IAuditService _auditService;
///
///     public async Task HandleAsync(
///         UserCreatedNotification notification,
///         CancellationToken cancellationToken)
///     {
///         await _auditService.LogAsync(
///             $"User {notification.UserId} created",
///             cancellationToken);
///     }
/// }
///
/// // Handler 2: Update cache
/// public class InvalidateUserCacheHandler : INotificationHandler&lt;UserCreatedNotification&gt;
/// {
///     private readonly ICacheService _cache;
///
///     public async Task HandleAsync(
///         UserCreatedNotification notification,
///         CancellationToken cancellationToken)
///     {
///         await _cache.InvalidateAsync("users", cancellationToken);
///     }
/// }
///
/// // Usage: Both handlers execute when notification is published
/// await _mediator.PublishAsync(new UserCreatedNotification(userId, email, DateTime.UtcNow));
/// </code>
/// </example>
/// </remarks>
public interface INotificationHandler<in TNotification>
    where TNotification : INotification
{
    /// <summary>
    /// Handles the notification asynchronously.
    /// </summary>
    /// <param name="notification">The notification instance containing event data</param>
    /// <param name="cancellationToken">
    /// Cancellation token that should be observed to allow graceful cancellation.
    /// Note: If this handler fails or is cancelled, other handlers will still execute.
    /// </param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task HandleAsync(TNotification notification, CancellationToken cancellationToken);
}
