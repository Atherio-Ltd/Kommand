using Kommand.Abstractions;
using Kommand.Sample.Api.Models;

namespace Kommand.Sample.Api.Queries;

/// <summary>
/// Query to retrieve a single product by ID.
/// </summary>
public record GetProductByIdQuery(Guid ProductId) : IQuery<Product?>;

/// <summary>
/// Query to retrieve a product by SKU.
/// </summary>
public record GetProductBySkuQuery(string Sku) : IQuery<Product?>;

/// <summary>
/// Query to retrieve all products with optional filtering.
/// </summary>
public record ListProductsQuery(
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    bool? InStockOnly = null,
    int Page = 1,
    int PageSize = 20) : IQuery<PagedResult<Product>>;
