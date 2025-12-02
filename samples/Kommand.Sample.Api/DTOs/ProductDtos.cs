namespace Kommand.Sample.Api.DTOs;

/// <summary>
/// Request DTO for creating a new product.
/// </summary>
public record CreateProductRequest(
    string Name,
    string Sku,
    string? Description,
    decimal Price,
    int StockQuantity = 0);

/// <summary>
/// Request DTO for updating an existing product.
/// </summary>
public record UpdateProductRequest(
    string Name,
    string? Description,
    decimal Price,
    int StockQuantity);

/// <summary>
/// Response DTO representing a product.
/// </summary>
public record ProductResponse(
    Guid Id,
    string Name,
    string Sku,
    string? Description,
    decimal Price,
    int StockQuantity,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    bool IsActive);
