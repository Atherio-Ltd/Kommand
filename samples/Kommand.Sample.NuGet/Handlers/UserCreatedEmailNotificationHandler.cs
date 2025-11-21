using Kommand.Abstractions;
using Kommand.Sample.Notifications;
using Microsoft.Extensions.Logging;

namespace Kommand.Sample.Handlers;

/// <summary>
/// First handler for UserCreatedNotification - sends a welcome email.
/// Demonstrates multiple handlers for the same notification.
/// </summary>
public class UserCreatedEmailNotificationHandler : INotificationHandler<UserCreatedNotification>
{
    private readonly ILogger<UserCreatedEmailNotificationHandler> _logger;

    public UserCreatedEmailNotificationHandler(ILogger<UserCreatedEmailNotificationHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(UserCreatedNotification notification, CancellationToken cancellationToken)
    {
        // In a real application, this would send an actual email
        _logger.LogInformation(
            "Sending welcome email to {Email} for user {UserId}",
            notification.Email,
            notification.UserId);

        Console.WriteLine($"  [Email Handler] Welcome email sent to {notification.Email}");

        return Task.CompletedTask;
    }
}
