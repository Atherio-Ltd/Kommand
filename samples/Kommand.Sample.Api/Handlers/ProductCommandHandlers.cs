using Kommand;
using Kommand.Abstractions;
using Kommand.Sample.Api.Commands;
using Kommand.Sample.Api.Infrastructure;
using Kommand.Sample.Api.Models;
using Kommand.Sample.Api.Notifications;

namespace Kommand.Sample.Api.Handlers;

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

/// <summary>
/// Handler for UpdateProductCommand.
/// </summary>
public class UpdateProductCommandHandler : ICommandHandler<UpdateProductCommand, Unit>
{
    private readonly IProductRepository _repository;

    public UpdateProductCommandHandler(IProductRepository repository)
    {
        _repository = repository;
    }

    public async Task<Unit> HandleAsync(UpdateProductCommand command, CancellationToken cancellationToken)
    {
        var product = await _repository.GetByIdAsync(command.ProductId, cancellationToken)
            ?? throw new InvalidOperationException($"Product with ID {command.ProductId} not found");

        product.Name = command.Name;
        product.Description = command.Description;
        product.Price = command.Price;
        product.StockQuantity = command.StockQuantity;
        product.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(product, cancellationToken);

        return Unit.Value;
    }
}
