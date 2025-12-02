using Kommand.Abstractions;
using Kommand.Sample.Api.Notifications;
using Microsoft.Extensions.Logging;

namespace Kommand.Sample.Api.Handlers;

/// <summary>
/// Sends a welcome email when a user is created.
/// Demonstrates notification handler with DI (ILogger).
/// </summary>
public class UserCreatedEmailHandler : INotificationHandler<UserCreatedNotification>
{
    private readonly ILogger<UserCreatedEmailHandler> _logger;

    public UserCreatedEmailHandler(ILogger<UserCreatedEmailHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(UserCreatedNotification notification, CancellationToken cancellationToken)
    {
        // In a real application, this would send an actual email
        _logger.LogInformation(
            "[Email] Sending welcome email to {Email} for user {UserId} ({Name})",
            notification.Email,
            notification.UserId,
            notification.Name);

        return Task.CompletedTask;
    }
}

/// <summary>
/// Creates an audit log entry when a user is created.
/// Demonstrates multiple handlers for the same notification.
/// </summary>
public class UserCreatedAuditHandler : INotificationHandler<UserCreatedNotification>
{
    private readonly ILogger<UserCreatedAuditHandler> _logger;

    public UserCreatedAuditHandler(ILogger<UserCreatedAuditHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(UserCreatedNotification notification, CancellationToken cancellationToken)
    {
        // In a real application, this would write to an audit log table
        _logger.LogInformation(
            "[Audit] User created - ID: {UserId}, Email: {Email}, Name: {Name}",
            notification.UserId,
            notification.Email,
            notification.Name);

        return Task.CompletedTask;
    }
}

/// <summary>
/// Handles user deactivation events.
/// </summary>
public class UserDeactivatedHandler : INotificationHandler<UserDeactivatedNotification>
{
    private readonly ILogger<UserDeactivatedHandler> _logger;

    public UserDeactivatedHandler(ILogger<UserDeactivatedHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(UserDeactivatedNotification notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "[Audit] User deactivated - ID: {UserId}, Email: {Email}",
            notification.UserId,
            notification.Email);

        // In a real application, you might:
        // - Revoke access tokens
        // - Notify the user
        // - Archive user data
        // - Update related records

        return Task.CompletedTask;
    }
}

/// <summary>
/// Handles product creation events.
/// </summary>
public class ProductCreatedHandler : INotificationHandler<ProductCreatedNotification>
{
    private readonly ILogger<ProductCreatedHandler> _logger;

    public ProductCreatedHandler(ILogger<ProductCreatedHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(ProductCreatedNotification notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "[Catalog] Product created - ID: {ProductId}, Name: {Name}, SKU: {Sku}",
            notification.ProductId,
            notification.Name,
            notification.Sku);

        // In a real application, you might:
        // - Update search index
        // - Notify inventory system
        // - Send to analytics

        return Task.CompletedTask;
    }
}
