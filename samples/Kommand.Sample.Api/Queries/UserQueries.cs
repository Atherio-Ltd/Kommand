using Kommand.Abstractions;
using Kommand.Sample.Api.Models;

namespace Kommand.Sample.Api.Queries;

/// <summary>
/// Query to retrieve a single user by ID.
/// Demonstrates a query returning a single object (or null).
/// </summary>
public record GetUserByIdQuery(Guid UserId) : IQuery<User?>;

/// <summary>
/// Query to retrieve a user by email.
/// </summary>
public record GetUserByEmailQuery(string Email) : IQuery<User?>;

/// <summary>
/// Query to retrieve all users with optional filtering.
/// Demonstrates a query returning a collection.
/// </summary>
public record ListUsersQuery(
    bool? ActiveOnly = null,
    int Page = 1,
    int PageSize = 20) : IQuery<PagedResult<User>>;

/// <summary>
/// Represents a paginated result.
/// </summary>
public record PagedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
}
