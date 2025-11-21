namespace Kommand;

/// <summary>
/// Base exception class for all Kommand-related exceptions.
/// </summary>
/// <remarks>
/// <para>
/// This abstract base class provides a common inheritance hierarchy for all exceptions
/// thrown by the Kommand library. Application code can catch this exception type to
/// handle any Kommand-specific error scenario.
/// </para>
/// <para>
/// <strong>Exception Hierarchy:</strong><br/>
/// All Kommand exceptions inherit from this base class, enabling developers to:
/// <list type="bullet">
/// <item><description>Catch all Kommand exceptions with a single catch block</description></item>
/// <item><description>Differentiate between Kommand errors and application errors</description></item>
/// <item><description>Implement custom error handling strategies for mediator operations</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// try
/// {
///     var result = await mediator.SendAsync(command);
/// }
/// catch (KommandException ex)
/// {
///     // Handle all Kommand-specific exceptions (HandlerNotFoundException, etc.)
///     _logger.LogError(ex, "Kommand operation failed");
///     return StatusCode(500, "Internal mediator error");
/// }
/// catch (ValidationException ex)
/// {
///     // Validation errors are application-level, not KommandException
///     return BadRequest(ex.Errors);
/// }
/// </code>
/// </example>
public abstract class KommandException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="KommandException"/> class.
    /// </summary>
    protected KommandException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="KommandException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    protected KommandException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="KommandException"/> class with a specified error message
    /// and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">
    /// The exception that is the cause of the current exception, or null if no inner exception is specified.
    /// </param>
    protected KommandException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
