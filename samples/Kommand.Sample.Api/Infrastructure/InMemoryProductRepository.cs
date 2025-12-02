using Kommand.Sample.Api.Models;

namespace Kommand.Sample.Api.Infrastructure;

/// <summary>
/// In-memory implementation of IProductRepository for demo purposes.
/// </summary>
public class InMemoryProductRepository : IProductRepository
{
    private static readonly List<Product> Products = new();
    private static readonly object Lock = new();

    public Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        lock (Lock)
        {
            var product = Products.FirstOrDefault(p => p.Id == id);
            return Task.FromResult(product);
        }
    }

    public Task<Product?> GetBySkuAsync(string sku, CancellationToken ct = default)
    {
        lock (Lock)
        {
            var product = Products.FirstOrDefault(p =>
                p.Sku.Equals(sku, StringComparison.OrdinalIgnoreCase));
            return Task.FromResult(product);
        }
    }

    public Task<(List<Product> Items, int TotalCount)> GetAllAsync(
        decimal? minPrice = null,
        decimal? maxPrice = null,
        bool? inStockOnly = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        lock (Lock)
        {
            var query = Products.Where(p => p.IsActive);

            if (minPrice.HasValue)
            {
                query = query.Where(p => p.Price >= minPrice.Value);
            }

            if (maxPrice.HasValue)
            {
                query = query.Where(p => p.Price <= maxPrice.Value);
            }

            if (inStockOnly == true)
            {
                query = query.Where(p => p.StockQuantity > 0);
            }

            var totalCount = query.Count();
            var items = query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return Task.FromResult((items, totalCount));
        }
    }

    public Task<Product> AddAsync(Product product, CancellationToken ct = default)
    {
        lock (Lock)
        {
            Products.Add(product);
            return Task.FromResult(product);
        }
    }

    public Task UpdateAsync(Product product, CancellationToken ct = default)
    {
        lock (Lock)
        {
            var existing = Products.FirstOrDefault(p => p.Id == product.Id);
            if (existing != null)
            {
                existing.Name = product.Name;
                existing.Description = product.Description;
                existing.Price = product.Price;
                existing.StockQuantity = product.StockQuantity;
                existing.UpdatedAt = product.UpdatedAt;
                existing.IsActive = product.IsActive;
            }

            return Task.CompletedTask;
        }
    }

    public Task<bool> SkuExistsAsync(string sku, CancellationToken ct = default)
    {
        lock (Lock)
        {
            var exists = Products.Any(p =>
                p.Sku.Equals(sku, StringComparison.OrdinalIgnoreCase));
            return Task.FromResult(exists);
        }
    }
}
