using Kommand.Sample.Api.Models;

namespace Kommand.Sample.Api.Infrastructure;

/// <summary>
/// In-memory implementation of IUserRepository for demo purposes.
/// Uses a static list to persist data across requests (simulating a database).
/// In a real application, replace this with Entity Framework DbContext.
/// </summary>
public class InMemoryUserRepository : IUserRepository
{
    // Static list to persist data across scoped instances
    private static readonly List<User> Users = new();
    private static readonly object Lock = new();

    public Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        lock (Lock)
        {
            var user = Users.FirstOrDefault(u => u.Id == id);
            return Task.FromResult(user);
        }
    }

    public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        lock (Lock)
        {
            var user = Users.FirstOrDefault(u =>
                u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
            return Task.FromResult(user);
        }
    }

    public Task<(List<User> Items, int TotalCount)> GetAllAsync(
        bool? activeOnly = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        lock (Lock)
        {
            var query = Users.AsEnumerable();

            if (activeOnly.HasValue)
            {
                query = query.Where(u => u.IsActive == activeOnly.Value);
            }

            var totalCount = query.Count();
            var items = query
                .OrderByDescending(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return Task.FromResult((items, totalCount));
        }
    }

    public Task<User> AddAsync(User user, CancellationToken ct = default)
    {
        lock (Lock)
        {
            Users.Add(user);
            return Task.FromResult(user);
        }
    }

    public Task UpdateAsync(User user, CancellationToken ct = default)
    {
        lock (Lock)
        {
            var existing = Users.FirstOrDefault(u => u.Id == user.Id);
            if (existing != null)
            {
                existing.Email = user.Email;
                existing.Name = user.Name;
                existing.PhoneNumber = user.PhoneNumber;
                existing.UpdatedAt = user.UpdatedAt;
                existing.IsActive = user.IsActive;
            }

            return Task.CompletedTask;
        }
    }

    public Task<bool> EmailExistsAsync(string email, CancellationToken ct = default)
    {
        lock (Lock)
        {
            var exists = Users.Any(u =>
                u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
            return Task.FromResult(exists);
        }
    }
}
