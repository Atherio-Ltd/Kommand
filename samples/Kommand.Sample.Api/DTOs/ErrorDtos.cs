namespace Kommand.Sample.Api.DTOs;

/// <summary>
/// Standard error response for API errors.
/// </summary>
public record ErrorResponse(
    string Title,
    int Status,
    string? Detail = null,
    IDictionary<string, string[]>? Errors = null);
