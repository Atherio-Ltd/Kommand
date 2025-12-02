namespace Kommand.Sample.Api.DTOs;

/// <summary>
/// Request DTO for creating a new user.
/// </summary>
public record CreateUserRequest(
    string Email,
    string Name,
    string? PhoneNumber = null);

/// <summary>
/// Request DTO for updating an existing user.
/// </summary>
public record UpdateUserRequest(
    string Name,
    string? PhoneNumber = null);

/// <summary>
/// Response DTO representing a user.
/// </summary>
public record UserResponse(
    Guid Id,
    string Email,
    string Name,
    string? PhoneNumber,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    bool IsActive);
