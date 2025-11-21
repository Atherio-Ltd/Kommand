namespace Kommand;

/// <summary>
/// Result of a validation operation, indicating success or failure with detailed error information.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="ValidationResult"/> is returned by validators to indicate whether validation
/// passed or failed. When validation fails, it contains a collection of all validation errors
/// that occurred.
/// </para>
/// <para>
/// <strong>Design:</strong><br/>
/// This type uses init-only properties to provide immutability while allowing object initializer syntax.
/// Use the static factory methods <see cref="Success"/> and <see cref="Failure(ValidationError[])"/> to create instances.
/// </para>
/// <para>
/// <strong>Error Collection Strategy:</strong><br/>
/// Validators should collect ALL validation errors before returning, rather than failing fast
/// on the first error. This provides a better user experience by showing all issues at once.
/// </para>
/// </remarks>
/// <example>
/// <strong>Example: Successful Validation</strong>
/// <code>
/// public Task&lt;ValidationResult&gt; ValidateAsync(
///     MyCommand command,
///     CancellationToken cancellationToken)
/// {
///     // All validation checks passed
///     return Task.FromResult(ValidationResult.Success());
/// }
/// </code>
///
/// <strong>Example: Failed Validation with Single Error</strong>
/// <code>
/// public Task&lt;ValidationResult&gt; ValidateAsync(
///     MyCommand command,
///     CancellationToken cancellationToken)
/// {
///     if (string.IsNullOrWhiteSpace(command.Name))
///     {
///         return Task.FromResult(ValidationResult.Failure(
///             new ValidationError("Name", "Name is required")));
///     }
///
///     return Task.FromResult(ValidationResult.Success());
/// }
/// </code>
///
/// <strong>Example: Failed Validation with Multiple Errors</strong>
/// <code>
/// public Task&lt;ValidationResult&gt; ValidateAsync(
///     CreateUserCommand command,
///     CancellationToken cancellationToken)
/// {
///     var errors = new List&lt;ValidationError&gt;();
///
///     // Collect all errors (not fail-fast)
///     if (string.IsNullOrWhiteSpace(command.Email))
///         errors.Add(new ValidationError("Email", "Email is required"));
///
///     if (string.IsNullOrWhiteSpace(command.Name))
///         errors.Add(new ValidationError("Name", "Name is required"));
///
///     if (command.Age &lt; 18)
///         errors.Add(new ValidationError("Age", "Must be 18 or older"));
///
///     // Return all errors at once
///     return Task.FromResult(errors.Any()
///         ? ValidationResult.Failure(errors.ToArray())
///         : ValidationResult.Success());
/// }
/// </code>
///
/// <strong>Example: Checking Validation Result</strong>
/// <code>
/// var result = await validator.ValidateAsync(command, cancellationToken);
///
/// if (!result.IsValid)
/// {
///     foreach (var error in result.Errors)
///     {
///         Console.WriteLine($"{error.PropertyName}: {error.ErrorMessage}");
///     }
/// }
/// </code>
/// </example>
public class ValidationResult
{
    /// <summary>
    /// Gets a value indicating whether the validation succeeded.
    /// </summary>
    /// <value>
    /// <c>true</c> if validation passed; <c>false</c> if validation failed.
    /// </value>
    public bool IsValid { get; init; }

    /// <summary>
    /// Gets the collection of validation errors.
    /// </summary>
    /// <value>
    /// A read-only list of <see cref="ValidationError"/> instances.
    /// Empty if <see cref="IsValid"/> is <c>true</c>.
    /// </value>
    public IReadOnlyList<ValidationError> Errors { get; init; } = Array.Empty<ValidationError>();

    /// <summary>
    /// Creates a successful validation result with no errors.
    /// </summary>
    /// <returns>A <see cref="ValidationResult"/> with <see cref="IsValid"/> = <c>true</c></returns>
    /// <example>
    /// <code>
    /// public Task&lt;ValidationResult&gt; ValidateAsync(MyCommand command, CancellationToken ct)
    /// {
    ///     // All checks passed
    ///     return Task.FromResult(ValidationResult.Success());
    /// }
    /// </code>
    /// </example>
    public static ValidationResult Success() => new() { IsValid = true };

    /// <summary>
    /// Creates a failed validation result with the specified errors.
    /// </summary>
    /// <param name="errors">One or more validation errors that occurred</param>
    /// <returns>A <see cref="ValidationResult"/> with <see cref="IsValid"/> = <c>false</c></returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="errors"/> is null</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="errors"/> is empty</exception>
    /// <remarks>
    /// At least one error must be provided. Use <see cref="Success"/> for validation that passed.
    /// </remarks>
    /// <example>
    /// <strong>Example: Single Error</strong>
    /// <code>
    /// return ValidationResult.Failure(
    ///     new ValidationError("Email", "Email is required"));
    /// </code>
    ///
    /// <strong>Example: Multiple Errors</strong>
    /// <code>
    /// return ValidationResult.Failure(
    ///     new ValidationError("Email", "Email is required"),
    ///     new ValidationError("Name", "Name is required"),
    ///     new ValidationError("Age", "Age must be 18 or older"));
    /// </code>
    /// </example>
    public static ValidationResult Failure(params ValidationError[] errors)
    {
        if (errors == null)
            throw new ArgumentNullException(nameof(errors));

        if (errors.Length == 0)
            throw new ArgumentException("At least one validation error must be provided.", nameof(errors));

        return new ValidationResult
        {
            IsValid = false,
            Errors = errors
        };
    }

    /// <summary>
    /// Creates a failed validation result from a collection of errors.
    /// </summary>
    /// <param name="errors">A collection of validation errors</param>
    /// <returns>A <see cref="ValidationResult"/> with <see cref="IsValid"/> = <c>false</c></returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="errors"/> is null</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="errors"/> is empty</exception>
    /// <example>
    /// <code>
    /// var errorList = new List&lt;ValidationError&gt;
    /// {
    ///     new ValidationError("Email", "Email is required"),
    ///     new ValidationError("Name", "Name is required")
    /// };
    ///
    /// return ValidationResult.Failure(errorList);
    /// </code>
    /// </example>
    public static ValidationResult Failure(IEnumerable<ValidationError> errors)
    {
        if (errors == null)
            throw new ArgumentNullException(nameof(errors));

        var errorArray = errors.ToArray();

        if (errorArray.Length == 0)
            throw new ArgumentException("At least one validation error must be provided.", nameof(errors));

        return new ValidationResult
        {
            IsValid = false,
            Errors = errorArray
        };
    }
}
