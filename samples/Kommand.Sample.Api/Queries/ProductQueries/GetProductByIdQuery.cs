using Kommand.Abstractions;
using Kommand.Sample.Api.Models;

namespace Kommand.Sample.Api.Queries.ProductQueries;

/// <summary>
/// Query to retrieve a single product by ID.
/// </summary>
public record GetProductByIdQuery(Guid ProductId) : IQuery<Product?>;
