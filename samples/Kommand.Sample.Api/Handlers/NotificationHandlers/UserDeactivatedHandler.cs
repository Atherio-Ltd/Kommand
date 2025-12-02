using Kommand.Abstractions;
using Kommand.Sample.Api.Notifications;
using Microsoft.Extensions.Logging;

namespace Kommand.Sample.Api.Handlers.NotificationHandlers;

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
