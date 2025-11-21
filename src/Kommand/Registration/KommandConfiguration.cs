using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Kommand.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Kommand.Registration;

/// <summary>
/// Configuration builder for Kommand registration.
/// Provides a fluent API for discovering and registering handlers, validators, and interceptors.
/// </summary>
/// <remarks>
/// <para>
/// This class is used to configure Kommand before registering it in the dependency injection container.
/// It collects all handler registrations, validator registrations, and interceptor types during configuration,
/// then applies them to the <see cref="IServiceCollection"/> when <c>AddKommand()</c> is called.
/// </para>
/// <para>
/// <strong>Assembly Scanning Cache:</strong><br/>
/// Assembly scanning results are cached per assembly to improve performance when the same assembly
/// is scanned multiple times. The cache is thread-safe and shared across all configuration instances.
/// </para>
/// <para>
/// <strong>Typical usage:</strong>
/// <code>
/// services.AddKommand(config =>
/// {
///     config.RegisterHandlersFromAssembly(typeof(Program).Assembly);
///     config.AddInterceptor&lt;LoggingInterceptor&gt;();
///     config.WithValidation();
/// });
/// </code>
/// </para>
/// </remarks>
public class KommandConfiguration
{
    /// <summary>
    /// Thread-safe cache of assembly scanning results to avoid redundant reflection operations.
    /// </summary>
    private static readonly ConcurrentDictionary<Assembly, CachedAssemblyRegistrations> AssemblyCache = new();

    private readonly List<ServiceDescriptor> _handlerDescriptors = new();
    private readonly List<ServiceDescriptor> _validatorDescriptors = new();
    private readonly List<Type> _interceptorTypes = new();

    /// <summary>
    /// Gets or sets the default lifetime for auto-discovered handlers.
    /// Defaults to <see cref="ServiceLifetime.Scoped"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Scoped lifetime is recommended for handlers because:
    /// <list type="bullet">
    /// <item><description>Handlers can participate in database transactions within a request scope</description></item>
    /// <item><description>Handlers can inject scoped dependencies like DbContext</description></item>
    /// <item><description>Better performance than Transient (fewer allocations per request)</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Note: Validators are always registered as Scoped regardless of this setting,
    /// since they often need to inject repositories for async validation.
    /// </para>
    /// </remarks>
    public ServiceLifetime DefaultHandlerLifetime { get; set; } = ServiceLifetime.Scoped;

    /// <summary>
    /// Gets the collection of registered handler descriptors.
    /// </summary>
    /// <remarks>
    /// This is an internal property used by <c>ServiceCollectionExtensions</c> to apply
    /// the registrations to the DI container.
    /// </remarks>
    internal IReadOnlyList<ServiceDescriptor> HandlerDescriptors => _handlerDescriptors;

    /// <summary>
    /// Gets the collection of registered validator descriptors.
    /// </summary>
    /// <remarks>
    /// This is an internal property used by <c>ServiceCollectionExtensions</c> to apply
    /// the registrations to the DI container. Validators are always registered as Scoped.
    /// </remarks>
    internal IReadOnlyList<ServiceDescriptor> ValidatorDescriptors => _validatorDescriptors;

    /// <summary>
    /// Gets the collection of registered interceptor types.
    /// </summary>
    /// <remarks>
    /// This is an internal property used by <c>ServiceCollectionExtensions</c> to apply
    /// the registrations to the DI container. Interceptors are registered in the order they are added.
    /// </remarks>
    internal IReadOnlyList<Type> InterceptorTypes => _interceptorTypes;

    /// <summary>
    /// Registers all handlers and validators from the specified assembly using reflection.
    /// </summary>
    /// <param name="assembly">The assembly to scan for handler and validator implementations</param>
    /// <param name="lifetime">
    /// Optional lifetime for handlers. If not specified, uses <see cref="DefaultHandlerLifetime"/> (Scoped).
    /// <para>
    /// <strong>⚠️ Warning about Transient lifetime:</strong><br/>
    /// Transient handlers cannot inject Scoped services like DbContext, IHttpContextAccessor, etc.
    /// The DI container will throw InvalidOperationException at startup if you attempt this (captive dependency).
    /// Only use Transient if your handlers are purely stateless and only inject Singleton services.
    /// </para>
    /// <para>
    /// Note: Validators are always registered as Scoped regardless of this parameter.
    /// </para>
    /// </param>
    /// <returns>The current configuration instance for fluent chaining</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="assembly"/> is null</exception>
    /// <remarks>
    /// <para>
    /// This method scans the specified assembly for implementations of:
    /// <list type="bullet">
    /// <item><description><see cref="ICommandHandler{TCommand, TResponse}"/> - Command handlers</description></item>
    /// <item><description><see cref="IQueryHandler{TQuery, TResponse}"/> - Query handlers</description></item>
    /// <item><description><see cref="INotificationHandler{TNotification}"/> - Notification handlers</description></item>
    /// <item><description>IValidator&lt;T&gt; - Request validators (future implementation)</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// All discovered implementations are automatically registered in the DI container.
    /// A single handler class can implement multiple handler interfaces.
    /// </para>
    /// <para>
    /// <strong>Recommended Lifetime: Scoped (default)</strong><br/>
    /// Scoped lifetime allows handlers to:
    /// <list type="bullet">
    /// <item><description>Inject DbContext and participate in database transactions</description></item>
    /// <item><description>Access request-scoped services like IHttpContextAccessor</description></item>
    /// <item><description>Share instances within a single request (better performance than Transient)</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Note:</strong> This method uses reflection and is not compatible with trimming.
    /// It requires full type metadata at runtime for dynamic type discovery.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddKommand(config =>
    /// {
    ///     // Register handlers from current assembly with default Scoped lifetime
    ///     config.RegisterHandlersFromAssembly(typeof(Program).Assembly);
    ///
    ///     // Register handlers from another assembly with custom lifetime
    ///     config.RegisterHandlersFromAssembly(typeof(MyHandler).Assembly, ServiceLifetime.Transient);
    /// });
    /// </code>
    /// </example>
    [RequiresUnreferencedCode("This method uses reflection to scan assemblies for handler implementations. It is not compatible with trimming.")]
    public KommandConfiguration RegisterHandlersFromAssembly(
        Assembly assembly,
        ServiceLifetime? lifetime = null)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        var handlerLifetime = lifetime ?? DefaultHandlerLifetime;

        // Warn users if they explicitly chose Transient lifetime
        if (lifetime.HasValue && lifetime.Value == ServiceLifetime.Transient)
        {
            Console.WriteLine(
                "⚠️ WARNING: Handlers registered with Transient lifetime from assembly '{0}'.\n" +
                "   Transient handlers CANNOT inject Scoped services (DbContext, IHttpContextAccessor, etc.).\n" +
                "   This will cause InvalidOperationException if attempted.\n" +
                "   Recommended: Use Scoped lifetime (default) unless handlers are purely stateless.",
                assembly.GetName().Name);
        }

        // Try to get cached registrations for this assembly
        var cachedRegistrations = AssemblyCache.GetOrAdd(assembly, asm =>
        {
            // Cache miss - perform reflection and cache results
            var handlers = new List<(Type InterfaceType, Type ImplementationType)>();
            var validators = new List<(Type InterfaceType, Type ImplementationType)>();

            // Scan for all handler types
            ScanCommandHandlers(asm, handlers);
            ScanQueryHandlers(asm, handlers);
            ScanNotificationHandlers(asm, handlers);
            ScanValidators(asm, validators);

            return new CachedAssemblyRegistrations
            {
                Handlers = handlers,
                Validators = validators
            };
        });

        // Apply cached registrations with the specified lifetime
        foreach (var (interfaceType, implementationType) in cachedRegistrations.Handlers)
        {
            _handlerDescriptors.Add(new ServiceDescriptor(interfaceType, implementationType, handlerLifetime));
        }

        foreach (var (interfaceType, implementationType) in cachedRegistrations.Validators)
        {
            // Validators are always Scoped
            _validatorDescriptors.Add(new ServiceDescriptor(interfaceType, implementationType, ServiceLifetime.Scoped));
        }

        return this;
    }

    /// <summary>
    /// Scans the assembly for ICommandHandler implementations and adds them to the list.
    /// </summary>
    [RequiresUnreferencedCode("Uses reflection for type discovery")]
    private static void ScanCommandHandlers(Assembly assembly, List<(Type InterfaceType, Type ImplementationType)> handlers)
    {
        var commandHandlers = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract)
            .Where(t => t.GetInterfaces().Any(i =>
                i.IsGenericType &&
                i.GetGenericTypeDefinition() == typeof(ICommandHandler<,>)))
            .ToList();

        foreach (var handlerType in commandHandlers)
        {
            var interfaces = handlerType.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommandHandler<,>));

            foreach (var @interface in interfaces)
            {
                handlers.Add((@interface, handlerType));
            }
        }
    }

    /// <summary>
    /// Scans the assembly for IQueryHandler implementations and adds them to the list.
    /// </summary>
    [RequiresUnreferencedCode("Uses reflection for type discovery")]
    private static void ScanQueryHandlers(Assembly assembly, List<(Type InterfaceType, Type ImplementationType)> handlers)
    {
        var queryHandlers = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract)
            .Where(t => t.GetInterfaces().Any(i =>
                i.IsGenericType &&
                i.GetGenericTypeDefinition() == typeof(IQueryHandler<,>)))
            .ToList();

        foreach (var handlerType in queryHandlers)
        {
            var interfaces = handlerType.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IQueryHandler<,>));

            foreach (var @interface in interfaces)
            {
                handlers.Add((@interface, handlerType));
            }
        }
    }

    /// <summary>
    /// Scans the assembly for INotificationHandler implementations and adds them to the list.
    /// </summary>
    [RequiresUnreferencedCode("Uses reflection for type discovery")]
    private static void ScanNotificationHandlers(Assembly assembly, List<(Type InterfaceType, Type ImplementationType)> handlers)
    {
        var notificationHandlers = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract)
            .Where(t => t.GetInterfaces().Any(i =>
                i.IsGenericType &&
                i.GetGenericTypeDefinition() == typeof(INotificationHandler<>)))
            .ToList();

        foreach (var handlerType in notificationHandlers)
        {
            var interfaces = handlerType.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(INotificationHandler<>));

            foreach (var @interface in interfaces)
            {
                handlers.Add((@interface, handlerType));
            }
        }
    }

    /// <summary>
    /// Scans the assembly for IValidator implementations and adds them to the list.
    /// Validators are always registered as Scoped to support async validation with repositories.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method discovers all classes that implement <c>IValidator&lt;T&gt;</c> and adds them
    /// to the validators list. Multiple validators can be registered for the same request type,
    /// and they will all execute when validation is enabled.
    /// </para>
    /// <para>
    /// <strong>Lifetime:</strong><br/>
    /// Validators are always registered with <see cref="ServiceLifetime.Scoped"/> lifetime,
    /// regardless of the <see cref="DefaultHandlerLifetime"/> setting. This is because validators
    /// often need to inject scoped services like repositories or DbContext for async validation
    /// (e.g., checking if an email already exists in the database).
    /// </para>
    /// <para>
    /// <strong>Multiple Validators:</strong><br/>
    /// If multiple validators are registered for the same request type, the ValidationInterceptor
    /// will execute all of them sequentially and collect all errors before throwing ValidationException.
    /// </para>
    /// </remarks>
    [RequiresUnreferencedCode("Uses reflection for type discovery")]
    private static void ScanValidators(Assembly assembly, List<(Type InterfaceType, Type ImplementationType)> validators)
    {
        var validatorTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract)
            .Where(t => t.GetInterfaces().Any(i =>
                i.IsGenericType &&
                i.GetGenericTypeDefinition() == typeof(IValidator<>)))
            .ToList();

        foreach (var validatorType in validatorTypes)
        {
            var interfaces = validatorType.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IValidator<>));

            foreach (var @interface in interfaces)
            {
                validators.Add((@interface, validatorType));
            }
        }
    }

    /// <summary>
    /// Adds an interceptor type to the pipeline.
    /// Interceptors execute in the order they are added (first added = outermost).
    /// </summary>
    /// <param name="interceptorType">
    /// The interceptor type to add. Must implement IInterceptor (will be defined in Phase 2).
    /// </param>
    /// <returns>The current configuration instance for fluent chaining</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="interceptorType"/> is null</exception>
    /// <remarks>
    /// <para>
    /// Interceptors provide cross-cutting concerns like logging, validation, caching, etc.
    /// They wrap around handler execution in a pipeline pattern.
    /// </para>
    /// <para>
    /// <strong>Execution Order:</strong><br/>
    /// If you add interceptors in order: [Logging, Validation, Metrics]<br/>
    /// Execution flows as:
    /// <code>
    /// → Logging (enter)
    ///   → Validation (enter)
    ///     → Metrics (enter)
    ///       → Handler
    ///     → Metrics (exit)
    ///   → Validation (exit)
    /// → Logging (exit)
    /// </code>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddKommand(config =>
    /// {
    ///     config.RegisterHandlersFromAssembly(typeof(Program).Assembly);
    ///     config.AddInterceptor(typeof(LoggingInterceptor));
    ///     config.AddInterceptor(typeof(ValidationInterceptor));
    /// });
    /// </code>
    /// </example>
    public KommandConfiguration AddInterceptor(Type interceptorType)
    {
        ArgumentNullException.ThrowIfNull(interceptorType);

        _interceptorTypes.Add(interceptorType);
        return this;
    }

    /// <summary>
    /// Adds an interceptor type to the pipeline using a generic type parameter.
    /// Interceptors execute in the order they are added (first added = outermost).
    /// </summary>
    /// <typeparam name="TInterceptor">
    /// The interceptor type to add. Must implement IInterceptor (will be defined in Phase 2).
    /// </typeparam>
    /// <returns>The current configuration instance for fluent chaining</returns>
    /// <remarks>
    /// This is a convenience overload of <see cref="AddInterceptor(Type)"/> that provides
    /// compile-time type safety and cleaner syntax.
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddKommand(config =>
    /// {
    ///     config.RegisterHandlersFromAssembly(typeof(Program).Assembly);
    ///     config.AddInterceptor&lt;LoggingInterceptor&gt;();
    ///     config.AddInterceptor&lt;ValidationInterceptor&gt;();
    /// });
    /// </code>
    /// </example>
    public KommandConfiguration AddInterceptor<TInterceptor>() where TInterceptor : class
    {
        return AddInterceptor(typeof(TInterceptor));
    }

    /// <summary>
    /// Enables validation by adding the ValidationInterceptor to the pipeline.
    /// </summary>
    /// <returns>The current configuration instance for fluent chaining</returns>
    /// <remarks>
    /// <para>
    /// This method adds <c>ValidationInterceptor&lt;,&gt;</c> to the interceptor pipeline,
    /// which will automatically execute all registered validators for each request before
    /// the handler executes.
    /// </para>
    /// <para>
    /// <strong>What This Does:</strong>
    /// <list type="bullet">
    /// <item><description>Adds ValidationInterceptor to the interceptor pipeline</description></item>
    /// <item><description>Resolves all IValidator&lt;T&gt; implementations for each request from DI</description></item>
    /// <item><description>Executes all validators sequentially (not in parallel)</description></item>
    /// <item><description>Collects ALL errors from ALL validators (not fail-fast)</description></item>
    /// <item><description>Throws <see cref="ValidationException"/> with all errors if any validation fails</description></item>
    /// <item><description>Short-circuits handler execution on validation failure</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Prerequisites:</strong><br/>
    /// Validators must be registered in assemblies scanned via <see cref="RegisterHandlersFromAssembly"/>.
    /// The method automatically discovers and registers validators during assembly scanning.
    /// </para>
    /// <para>
    /// <strong>Performance:</strong><br/>
    /// If no validators are registered for a request type, the ValidationInterceptor has minimal
    /// overhead (just an empty collection check). This allows you to enable validation globally
    /// without performance impact on requests that don't need validation.
    /// </para>
    /// <para>
    /// <strong>Order Matters:</strong><br/>
    /// Interceptors execute in the order they are added. If you want validation to run before
    /// other interceptors (like logging), call <c>WithValidation()</c> first.
    /// </para>
    /// </remarks>
    /// <example>
    /// <strong>Example: Basic Setup</strong>
    /// <code>
    /// services.AddKommand(config =>
    /// {
    ///     // Discovers both handlers and validators
    ///     config.RegisterHandlersFromAssembly(typeof(Program).Assembly);
    ///
    ///     // Enables automatic validation
    ///     config.WithValidation();
    /// });
    /// </code>
    ///
    /// <strong>Example: With Multiple Interceptors</strong>
    /// <code>
    /// services.AddKommand(config =>
    /// {
    ///     config.RegisterHandlersFromAssembly(typeof(Program).Assembly);
    ///
    ///     // Validation runs first (outermost)
    ///     config.WithValidation();
    ///
    ///     // Then logging (innermost)
    ///     config.AddInterceptor&lt;LoggingInterceptor&gt;();
    /// });
    ///
    /// // Execution order:
    /// // → Validation (runs first)
    /// //   → Logging (runs second)
    /// //     → Handler
    /// </code>
    ///
    /// <strong>Example: Validation with Activity Tracing</strong>
    /// <code>
    /// services.AddKommand(config =>
    /// {
    ///     config.RegisterHandlersFromAssembly(typeof(Program).Assembly);
    ///     config.WithValidation();
    ///     config.WithActivityTracing(); // Built-in OpenTelemetry tracing
    /// });
    /// </code>
    /// </example>
    public KommandConfiguration WithValidation()
    {
        // Add ValidationInterceptor as an open generic type
        // The DI container will create closed generic instances for each request type
        AddInterceptor(typeof(ValidationInterceptor<,>));
        return this;
    }

    /// <summary>
    /// Cached registration information for an assembly.
    /// </summary>
    private record CachedAssemblyRegistrations
    {
        public required List<(Type InterfaceType, Type ImplementationType)> Handlers { get; init; }
        public required List<(Type InterfaceType, Type ImplementationType)> Validators { get; init; }
    }
}
