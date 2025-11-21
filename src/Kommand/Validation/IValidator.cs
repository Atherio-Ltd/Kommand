namespace Kommand;

/// <summary>
/// Validator interface for validating requests before handler execution.
/// Validators are automatically discovered during assembly scanning and executed by ValidationInterceptor.
/// </summary>
/// <typeparam name="T">The type of request to validate (contravariant to support validation of base types)</typeparam>
/// <remarks>
/// <para>
/// Validators provide a way to implement validation logic without external dependencies like FluentValidation.
/// They are executed automatically when <c>WithValidation()</c> is called during Kommand configuration.
/// </para>
/// <para>
/// <strong>Key Features:</strong>
/// <list type="bullet">
/// <item><description><strong>Auto-Discovery:</strong> Validators are automatically registered when <c>RegisterHandlersFromAssembly()</c> is called</description></item>
/// <item><description><strong>Dependency Injection:</strong> Validators can inject services like repositories, DbContext, or external APIs</description></item>
/// <item><description><strong>Async Support:</strong> Validation can be asynchronous, enabling database queries or API calls</description></item>
/// <item><description><strong>Scoped Lifetime:</strong> Validators are registered as Scoped, allowing them to participate in request-scoped operations</description></item>
/// <item><description><strong>Error Collection:</strong> All validators for a request type run sequentially, collecting all errors before throwing</description></item>
/// </list>
/// </para>
/// <para>
/// <strong>Execution Order:</strong><br/>
/// When validation is enabled:
/// <list type="number">
/// <item><description>All validators for the request type are resolved from DI</description></item>
/// <item><description>Each validator executes sequentially (not in parallel)</description></item>
/// <item><description>All errors from all validators are collected</description></item>
/// <item><description>If any errors exist, ValidationException is thrown with all errors</description></item>
/// <item><description>If validation passes, the handler executes normally</description></item>
/// </list>
/// </para>
/// <para>
/// <strong>Registration:</strong><br/>
/// Validators are registered automatically during assembly scanning:
/// <code>
/// services.AddKommand(config =>
/// {
///     // This discovers both handlers AND validators
///     config.RegisterHandlersFromAssembly(typeof(Program).Assembly);
///
///     // This adds ValidationInterceptor to the pipeline
///     config.WithValidation();
/// });
/// </code>
/// </para>
/// </remarks>
/// <example>
/// <strong>Example: Simple Validator</strong>
/// <code>
/// public record CreateUserCommand(string Email, string Name) : ICommand&lt;User&gt;;
///
/// public class CreateUserCommandValidator : IValidator&lt;CreateUserCommand&gt;
/// {
///     public Task&lt;ValidationResult&gt; ValidateAsync(
///         CreateUserCommand command,
///         CancellationToken cancellationToken)
///     {
///         var errors = new List&lt;ValidationError&gt;();
///
///         if (string.IsNullOrWhiteSpace(command.Email))
///             errors.Add(new ValidationError("Email", "Email is required"));
///
///         if (!command.Email.Contains("@"))
///             errors.Add(new ValidationError("Email", "Email must be valid"));
///
///         if (string.IsNullOrWhiteSpace(command.Name))
///             errors.Add(new ValidationError("Name", "Name is required"));
///
///         return Task.FromResult(errors.Any()
///             ? ValidationResult.Failure(errors.ToArray())
///             : ValidationResult.Success());
///     }
/// }
/// </code>
///
/// <strong>Example: Async Validator with Database Access</strong>
/// <code>
/// public class CreateUserCommandValidator : IValidator&lt;CreateUserCommand&gt;
/// {
///     private readonly IUserRepository _repository;
///
///     // Validators can inject dependencies!
///     public CreateUserCommandValidator(IUserRepository repository)
///     {
///         _repository = repository;
///     }
///
///     public async Task&lt;ValidationResult&gt; ValidateAsync(
///         CreateUserCommand command,
///         CancellationToken cancellationToken)
///     {
///         var errors = new List&lt;ValidationError&gt;();
///
///         // Synchronous validation
///         if (string.IsNullOrWhiteSpace(command.Email))
///             errors.Add(new ValidationError("Email", "Email is required"));
///
///         // Asynchronous validation - check database
///         if (await _repository.EmailExistsAsync(command.Email, cancellationToken))
///             errors.Add(new ValidationError("Email", "Email already in use"));
///
///         return errors.Any()
///             ? ValidationResult.Failure(errors.ToArray())
///             : ValidationResult.Success();
///     }
/// }
/// </code>
///
/// <strong>Example: Multiple Validators for Same Request</strong>
/// <code>
/// // You can have multiple validators for the same request type.
/// // All validators will execute and their errors will be combined.
///
/// public class CreateUserBusinessRulesValidator : IValidator&lt;CreateUserCommand&gt;
/// {
///     public Task&lt;ValidationResult&gt; ValidateAsync(
///         CreateUserCommand command,
///         CancellationToken cancellationToken)
///     {
///         var errors = new List&lt;ValidationError&gt;();
///
///         // Business rule: Name must not contain numbers
///         if (command.Name.Any(char.IsDigit))
///             errors.Add(new ValidationError("Name", "Name cannot contain numbers"));
///
///         return Task.FromResult(errors.Any()
///             ? ValidationResult.Failure(errors.ToArray())
///             : ValidationResult.Success());
///     }
/// }
///
/// // Both CreateUserCommandValidator and CreateUserBusinessRulesValidator
/// // will execute, and all errors will be collected before throwing ValidationException.
/// </code>
///
/// <strong>Example: Handling Validation Errors</strong>
/// <code>
/// var mediator = serviceProvider.GetRequiredService&lt;IMediator&gt;();
///
/// try
/// {
///     var user = await mediator.SendAsync(
///         new CreateUserCommand("invalid-email", ""),
///         cancellationToken);
/// }
/// catch (ValidationException ex)
/// {
///     // ex.Errors contains all validation errors from all validators
///     foreach (var error in ex.Errors)
///     {
///         Console.WriteLine($"{error.PropertyName}: {error.ErrorMessage}");
///     }
///     // Output:
///     // Email: Email must be valid
///     // Name: Name is required
/// }
/// </code>
/// </example>
public interface IValidator<in T>
{
    /// <summary>
    /// Validates the specified instance asynchronously.
    /// </summary>
    /// <param name="instance">The instance to validate (typically a command or query)</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>
    /// A <see cref="ValidationResult"/> indicating success or failure.
    /// If validation fails, the result contains all validation errors.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method is called by ValidationInterceptor before
    /// the handler executes. If this method returns a result with <c>IsValid = false</c>,
    /// the errors are collected along with errors from other validators for the same request type.
    /// </para>
    /// <para>
    /// If any validator returns errors, <see cref="ValidationException"/> is thrown with all
    /// errors, and the handler is not executed (short-circuit behavior).
    /// </para>
    /// <para>
    /// Because validators are registered as Scoped, this method can safely use request-scoped
    /// dependencies like DbContext or repositories.
    /// </para>
    /// </remarks>
    Task<ValidationResult> ValidateAsync(T instance, CancellationToken cancellationToken);
}
