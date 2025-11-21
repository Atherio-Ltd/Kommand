namespace Kommand;

/// <summary>
/// Represents a single validation error for a specific property.
/// </summary>
/// <param name="PropertyName">The name of the property that failed validation</param>
/// <param name="ErrorMessage">A human-readable description of the validation error</param>
/// <param name="ErrorCode">Optional error code for categorizing errors (e.g., "REQUIRED", "INVALID_FORMAT", "DUPLICATE")</param>
/// <remarks>
/// <para>
/// <see cref="ValidationError"/> is an immutable record that represents a single validation failure.
/// Multiple errors can be collected and returned in a <see cref="ValidationResult"/>.
/// </para>
/// <para>
/// <strong>Best Practices:</strong>
/// <list type="bullet">
/// <item><description><strong>PropertyName:</strong> Use the exact property name from the request (e.g., "Email", "FirstName")</description></item>
/// <item><description><strong>ErrorMessage:</strong> Provide clear, actionable error messages for end users</description></item>
/// <item><description><strong>ErrorCode:</strong> Use consistent codes across validators for easier client-side handling</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <strong>Example: Basic Validation Errors</strong>
/// <code>
/// var errors = new List&lt;ValidationError&gt;
/// {
///     new ValidationError("Email", "Email is required"),
///     new ValidationError("Email", "Email must be a valid email address"),
///     new ValidationError("Age", "Age must be between 18 and 120")
/// };
/// </code>
///
/// <strong>Example: Errors with Error Codes</strong>
/// <code>
/// var errors = new List&lt;ValidationError&gt;
/// {
///     new ValidationError("Email", "Email is required", "REQUIRED"),
///     new ValidationError("Email", "Email must be valid", "INVALID_FORMAT"),
///     new ValidationError("Username", "Username already exists", "DUPLICATE")
/// };
///
/// // Client-side handling
/// var requiredErrors = errors.Where(e => e.ErrorCode == "REQUIRED");
/// </code>
///
/// <strong>Example: Creating from Validation Logic</strong>
/// <code>
/// public async Task&lt;ValidationResult&gt; ValidateAsync(
///     CreateUserCommand command,
///     CancellationToken cancellationToken)
/// {
///     var errors = new List&lt;ValidationError&gt;();
///
///     if (string.IsNullOrWhiteSpace(command.Email))
///         errors.Add(new ValidationError("Email", "Email is required", "REQUIRED"));
///     else if (!IsValidEmail(command.Email))
///         errors.Add(new ValidationError("Email", "Email format is invalid", "INVALID_FORMAT"));
///
///     return errors.Any()
///         ? ValidationResult.Failure(errors.ToArray())
///         : ValidationResult.Success();
/// }
/// </code>
/// </example>
public record ValidationError(string PropertyName, string ErrorMessage, string? ErrorCode = null);
