using Kommand;
using Kommand.Abstractions;
using Kommand.Sample.Api.Commands.ProductCommands;
using Kommand.Sample.Api.Infrastructure;

namespace Kommand.Sample.Api.Handlers.ProductCommandHandlers;

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
