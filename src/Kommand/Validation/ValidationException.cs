namespace Kommand;

/// <summary>
/// Exception thrown when validation fails for a request.
/// Contains all validation errors from all validators that ran for the request.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="ValidationException"/> is thrown by ValidationInterceptor
/// when one or more validators return validation errors. This exception contains the complete
/// collection of all errors from all validators that executed.
/// </para>
/// <para>
/// <strong>When This is Thrown:</strong><br/>
/// This exception is automatically thrown by the validation pipeline when:
/// <list type="bullet">
/// <item><description>Validation is enabled via <c>config.WithValidation()</c></description></item>
/// <item><description>One or more validators are registered for the request type</description></item>
/// <item><description>At least one validator returns a failed <see cref="ValidationResult"/></description></item>
/// </list>
/// </para>
/// <para>
/// <strong>Error Collection Strategy:</strong><br/>
/// The ValidationInterceptor executes ALL validators for a request type
/// and collects ALL errors before throwing this exception. This is not a fail-fast approach;
/// it ensures that users see all validation issues at once.
/// </para>
/// <para>
/// <strong>Handling in ASP.NET Core:</strong><br/>
/// You can catch this exception in middleware to return appropriate HTTP responses:
/// <code>
/// app.Use(async (context, next) =>
/// {
///     try
///     {
///         await next(context);
///     }
///     catch (ValidationException ex)
///     {
///         context.Response.StatusCode = 400; // Bad Request
///         var errors = ex.Errors.Select(e => new
///         {
///             property = e.PropertyName,
///             message = e.ErrorMessage,
///             code = e.ErrorCode
///         });
///         await context.Response.WriteAsJsonAsync(new { errors });
///     }
/// });
/// </code>
/// </para>
/// </remarks>
/// <example>
/// <strong>Example: Catching Validation Exception</strong>
/// <code>
/// var mediator = serviceProvider.GetRequiredService&lt;IMediator&gt;();
///
/// try
/// {
///     var user = await mediator.SendAsync(
///         new CreateUserCommand("", "John"),
///         cancellationToken);
/// }
/// catch (ValidationException ex)
/// {
///     Console.WriteLine($"Validation failed with {ex.Errors.Count} error(s):");
///     foreach (var error in ex.Errors)
///     {
///         Console.WriteLine($"  - {error.PropertyName}: {error.ErrorMessage}");
///     }
/// }
/// // Output:
/// // Validation failed with 2 error(s):
/// //   - Email: Email is required
/// //   - Email: Email must be a valid email address
/// </code>
///
/// <strong>Example: ASP.NET Core Controller</strong>
/// <code>
/// [ApiController]
/// [Route("api/[controller]")]
/// public class UsersController : ControllerBase
/// {
///     private readonly IMediator _mediator;
///
///     public UsersController(IMediator mediator)
///     {
///         _mediator = mediator;
///     }
///
///     [HttpPost]
///     public async Task&lt;IActionResult&gt; CreateUser([FromBody] CreateUserRequest request)
///     {
///         try
///         {
///             var command = new CreateUserCommand(request.Email, request.Name);
///             var user = await _mediator.SendAsync(command, HttpContext.RequestAborted);
///             return Created($"/api/users/{user.Id}", user);
///         }
///         catch (ValidationException ex)
///         {
///             var errors = ex.Errors.ToDictionary(
///                 e => e.PropertyName,
///                 e => e.ErrorMessage);
///             return BadRequest(new { errors });
///         }
///     }
/// }
/// </code>
///
/// <strong>Example: Global Exception Handler (ASP.NET Core 8+)</strong>
/// <code>
/// public class GlobalExceptionHandler : IExceptionHandler
/// {
///     public async ValueTask&lt;bool&gt; TryHandleAsync(
///         HttpContext httpContext,
///         Exception exception,
///         CancellationToken cancellationToken)
///     {
///         if (exception is ValidationException validationException)
///         {
///             httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
///
///             var problemDetails = new ProblemDetails
///             {
///                 Status = StatusCodes.Status400BadRequest,
///                 Title = "Validation Error",
///                 Detail = $"One or more validation errors occurred ({validationException.Errors.Count} error(s))"
///             };
///
///             problemDetails.Extensions["errors"] = validationException.Errors
///                 .GroupBy(e => e.PropertyName)
///                 .ToDictionary(
///                     g => g.Key,
///                     g => g.Select(e => e.ErrorMessage).ToArray());
///
///             await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
///             return true;
///         }
///
///         return false;
///     }
/// }
/// </code>
/// </example>
public class ValidationException : Exception
{
    /// <summary>
    /// Gets the collection of validation errors that caused this exception.
    /// </summary>
    /// <value>
    /// A read-only list of all <see cref="ValidationError"/> instances collected
    /// from all validators that ran for the request.
    /// </value>
    public IReadOnlyList<ValidationError> Errors { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationException"/> class
    /// with the specified validation errors.
    /// </summary>
    /// <param name="errors">The collection of validation errors</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="errors"/> is null</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="errors"/> is empty</exception>
    /// <remarks>
    /// This constructor creates an exception message that includes the total error count.
    /// The message format is: "Validation failed with {count} error(s)"
    /// </remarks>
    public ValidationException(IReadOnlyList<ValidationError> errors)
        : base($"Validation failed with {errors?.Count ?? 0} error(s)")
    {
        if (errors == null)
            throw new ArgumentNullException(nameof(errors), "Validation errors cannot be null");

        if (errors.Count == 0)
            throw new ArgumentException("At least one validation error must be provided", nameof(errors));

        Errors = errors;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationException"/> class
    /// with the specified validation errors.
    /// </summary>
    /// <param name="errors">The validation errors as a params array</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="errors"/> is null</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="errors"/> is empty</exception>
    /// <example>
    /// <code>
    /// throw new ValidationException(
    ///     new ValidationError("Email", "Email is required"),
    ///     new ValidationError("Name", "Name is required"));
    /// </code>
    /// </example>
    public ValidationException(params ValidationError[] errors)
        : this((IReadOnlyList<ValidationError>)errors)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationException"/> class
    /// with the specified validation errors and a custom message.
    /// </summary>
    /// <param name="message">The custom error message</param>
    /// <param name="errors">The collection of validation errors</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="errors"/> is null</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="errors"/> is empty</exception>
    public ValidationException(string message, IReadOnlyList<ValidationError> errors)
        : base(message)
    {
        if (errors == null)
            throw new ArgumentNullException(nameof(errors), "Validation errors cannot be null");

        if (errors.Count == 0)
            throw new ArgumentException("At least one validation error must be provided", nameof(errors));

        Errors = errors;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationException"/> class
    /// with the specified validation errors, custom message, and inner exception.
    /// </summary>
    /// <param name="message">The custom error message</param>
    /// <param name="innerException">The exception that caused this validation exception</param>
    /// <param name="errors">The collection of validation errors</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="errors"/> is null</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="errors"/> is empty</exception>
    public ValidationException(string message, Exception innerException, IReadOnlyList<ValidationError> errors)
        : base(message, innerException)
    {
        if (errors == null)
            throw new ArgumentNullException(nameof(errors), "Validation errors cannot be null");

        if (errors.Count == 0)
            throw new ArgumentException("At least one validation error must be provided", nameof(errors));

        Errors = errors;
    }
}
