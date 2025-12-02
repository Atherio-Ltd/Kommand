using Kommand.Abstractions;
using Kommand.Sample.Api.Infrastructure;
using Kommand.Sample.Api.Models;
using Kommand.Sample.Api.Queries;

namespace Kommand.Sample.Api.Handlers;

/// <summary>
/// Handler for GetProductByIdQuery.
/// </summary>
public class GetProductByIdQueryHandler : IQueryHandler<GetProductByIdQuery, Product?>
{
    private readonly IProductRepository _repository;

    public GetProductByIdQueryHandler(IProductRepository repository)
    {
        _repository = repository;
    }

    public async Task<Product?> HandleAsync(GetProductByIdQuery query, CancellationToken cancellationToken)
    {
        return await _repository.GetByIdAsync(query.ProductId, cancellationToken);
    }
}

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

/// <summary>
/// Handler for ListProductsQuery.
/// </summary>
public class ListProductsQueryHandler : IQueryHandler<ListProductsQuery, PagedResult<Product>>
{
    private readonly IProductRepository _repository;

    public ListProductsQueryHandler(IProductRepository repository)
    {
        _repository = repository;
    }

    public async Task<PagedResult<Product>> HandleAsync(ListProductsQuery query, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _repository.GetAllAsync(
            query.MinPrice,
            query.MaxPrice,
            query.InStockOnly,
            query.Page,
            query.PageSize,
            cancellationToken);

        return new PagedResult<Product>(items, totalCount, query.Page, query.PageSize);
    }
}
