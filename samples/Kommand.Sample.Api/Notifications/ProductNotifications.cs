using Kommand.Abstractions;

namespace Kommand.Sample.Api.Notifications;

/// <summary>
/// Domain event published when a product is created.
/// </summary>
public record ProductCreatedNotification(
    Guid ProductId,
    string Name,
    string Sku) : INotification;
