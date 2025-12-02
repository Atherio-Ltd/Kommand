using Kommand.Abstractions;
using Kommand.Sample.Api.Models;

namespace Kommand.Sample.Api.Commands;

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

/// <summary>
/// Command to update an existing product.
/// </summary>
public record UpdateProductCommand(
    Guid ProductId,
    string Name,
    string? Description,
    decimal Price,
    int StockQuantity) : ICommand;
