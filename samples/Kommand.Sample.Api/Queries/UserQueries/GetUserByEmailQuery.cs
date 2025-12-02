using Kommand.Abstractions;
using Kommand.Sample.Api.Models;

namespace Kommand.Sample.Api.Queries.UserQueries;

/// <summary>
/// Query to retrieve a user by email.
/// </summary>
public record GetUserByEmailQuery(string Email) : IQuery<User?>;
