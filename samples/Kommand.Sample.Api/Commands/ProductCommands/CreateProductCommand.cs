using Kommand.Abstractions;
using Kommand.Sample.Api.Models;

namespace Kommand.Sample.Api.Commands.ProductCommands;

/// <summary>
/// Command to create a new product.
/// Demonstrates a command with multiple validation rules.
/// </summary>
public record CreateProductCommand(
    string Name,
    string Sku,
    string? Description,
    decimal Price,
    int StockQuantity) : ICommand<Product>;
