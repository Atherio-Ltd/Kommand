using Kommand.Abstractions;
using Kommand.Sample.Models;

namespace Kommand.Sample.Queries;

/// <summary>
/// Query to retrieve all users.
/// This demonstrates a query returning a collection.
/// </summary>
public record ListUsersQuery : IQuery<List<User>>;
