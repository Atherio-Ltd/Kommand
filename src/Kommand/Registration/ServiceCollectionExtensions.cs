using System.Diagnostics.CodeAnalysis;
using Kommand;
using Kommand.Abstractions;
using Kommand.Implementation;
using Kommand.Interceptors;
using Kommand.Registration;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for registering Kommand mediator and handlers in the dependency injection container.
/// </summary>
/// <remarks>
/// <para>
/// This class provides the primary entry point for configuring Kommand in your application.
/// The <see cref="AddKommand"/> method registers the mediator, discovers handlers, and configures
/// the request processing pipeline.
/// </para>
/// </remarks>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers Kommand mediator and handlers in the service collection.
    /// </summary>
    /// <param name="services">The service collection to register services into</param>
    /// <param name="configure">
    /// Configuration action that specifies which assemblies to scan for handlers,
    /// which interceptors to add, and other options.
    /// </param>
    /// <returns>The service collection for method chaining</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="services"/> or <paramref name="configure"/> is null
    /// </exception>
    /// <remarks>
    /// <para>
    /// <strong>What gets registered:</strong>
    /// <list type="bullet">
    /// <item><description><see cref="IMediator"/> - Registered as <strong>Scoped</strong> (not Singleton)</description></item>
    /// <item><description>Command handlers - <see cref="ICommandHandler{TCommand, TResponse}"/></description></item>
    /// <item><description>Query handlers - <see cref="IQueryHandler{TQuery, TResponse}"/></description></item>
    /// <item><description>Notification handlers - <see cref="INotificationHandler{TNotification}"/></description></item>
    /// <item><description>Validators - IValidator&lt;T&gt; (when implemented in Phase 4)</description></item>
    /// <item><description>Built-in OpenTelemetry interceptors - <see cref="ActivityInterceptor{TRequest,TResponse}"/> and <see cref="MetricsInterceptor{TRequest,TResponse}"/> (automatically registered as Singleton)</description></item>
    /// <item><description>Custom interceptors - User-defined cross-cutting concern implementations</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Default Lifetimes:</strong><br/>
    /// - Mediator: <strong>Scoped</strong> (allows participation in request scope)<br/>
    /// - Handlers: <strong>Scoped</strong> (can inject DbContext, participate in transactions)<br/>
    /// - Validators: <strong>Scoped</strong> (can inject repositories for async validation)<br/>
    /// - Built-in OTEL Interceptors: <strong>Singleton</strong> (stateless, lightweight, zero overhead when OTEL not configured)<br/>
    /// - Custom Interceptors: <strong>Scoped</strong> (can maintain state within a request)
    /// </para>
    /// <para>
    /// <strong>Why Scoped?</strong><br/>
    /// Scoped lifetime is recommended because:
    /// <list type="bullet">
    /// <item><description>Handlers can inject scoped dependencies like Entity Framework DbContext</description></item>
    /// <item><description>Multiple handlers in a single request share the same DbContext instance</description></item>
    /// <item><description>Better performance than Transient (fewer allocations)</description></item>
    /// <item><description>Automatically disposed at end of request scope</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// Basic configuration in Program.cs or Startup.cs:
    /// <code>
    /// // ASP.NET Core minimal API
    /// var builder = WebApplication.CreateBuilder(args);
    ///
    /// builder.Services.AddKommand(config =>
    /// {
    ///     // Register all handlers from the current assembly
    ///     config.RegisterHandlersFromAssembly(typeof(Program).Assembly);
    /// });
    ///
    /// var app = builder.Build();
    /// </code>
    /// </example>
    /// <example>
    /// Advanced configuration with multiple assemblies and interceptors:
    /// <code>
    /// services.AddKommand(config =>
    /// {
    ///     // Register handlers from multiple assemblies
    ///     config.RegisterHandlersFromAssembly(typeof(Program).Assembly);
    ///     config.RegisterHandlersFromAssembly(typeof(CoreHandler).Assembly);
    ///
    ///     // Add custom interceptors (executed in order: Logging → Validation → Metrics)
    ///     config.AddInterceptor&lt;LoggingInterceptor&gt;();
    ///     config.AddInterceptor&lt;ValidationInterceptor&gt;();
    ///     config.AddInterceptor&lt;MetricsInterceptor&gt;();
    ///
    ///     // Override default handler lifetime
    ///     config.DefaultHandlerLifetime = ServiceLifetime.Transient;
    /// });
    /// </code>
    /// </example>
    /// <example>
    /// Using Kommand in a controller:
    /// <code>
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
    ///     public async Task&lt;IActionResult&gt; CreateUser(
    ///         [FromBody] CreateUserRequest request,
    ///         CancellationToken cancellationToken)
    ///     {
    ///         var command = new CreateUserCommand(request.Email, request.Name);
    ///         var user = await _mediator.SendAsync(command, cancellationToken);
    ///
    ///         // Publish domain event
    ///         await _mediator.PublishAsync(
    ///             new UserCreatedNotification(user.Id, user.Email),
    ///             cancellationToken);
    ///
    ///         return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
    ///     }
    ///
    ///     [HttpGet("{id}")]
    ///     public async Task&lt;IActionResult&gt; GetUser(Guid id, CancellationToken cancellationToken)
    ///     {
    ///         var query = new GetUserByIdQuery(id);
    ///         var user = await _mediator.QueryAsync(query, cancellationToken);
    ///         return user != null ? Ok(user) : NotFound();
    ///     }
    /// }
    /// </code>
    /// </example>
    [RequiresUnreferencedCode("Kommand uses reflection to discover and register handlers. It is not compatible with trimming.")]
    public static IServiceCollection AddKommand(
        this IServiceCollection services,
        Action<KommandConfiguration> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        // Create configuration and invoke user's setup
        var config = new KommandConfiguration();
        configure(config);

        // Register IMediator as Scoped (NOT Singleton!)
        // Scoped allows mediator to participate in request scope and access scoped services
        services.AddScoped<IMediator, Mediator>();

        // Register all discovered handlers
        foreach (var descriptor in config.HandlerDescriptors)
        {
            services.Add(descriptor);
        }

        // Register all discovered validators
        // (Will be populated in Phase 4 when IValidator<T> is implemented)
        foreach (var descriptor in config.ValidatorDescriptors)
        {
            services.Add(descriptor);
        }

        // Register built-in OpenTelemetry interceptors (always enabled, zero-config)
        // These interceptors are registered as Singleton because they are stateless and lightweight
        // They create Activities and Metrics with ~10-50ns overhead when OTEL is not configured
        // IMPORTANT: These must be registered BEFORE user-defined interceptors to be outermost
        services.TryAddEnumerable(ServiceDescriptor.Singleton(
            typeof(IInterceptor<,>),
            typeof(ActivityInterceptor<,>)));
        services.TryAddEnumerable(ServiceDescriptor.Singleton(
            typeof(IInterceptor<,>),
            typeof(MetricsInterceptor<,>)));

        // Register user-defined interceptors
        // Interceptors are registered as Scoped to allow state management within a request
        // We register both the concrete type AND all IInterceptor<,> interfaces it implements
        foreach (var interceptorType in config.InterceptorTypes)
        {
            // Find all IInterceptor<,> interfaces this type implements
            var interceptorInterfaces = interceptorType.GetInterfaces()
                .Where(i => i.IsGenericType &&
                           (i.GetGenericTypeDefinition() == typeof(IInterceptor<,>) ||
                            i.GetGenericTypeDefinition() == typeof(ICommandInterceptor<,>) ||
                            i.GetGenericTypeDefinition() == typeof(IQueryInterceptor<,>)));

            foreach (var @interface in interceptorInterfaces)
            {
                // Register each interceptor interface implementation
                // Multiple registrations of the same interface are collected via IEnumerable<> resolution
                services.AddScoped(@interface, interceptorType);
            }
        }

        return services;
    }
}
