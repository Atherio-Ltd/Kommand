using Kommand.Abstractions;
using Kommand.Sample.Api.Infrastructure;
using Kommand.Sample.Api.Models;
using Kommand.Sample.Api.Queries.ProductQueries;

namespace Kommand.Sample.Api.Handlers.ProductQueryHandlers;

/// <summary>
/// Handler for GetProductBySkuQuery.
/// </summary>
public class GetProductBySkuQueryHandler : IQueryHandler<GetProductBySkuQuery, Product?>
{
    private readonly IProductRepository _repository;

    public GetProductBySkuQueryHandler(IProductRepository repository)
    {
        _repository = repository;
    }

    public async Task<Product?> HandleAsync(GetProductBySkuQuery query, CancellationToken cancellationToken)
    {
        return await _repository.GetBySkuAsync(query.Sku, cancellationToken);
    }
}
