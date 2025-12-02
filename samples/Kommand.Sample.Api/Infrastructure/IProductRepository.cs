using Kommand.Sample.Api.Models;

namespace Kommand.Sample.Api.Infrastructure;

/// <summary>
/// Repository interface for product data access.
/// </summary>
public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Product?> GetBySkuAsync(string sku, CancellationToken ct = default);
    Task<(List<Product> Items, int TotalCount)> GetAllAsync(
        decimal? minPrice = null,
        decimal? maxPrice = null,
        bool? inStockOnly = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default);
    Task<Product> AddAsync(Product product, CancellationToken ct = default);
    Task UpdateAsync(Product product, CancellationToken ct = default);
    Task<bool> SkuExistsAsync(string sku, CancellationToken ct = default);
}
