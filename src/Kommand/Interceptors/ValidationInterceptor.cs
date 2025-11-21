namespace Kommand;

using Kommand.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

/// <summary>
/// Built-in interceptor that automatically runs all registered validators for a request.
/// Validates requests before handler execution, collecting all errors from all validators.
/// </summary>
/// <typeparam name="TRequest">The type of request being validated (must implement IRequest&lt;TResponse&gt;)</typeparam>
/// <typeparam name="TResponse">The type of response returned by the handler</typeparam>
/// <remarks>
/// <para>
/// ValidationInterceptor is the core component of Kommand's validation system. When enabled via
/// <c>config.WithValidation()</c>, it automatically intercepts all requests and runs validators
/// before the handler executes.
/// </para>
/// <para>
/// <strong>How It Works:</strong>
/// <list type="number">
/// <item><description>Resolves all validators for the request type from DI (via <c>IEnumerable&lt;IValidator&lt;TRequest&gt;&gt;</c>)</description></item>
/// <item><description>If no validators are registered, skips validation and calls the handler immediately</description></item>
/// <item><description>If validators exist, executes them sequentially (not in parallel)</description></item>
/// <item><description>Collects ALL errors from ALL validators (not fail-fast)</description></item>
/// <item><description>If any errors exist, throws <see cref="ValidationException"/> with all errors (short-circuits handler)</description></item>
/// <item><description>If validation passes, calls the handler normally</description></item>
/// </list>
/// </para>
/// <para>
/// <strong>Error Collection Strategy:</strong><br/>
/// This interceptor does NOT fail fast. It runs all validators even if early validators fail,
/// collecting all validation errors before throwing. This provides a better user experience
/// by showing all validation issues at once, rather than forcing users to fix errors one at a time.
/// </para>
/// <para>
/// <strong>Validator Discovery:</strong><br/>
/// Validators are automatically discovered during assembly scanning when you call
/// <c>config.RegisterHandlersFromAssembly()</c>. They are registered as open generic types
/// (<c>IValidator&lt;&gt;</c>), and the DI container resolves all validators for each specific request type.
/// </para>
/// <para>
/// <strong>Logging:</strong><br/>
/// This interceptor logs validation failures at two levels:
/// <list type="bullet">
/// <item><description><strong>Warning:</strong> Each individual validator that fails (with error count)</description></item>
/// <item><description><strong>Error:</strong> Total validation failure for the request (with total error count)</description></item>
/// </list>
/// </para>
/// <para>
/// <strong>Performance:</strong><br/>
/// When no validators are registered for a request type, this interceptor has minimal overhead
/// (just an empty collection check). This allows you to enable validation globally without
/// performance impact on requests that don't need validation.
/// </para>
/// <para>
/// <strong>Registration:</strong><br/>
/// This interceptor is registered automatically when you call <c>config.WithValidation()</c>:
/// <code>
/// services.AddKommand(config =>
/// {
///     config.RegisterHandlersFromAssembly(typeof(Program).Assembly);
///     config.WithValidation(); // Adds ValidationInterceptor to pipeline
/// });
/// </code>
/// </para>
/// </remarks>
/// <example>
/// <strong>Example: Validation Interceptor in Action</strong>
/// <code>
/// // Setup
/// services.AddKommand(config =>
/// {
///     config.RegisterHandlersFromAssembly(typeof(Program).Assembly);
///     config.WithValidation();
/// });
///
/// var mediator = serviceProvider.GetRequiredService&lt;IMediator&gt;();
///
/// // When you send a command with validation enabled:
/// try
/// {
///     var result = await mediator.SendAsync(
///         new CreateUserCommand("", "John"), // Invalid email
///         cancellationToken);
/// }
/// catch (ValidationException ex)
/// {
///     // ValidationInterceptor collected all errors from all validators
///     foreach (var error in ex.Errors)
///     {
///         Console.WriteLine($"{error.PropertyName}: {error.ErrorMessage}");
///     }
/// }
/// </code>
///
/// <strong>Example: Multiple Validators Executing</strong>
/// <code>
/// // Validator 1: Basic validation
/// public class CreateUserBasicValidator : IValidator&lt;CreateUserCommand&gt;
/// {
///     public Task&lt;ValidationResult&gt; ValidateAsync(
///         CreateUserCommand command,
///         CancellationToken ct)
///     {
///         var errors = new List&lt;ValidationError&gt;();
///         if (string.IsNullOrWhiteSpace(command.Email))
///             errors.Add(new ValidationError("Email", "Email is required"));
///         return Task.FromResult(errors.Any()
///             ? ValidationResult.Failure(errors.ToArray())
///             : ValidationResult.Success());
///     }
/// }
///
/// // Validator 2: Business rules
/// public class CreateUserBusinessValidator : IValidator&lt;CreateUserCommand&gt;
/// {
///     private readonly IUserRepository _repo;
///
///     public CreateUserBusinessValidator(IUserRepository repo) => _repo = repo;
///
///     public async Task&lt;ValidationResult&gt; ValidateAsync(
///         CreateUserCommand command,
///         CancellationToken ct)
///     {
///         var errors = new List&lt;ValidationError&gt;();
///         if (await _repo.EmailExistsAsync(command.Email, ct))
///             errors.Add(new ValidationError("Email", "Email already in use"));
///         return errors.Any()
///             ? ValidationResult.Failure(errors.ToArray())
///             : ValidationResult.Success();
///     }
/// }
///
/// // ValidationInterceptor will execute BOTH validators and combine their errors
/// // before throwing ValidationException (if any errors exist).
/// </code>
/// </example>
public sealed class ValidationInterceptor<TRequest, TResponse> : IInterceptor<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;
    private readonly ILogger<ValidationInterceptor<TRequest, TResponse>> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationInterceptor{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="validators">All validators registered for this request type (resolved from DI)</param>
    /// <param name="logger">Optional logger for validation failures. If null, uses NullLogger.</param>
    /// <exception cref="ArgumentNullException">Thrown if validators is null</exception>
    /// <remarks>
    /// The validators parameter is typically injected by the DI container as
    /// <c>IEnumerable&lt;IValidator&lt;TRequest&gt;&gt;</c>. If no validators are registered
    /// for the request type, this will be an empty collection (not null).
    /// The logger parameter is optional and defaults to NullLogger if not provided by DI.
    /// </remarks>
    public ValidationInterceptor(
        IEnumerable<IValidator<TRequest>> validators,
        ILogger<ValidationInterceptor<TRequest, TResponse>>? logger = null)
    {
        _validators = validators ?? throw new ArgumentNullException(nameof(validators));
        _logger = logger ?? NullLogger<ValidationInterceptor<TRequest, TResponse>>.Instance;
    }

    /// <summary>
    /// Executes all validators for the request, then calls the handler if validation passes.
    /// </summary>
    /// <param name="request">The request to validate and handle</param>
    /// <param name="next">The next delegate in the pipeline (typically the handler)</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>The response from the handler if validation passes</returns>
    /// <exception cref="ValidationException">
    /// Thrown if any validator returns errors. Contains all errors from all validators.
    /// </exception>
    /// <remarks>
    /// <para>
    /// <strong>Execution Flow:</strong>
    /// <list type="number">
    /// <item><description>Check if any validators are registered (skip if empty)</description></item>
    /// <item><description>Execute each validator sequentially</description></item>
    /// <item><description>Collect errors from all validators</description></item>
    /// <item><description>Log warnings for each failed validator</description></item>
    /// <item><description>If any errors, log error and throw ValidationException</description></item>
    /// <item><description>If no errors, call next() to execute handler</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// This method does NOT execute validators in parallel. Sequential execution ensures
    /// predictable order and avoids potential race conditions with shared dependencies.
    /// </para>
    /// </remarks>
    public async Task<TResponse> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Skip validation if no validators are registered for this request type
        // This provides zero overhead for requests that don't need validation
        if (!_validators.Any())
        {
            return await next();
        }

        var errors = new List<ValidationError>();

        // Execute all validators sequentially
        // We don't fail fast - we want to collect ALL errors from ALL validators
        foreach (var validator in _validators)
        {
            var result = await validator.ValidateAsync(request, cancellationToken);

            if (!result.IsValid)
            {
                errors.AddRange(result.Errors);

                _logger.LogWarning(
                    "Validation failed for {RequestType} with validator {ValidatorType}: {ErrorCount} error(s)",
                    typeof(TRequest).Name,
                    validator.GetType().Name,
                    result.Errors.Count);
            }
        }

        // If any errors were collected, throw ValidationException (short-circuit handler)
        if (errors.Count > 0)
        {
            _logger.LogError(
                "Validation failed for {RequestType} with {ErrorCount} total error(s) from {ValidatorCount} validator(s)",
                typeof(TRequest).Name,
                errors.Count,
                _validators.Count());

            throw new ValidationException(errors.AsReadOnly());
        }

        // Validation passed, continue to handler
        return await next();
    }
}
