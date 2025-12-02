using Kommand.Abstractions;
using Kommand.Sample.Api.Notifications;
using Microsoft.Extensions.Logging;

namespace Kommand.Sample.Api.Handlers.NotificationHandlers;

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
