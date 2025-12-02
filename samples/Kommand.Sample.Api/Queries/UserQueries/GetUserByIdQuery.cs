using Kommand.Abstractions;
using Kommand.Sample.Api.Models;

namespace Kommand.Sample.Api.Queries.UserQueries;

/// <summary>
/// Query to retrieve a single user by ID.
/// Demonstrates a query returning a single object (or null).
/// </summary>
public record GetUserByIdQuery(Guid UserId) : IQuery<User?>;
