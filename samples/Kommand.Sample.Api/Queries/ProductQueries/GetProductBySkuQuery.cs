using Kommand.Abstractions;
using Kommand.Sample.Api.Models;

namespace Kommand.Sample.Api.Queries.ProductQueries;

/// <summary>
/// Query to retrieve a product by SKU.
/// </summary>
public record GetProductBySkuQuery(string Sku) : IQuery<Product?>;
