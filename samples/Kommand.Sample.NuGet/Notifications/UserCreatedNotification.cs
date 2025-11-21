using Kommand.Abstractions;

namespace Kommand.Sample.Notifications;

/// <summary>
/// Domain event published when a user is created.
/// This demonstrates notifications (pub/sub) where multiple handlers can react to the same event.
/// </summary>
public record UserCreatedNotification(Guid UserId, string Email, string Name) : INotification;
