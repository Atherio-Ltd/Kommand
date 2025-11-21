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
    /// <exception cref="HandlerNotFoundException">Thrown when no handler is registered for the command type</exception>
    public async Task<TResponse> SendAsync<TResponse>(
        ICommand<TResponse> command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Build handler type: ICommandHandler<TCommand, TResponse>
        var commandType = command.GetType();
        var handlerType = typeof(ICommandHandler<,>).MakeGenericType(commandType, typeof(TResponse));

        // Resolve handler from DI container
        var handler = _serviceProvider.GetService(handlerType) ?? throw new HandlerNotFoundException(commandType);

        // Get handler method for invocation
        var handleMethod = handlerType.GetMethod(nameof(ICommandHandler<ICommand<TResponse>, TResponse>.HandleAsync));

        // Create handler function that will be wrapped by interceptor pipeline
        Func<Task<TResponse>> handlerFunc = async () =>
        {
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
        };

        // Build and execute interceptor pipeline using the actual command type
        var pipeline = BuildPipeline(commandType, typeof(TResponse), command, handlerFunc, cancellationToken);
        return await pipeline();
    }

    /// <summary>
    /// Sends a command without a response (void command).
    /// </summary>
    /// <param name="command">The command to execute</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="command"/> is null</exception>
    /// <exception cref="HandlerNotFoundException">Thrown when no handler is registered for the command type</exception>
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
        var handler = _serviceProvider.GetService(handlerType) ?? throw new HandlerNotFoundException(commandType);

        // Get handler method for invocation
        var handleMethod = handlerType.GetMethod(nameof(ICommandHandler<ICommand, Unit>.HandleAsync));

        // Create handler function that will be wrapped by interceptor pipeline
        Func<Task<Unit>> handlerFunc = async () =>
        {
            try
            {
                var task = (Task<Unit>)handleMethod!.Invoke(handler, [command, cancellationToken])!;
                return await task;
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                // Unwrap TargetInvocationException and throw the actual handler exception
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                throw; // This line is unreachable but required for compiler
            }
        };

        // Build and execute interceptor pipeline using the actual command type
        var pipeline = BuildPipeline(commandType, typeof(Unit), command, handlerFunc, cancellationToken);
        await pipeline();
    }

    /// <summary>
    /// Executes a query and returns the result.
    /// </summary>
    /// <typeparam name="TResponse">The type of data returned by the query handler</typeparam>
    /// <param name="query">The query to execute</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The data from the query handler</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="query"/> is null</exception>
    /// <exception cref="HandlerNotFoundException">Thrown when no handler is registered for the query type</exception>
    public async Task<TResponse> QueryAsync<TResponse>(
        IQuery<TResponse> query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        // Build handler type: IQueryHandler<TQuery, TResponse>
        var queryType = query.GetType();
        var handlerType = typeof(IQueryHandler<,>).MakeGenericType(queryType, typeof(TResponse));

        // Resolve handler from DI container
        var handler = _serviceProvider.GetService(handlerType) ?? throw new HandlerNotFoundException(queryType);

        // Get handler method for invocation
        var handleMethod = handlerType.GetMethod(nameof(IQueryHandler<IQuery<TResponse>, TResponse>.HandleAsync));

        // Create handler function that will be wrapped by interceptor pipeline
        Func<Task<TResponse>> handlerFunc = async () =>
        {
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
        };

        // Build and execute interceptor pipeline using the actual query type
        var pipeline = BuildPipeline(queryType, typeof(TResponse), query, handlerFunc, cancellationToken);
        return await pipeline();
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

    /// <summary>
    /// Builds the interceptor pipeline for a request, wrapping the handler execution with registered interceptors.
    /// </summary>
    /// <typeparam name="TResponse">The type of response returned by the handler</typeparam>
    /// <param name="requestType">The runtime type of the request (e.g., TestCommand, not ICommand)</param>
    /// <param name="responseType">The runtime type of the response</param>
    /// <param name="request">The request instance</param>
    /// <param name="handlerFunc">Function that invokes the actual handler</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A delegate that executes the complete pipeline (interceptors + handler)</returns>
    /// <remarks>
    /// <para>
    /// This method constructs the interceptor pipeline by wrapping the handler function with all
    /// registered interceptors for the request type. Interceptors are resolved from the DI container
    /// and built into a chain where each interceptor can execute logic before and after calling the next.
    /// </para>
    /// <para>
    /// <strong>Pipeline Construction Order:</strong><br/>
    /// Interceptors are built in <strong>reverse order</strong> to ensure correct execution flow.
    /// If interceptors are registered as: [Logging, Validation, Metrics]<br/>
    /// They are built as: Handler → Metrics → Validation → Logging<br/>
    /// So execution flows: Logging (enter) → Validation (enter) → Metrics (enter) → Handler → Metrics (exit) → Validation (exit) → Logging (exit)
    /// </para>
    /// <para>
    /// <strong>Interceptor Resolution:</strong><br/>
    /// The method resolves all interceptors that match <c>IInterceptor&lt;TRequest, TResponse&gt;</c>.
    /// This includes both generic interceptors and type-specific interceptors (ICommandInterceptor, IQueryInterceptor)
    /// since they all implement the base IInterceptor interface internally.
    /// </para>
    /// <para>
    /// <strong>No Interceptors Scenario:</strong><br/>
    /// If no interceptors are registered for the request type, the method returns a delegate that
    /// directly invokes the handler function without any wrapping. This ensures zero overhead when
    /// interceptors are not used.
    /// </para>
    /// </remarks>
    private RequestHandlerDelegate<TResponse> BuildPipeline<TResponse>(
        Type requestType,
        Type responseType,
        object request,
        Func<Task<TResponse>> handlerFunc,
        CancellationToken cancellationToken)
    {
        // Start with the handler as the innermost delegate
        RequestHandlerDelegate<TResponse> pipeline = () => handlerFunc();

        // Resolve all interceptors for this request type
        // Get IEnumerable<IInterceptor<TRequest, TResponse>> from the container
        // Use the actual request type (e.g., TestCommand), not the base interface (e.g., ICommand)
        var interceptorType = typeof(IInterceptor<,>).MakeGenericType(requestType, responseType);
        var enumerableType = typeof(IEnumerable<>).MakeGenericType(interceptorType);
        var interceptorsEnumerable = _serviceProvider.GetService(enumerableType) as System.Collections.IEnumerable;

        // If no interceptors registered, return handler directly (zero overhead)
        if (interceptorsEnumerable == null)
        {
            return pipeline;
        }

        // Convert to list for reverse iteration
        var interceptors = interceptorsEnumerable.Cast<object>().ToList();
        if (!interceptors.Any())
        {
            return pipeline;
        }

        // Build pipeline in reverse order (last registered interceptor wraps handler)
        // This ensures first registered interceptor is outermost (executes first on entry, last on exit)
        foreach (var interceptor in interceptors.AsEnumerable().Reverse())
        {
            var currentPipeline = pipeline; // Capture current pipeline for closure
            var handleMethod = interceptorType.GetMethod(nameof(IInterceptor<IRequest<TResponse>, TResponse>.HandleAsync));

            // Wrap current pipeline with this interceptor
            pipeline = () =>
            {
                try
                {
                    var task = (Task<TResponse>)handleMethod!.Invoke(
                        interceptor,
                        new object[] { request, currentPipeline, cancellationToken })!;
                    return task;
                }
                catch (TargetInvocationException ex) when (ex.InnerException != null)
                {
                    // Unwrap TargetInvocationException from reflection and preserve original stack trace
                    System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                    throw; // Unreachable but required for compiler
                }
            };
        }

        return pipeline;
    }
}
