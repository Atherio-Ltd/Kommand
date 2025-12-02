using Kommand.Abstractions;

namespace Kommand.Sample.Api.Commands.ProductCommands;

/// <summary>
/// Command to update an existing product.
/// </summary>
public record UpdateProductCommand(
    Guid ProductId,
    string Name,
    string? Description,
    decimal Price,
    int StockQuantity) : ICommand;
