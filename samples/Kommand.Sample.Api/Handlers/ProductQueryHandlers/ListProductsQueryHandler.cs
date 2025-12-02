using Kommand.Abstractions;
using Kommand.Sample.Api.Infrastructure;
using Kommand.Sample.Api.Models;
using Kommand.Sample.Api.Queries;
using Kommand.Sample.Api.Queries.ProductQueries;

namespace Kommand.Sample.Api.Handlers.ProductQueryHandlers;

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
