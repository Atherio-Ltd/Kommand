using Kommand.Abstractions;
using Kommand.Sample.Api.Notifications;
using Microsoft.Extensions.Logging;

namespace Kommand.Sample.Api.Handlers.NotificationHandlers;

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
