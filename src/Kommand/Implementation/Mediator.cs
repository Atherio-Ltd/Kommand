using System.Reflection;
using Kommand.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Kommand.Implementation;

/// <summary>
/// Default implementation of <see cref="IMediator"/>.
/// This class is internal and not exposed to consumers - they only interact with the IMediator interface.
/// </summary>
/// <remarks>
/// <para>
/// The mediator uses reflection to dynamically resolve and invoke handlers at runtime.
/// This is necessary because the exact command/query/notification type is not known at compile time.
/// </para>
/// <para>
/// <strong>Handler Resolution Strategy:</strong><br/>
/// 1. Build the generic handler interface type (e.g., ICommandHandler&lt;CreateUserCommand, User&gt;)<br/>
/// 2. Resolve the handler instance from the DI container using IServiceProvider<br/>
/// 3. Invoke the HandleAsync method using reflection<br/>
/// 4. Return the result to the caller
/// </para>
/// <para>
/// <strong>Why IServiceProvider?</strong><br/>
/// IServiceProvider is required because we need to resolve generic handler types that are constructed
/// at runtime based on the concrete request type. We cannot use constructor injection for handlers
/// since there can be hundreds of handler types in a large application.
/// </para>
/// </remarks>
internal sealed class Mediator : IMediator
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="Mediator"/> class.
    /// </summary>
    /// <param name="serviceProvider">
    /// The service provider used to resolve handler instances.
    /// Must not be null.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="serviceProvider"/> is null.
    /// </exception>
    public Mediator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <summary>
    /// Sends a command with a response and waits for the result.
    /// </summary>
    /// <typeparam name="TResponse">The type of value returned by the command handler</typeparam>
    /// <param name="command">The command to execute</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The result from the command handler</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="command"/> is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when no handler is registered for the command type</exception>
    public async Task<TResponse> SendAsync<TResponse>(
        ICommand<TResponse> command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Build handler type: ICommandHandler<TCommand, TResponse>
        var commandType = command.GetType();
        var handlerType = typeof(ICommandHandler<,>).MakeGenericType(commandType, typeof(TResponse));

        // Resolve handler from DI container
        var handler = _serviceProvider.GetService(handlerType) ?? throw new InvalidOperationException(
                $"No handler registered for command type '{commandType.Name}'. " +
                $"Ensure the handler is registered in the DI container using RegisterHandlersFromAssembly().");

        // Invoke HandleAsync using reflection
        var handleMethod = handlerType.GetMethod(nameof(ICommandHandler<ICommand<TResponse>, TResponse>.HandleAsync));

        try
        {
            var task = (Task<TResponse>)handleMethod!.Invoke(handler, new object[] { command, cancellationToken })!;
            return await task;
        }
        catch (TargetInvocationException ex) when (ex.InnerException != null)
        {
            // Unwrap TargetInvocationException and throw the actual handler exception
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            throw; // This line is unreachable but required for compiler
        }
    }

    /// <summary>
    /// Sends a command without a response (void command).
    /// </summary>
    /// <param name="command">The command to execute</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="command"/> is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when no handler is registered for the command type</exception>
    /// <remarks>
    /// Void commands internally use <see cref="Unit"/> as the response type to maintain
    /// uniformity in the mediator pipeline. The Unit value is discarded after handler execution.
    /// </remarks>
    public async Task SendAsync(
        ICommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Build handler type: ICommandHandler<TCommand, Unit> (void commands use Unit)
        var commandType = command.GetType();
        var handlerType = typeof(ICommandHandler<,>).MakeGenericType(commandType, typeof(Unit));

        // Resolve handler from DI container
        var handler = _serviceProvider.GetService(handlerType) ?? throw new InvalidOperationException(
                $"No handler registered for command type '{commandType.Name}'. " +
                $"Ensure the handler is registered in the DI container using RegisterHandlersFromAssembly().");

        // Invoke HandleAsync using reflection
        var handleMethod = handlerType.GetMethod(nameof(ICommandHandler<ICommand, Unit>.HandleAsync));
        await (Task<Unit>)handleMethod!.Invoke(handler, [command, cancellationToken])!;
    }

    /// <summary>
    /// Executes a query and returns the result.
    /// </summary>
    /// <typeparam name="TResponse">The type of data returned by the query handler</typeparam>
    /// <param name="query">The query to execute</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The data from the query handler</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="query"/> is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when no handler is registered for the query type</exception>
    public async Task<TResponse> QueryAsync<TResponse>(
        IQuery<TResponse> query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        // Build handler type: IQueryHandler<TQuery, TResponse>
        var queryType = query.GetType();
        var handlerType = typeof(IQueryHandler<,>).MakeGenericType(queryType, typeof(TResponse));

        // Resolve handler from DI container
        var handler = _serviceProvider.GetService(handlerType) ?? throw new InvalidOperationException(
                $"No handler registered for query type '{queryType.Name}'. " +
                $"Ensure the handler is registered in the DI container using RegisterHandlersFromAssembly().");

        // Invoke HandleAsync using reflection
        var handleMethod = handlerType.GetMethod(nameof(IQueryHandler<IQuery<TResponse>, TResponse>.HandleAsync));

        try
        {
            var task = (Task<TResponse>)handleMethod!.Invoke(handler, [query, cancellationToken])!;
            return await task;
        }
        catch (TargetInvocationException ex) when (ex.InnerException != null)
        {
            // Unwrap TargetInvocationException and throw the actual handler exception
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            throw; // This line is unreachable but required for compiler
        }
    }

    /// <summary>
    /// Publishes a notification to all registered handlers.
    /// </summary>
    /// <typeparam name="TNotification">The type of notification to publish</typeparam>
    /// <param name="notification">The notification instance containing event data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="notification"/> is null</exception>
    /// <remarks>
    /// <para>
    /// <strong>Execution Behavior:</strong>
    /// <list type="bullet">
    /// <item><description>If no handlers are registered, the method completes silently without throwing</description></item>
    /// <item><description>All handlers execute sequentially (not in parallel)</description></item>
    /// <item><description>If one handler throws an exception, other handlers still execute</description></item>
    /// <item><description>Handler exceptions are caught and swallowed to ensure resilience</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// This fire-and-forget behavior makes notifications ideal for domain events and side effects
    /// that should not break the main operation flow.
    /// </para>
    /// </remarks>
    public async Task PublishAsync<TNotification>(
        TNotification notification,
        CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        if (notification == null) throw new ArgumentNullException(nameof(notification));

        // Build handler type: INotificationHandler<TNotification>
        var notificationType = notification.GetType();
        var handlerType = typeof(INotificationHandler<>).MakeGenericType(notificationType);

        // Resolve ALL handlers (notifications can have multiple handlers)
        var enumerableType = typeof(IEnumerable<>).MakeGenericType(handlerType);
        var handlers = _serviceProvider.GetService(enumerableType) as IEnumerable<object>;

        // No handlers registered - this is OK for notifications (fire-and-forget)
        if (handlers == null || !handlers.Any())
        {
            return;
        }

        // Execute handlers sequentially
        var handleMethod = handlerType.GetMethod(nameof(INotificationHandler<INotification>.HandleAsync));

        foreach (var handler in handlers)
        {
            try
            {
                // Invoke handler - continue even if this one fails
                await (Task)handleMethod!.Invoke(handler, new object[] { notification, cancellationToken })!;
            }
            catch (Exception)
            {
                // Swallow exceptions - one handler failure shouldn't prevent others from executing
                // TODO: Add logging in future task to track these failures
            }
        }
    }
}
