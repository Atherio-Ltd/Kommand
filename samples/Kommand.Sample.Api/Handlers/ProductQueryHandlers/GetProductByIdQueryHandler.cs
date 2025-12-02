using Kommand.Abstractions;
using Kommand.Sample.Api.Infrastructure;
using Kommand.Sample.Api.Models;
using Kommand.Sample.Api.Queries.ProductQueries;

namespace Kommand.Sample.Api.Handlers.ProductQueryHandlers;

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
