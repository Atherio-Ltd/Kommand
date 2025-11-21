using Kommand.Abstractions;
using Kommand.Sample.Models;

namespace Kommand.Sample.Queries;

/// <summary>
/// Query to retrieve a single user by ID.
/// This demonstrates a query returning a single object (or null).
/// </summary>
public record GetUserQuery(Guid UserId) : IQuery<User?>;
