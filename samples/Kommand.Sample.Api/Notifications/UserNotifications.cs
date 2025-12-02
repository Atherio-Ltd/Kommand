using Kommand.Abstractions;

namespace Kommand.Sample.Api.Notifications;

/// <summary>
/// Domain event published when a user is created.
/// Multiple handlers can react to this event (pub/sub pattern).
/// </summary>
public record UserCreatedNotification(
    Guid UserId,
    string Email,
    string Name) : INotification;

/// <summary>
/// Domain event published when a user is deactivated.
/// </summary>
public record UserDeactivatedNotification(
    Guid UserId,
    string Email) : INotification;
