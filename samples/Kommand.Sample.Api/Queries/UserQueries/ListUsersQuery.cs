using Kommand.Abstractions;
using Kommand.Sample.Api.Models;

namespace Kommand.Sample.Api.Queries.UserQueries;

/// <summary>
/// Query to retrieve all users with optional filtering.
/// Demonstrates a query returning a collection.
/// </summary>
public record ListUsersQuery(
    bool? ActiveOnly = null,
    int Page = 1,
    int PageSize = 20) : IQuery<PagedResult<User>>;
