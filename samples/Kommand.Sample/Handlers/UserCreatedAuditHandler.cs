using Kommand.Abstractions;
using Kommand.Sample.Notifications;
using Microsoft.Extensions.Logging;

namespace Kommand.Sample.Handlers;

/// <summary>
/// Second handler for UserCreatedNotification - logs audit trail.
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
        // In a real application, this would write to an audit log table/service
        _logger.LogInformation(
            "Audit: User created - ID: {UserId}, Name: {Name}",
            notification.UserId,
            notification.Name);

        Console.WriteLine($"  [Audit Handler] Audit log created for user {notification.UserId}");

        return Task.CompletedTask;
    }
}
