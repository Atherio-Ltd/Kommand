namespace Kommand;

/// <summary>
/// Exception thrown when no handler is registered for a command or query type.
/// </summary>
/// <remarks>
/// <para>
/// This exception is thrown by <see cref="Abstractions.IMediator"/> when attempting to execute
/// a command or query that has no corresponding handler registered in the dependency injection container.
/// </para>
/// <para>
/// <strong>Common Causes:</strong>
/// <list type="bullet">
/// <item><description>Handler class was not registered via <c>RegisterHandlersFromAssembly()</c></description></item>
/// <item><description>Handler is in a different assembly that was not scanned</description></item>
/// <item><description>Handler class is not public or does not implement the correct interface</description></item>
/// <item><description>Handler was registered with the wrong lifetime (should be Scoped by default)</description></item>
/// </list>
/// </para>
/// <para>
/// <strong>Resolution:</strong><br/>
/// Ensure the handler is properly registered during application startup:
/// <code>
/// services.AddKommand(config =>
/// {
///     config.RegisterHandlersFromAssembly(typeof(Program).Assembly);
///     config.RegisterHandlersFromAssembly(typeof(CreateUserCommandHandler).Assembly);
/// });
/// </code>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// try
/// {
///     var user = await mediator.SendAsync(new CreateUserCommand("test@example.com"));
/// }
/// catch (HandlerNotFoundException ex)
/// {
///     _logger.LogError(
///         "No handler registered for {RequestType}. Did you forget to call RegisterHandlersFromAssembly()?",
///         ex.RequestType.Name);
///
///     return StatusCode(500, $"Handler configuration error for {ex.RequestType.Name}");
/// }
/// </code>
/// </example>
public sealed class HandlerNotFoundException : KommandException
{
    /// <summary>
    /// Gets the type of the request (command or query) that has no registered handler.
    /// </summary>
    /// <value>
    /// The runtime type of the command or query that was sent to the mediator.
    /// </value>
    /// <remarks>
    /// This property is useful for logging and debugging to identify exactly which handler is missing.
    /// The type name can be used to search the codebase for the handler implementation.
    /// </remarks>
    public Type RequestType { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="HandlerNotFoundException"/> class.
    /// </summary>
    /// <param name="requestType">The type of request that has no registered handler.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="requestType"/> is null.</exception>
    public HandlerNotFoundException(Type requestType)
        : base(BuildMessage(requestType))
    {
        RequestType = requestType ?? throw new ArgumentNullException(nameof(requestType));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HandlerNotFoundException"/> class with a custom message.
    /// </summary>
    /// <param name="requestType">The type of request that has no registered handler.</param>
    /// <param name="message">The custom error message.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="requestType"/> is null.</exception>
    public HandlerNotFoundException(Type requestType, string message)
        : base(message)
    {
        RequestType = requestType ?? throw new ArgumentNullException(nameof(requestType));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HandlerNotFoundException"/> class with a custom message
    /// and inner exception.
    /// </summary>
    /// <param name="requestType">The type of request that has no registered handler.</param>
    /// <param name="message">The custom error message.</param>
    /// <param name="innerException">The inner exception that caused this exception.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="requestType"/> is null.</exception>
    public HandlerNotFoundException(Type requestType, string message, Exception innerException)
        : base(message, innerException)
    {
        RequestType = requestType ?? throw new ArgumentNullException(nameof(requestType));
    }

    /// <summary>
    /// Builds a descriptive error message for the exception.
    /// </summary>
    /// <param name="requestType">The type of request that has no registered handler.</param>
    /// <returns>A formatted error message with troubleshooting guidance.</returns>
    private static string BuildMessage(Type requestType)
    {
        if (requestType == null)
        {
            return "No handler registered for the request type.";
        }

        return $"No handler registered for request type '{requestType.Name}'. " +
               $"Ensure the handler is registered in the DI container using RegisterHandlersFromAssembly().";
    }
}
