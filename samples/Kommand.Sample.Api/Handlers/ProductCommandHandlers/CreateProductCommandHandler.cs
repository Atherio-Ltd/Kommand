using Kommand.Abstractions;
using Kommand.Sample.Api.Commands.ProductCommands;
using Kommand.Sample.Api.Infrastructure;
using Kommand.Sample.Api.Models;
using Kommand.Sample.Api.Notifications;

namespace Kommand.Sample.Api.Handlers.ProductCommandHandlers;

/// <summary>
/// Handler for CreateProductCommand.
/// </summary>
public class CreateProductCommandHandler : ICommandHandler<CreateProductCommand, Product>
{
    private readonly IProductRepository _repository;
    private readonly IMediator _mediator;

    public CreateProductCommandHandler(IProductRepository repository, IMediator mediator)
    {
        _repository = repository;
        _mediator = mediator;
    }

    public async Task<Product> HandleAsync(CreateProductCommand command, CancellationToken cancellationToken)
    {
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = command.Name,
            Sku = command.Sku,
            Description = command.Description,
            Price = command.Price,
            StockQuantity = command.StockQuantity,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(product, cancellationToken);

        // Publish domain event
        await _mediator.PublishAsync(
            new ProductCreatedNotification(product.Id, product.Name, product.Sku),
            cancellationToken);

        return product;
    }
}
