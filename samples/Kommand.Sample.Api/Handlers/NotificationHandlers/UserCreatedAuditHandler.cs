using Kommand.Abstractions;
using Kommand.Sample.Api.Notifications;
using Microsoft.Extensions.Logging;

namespace Kommand.Sample.Api.Handlers.NotificationHandlers;

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
