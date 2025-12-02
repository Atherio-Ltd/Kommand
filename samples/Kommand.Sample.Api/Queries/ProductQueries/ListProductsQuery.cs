using Kommand.Abstractions;
using Kommand.Sample.Api.Models;

namespace Kommand.Sample.Api.Queries.ProductQueries;

/// <summary>
/// Query to retrieve all products with optional filtering.
/// </summary>
public record ListProductsQuery(
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    bool? InStockOnly = null,
    int Page = 1,
    int PageSize = 20) : IQuery<PagedResult<Product>>;
