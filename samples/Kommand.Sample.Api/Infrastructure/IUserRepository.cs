using Kommand.Sample.Api.Models;

namespace Kommand.Sample.Api.Infrastructure;

/// <summary>
/// Repository interface for user data access.
/// In a real application, this would be implemented with Entity Framework or another ORM.
/// </summary>
public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<(List<User> Items, int TotalCount)> GetAllAsync(
        bool? activeOnly = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default);
    Task<User> AddAsync(User user, CancellationToken ct = default);
    Task UpdateAsync(User user, CancellationToken ct = default);
    Task<bool> EmailExistsAsync(string email, CancellationToken ct = default);
}
