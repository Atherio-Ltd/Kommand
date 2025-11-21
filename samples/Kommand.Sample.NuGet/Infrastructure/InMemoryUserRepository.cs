using Kommand.Sample.Models;

namespace Kommand.Sample.Infrastructure;

/// <summary>
/// In-memory implementation of IUserRepository for demo purposes.
/// In a real application, replace this with Entity Framework DbContext.
/// </summary>
public class InMemoryUserRepository : IUserRepository
{
    private readonly List<User> _users = new();
    private readonly object _lock = new();

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var user = _users.FirstOrDefault(u => u.Id == id);
            return Task.FromResult(user);
        }
    }

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var user = _users.FirstOrDefault(u =>
                u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
            return Task.FromResult(user);
        }
    }

    public Task<List<User>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            // Return a copy to avoid external modification
            return Task.FromResult(_users.ToList());
        }
    }

    public Task<User> AddAsync(User user, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            _users.Add(user);
            return Task.FromResult(user);
        }
    }

    public Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var existing = _users.FirstOrDefault(u => u.Id == user.Id);
            if (existing != null)
            {
                existing.Email = user.Email;
                existing.Name = user.Name;
                existing.UpdatedAt = user.UpdatedAt;
                existing.IsActive = user.IsActive;
            }
            return Task.CompletedTask;
        }
    }

    public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var exists = _users.Any(u =>
                u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
            return Task.FromResult(exists);
        }
    }
}
