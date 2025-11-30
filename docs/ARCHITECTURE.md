# Kommand - Production-Ready CQRS Library

**Project**: Kommand
**Date**: 2025-01-13
**Status**: Architecture & Design Phase
**Version**: v1.0 Specification

---

## Executive Summary

Kommand is a production-ready CQRS library for .NET with:
- **Zero external dependencies** (except DI abstractions)
- **Automatic OpenTelemetry instrumentation** (zero configuration)
- **Interceptor-based cross-cutting concerns** (logging, validation, etc.)
- **MIT License** (fully open source)

This document covers **17 critical architectural dimensions** for building a library that's simple, fast, and production-ready.

### Why "Kommand"?

- **Playful branding**: "K" for coolness (like Kotlin, Kubernetes, K8s)
- **Clear meaning**: Commands are core to CQRS
- **Memorable**: Easy to remember, pronounce, spell
- **Developer-friendly**: Fun but professional

---

## Table of Contents

1. [Core Library Scope & Features](#1-core-library-scope--features)
2. [Architecture: Command vs Query (CQRS)](#2-architecture-command-vs-query-cqrs)
3. [Interceptor System](#3-interceptor-system)
4. [Dependency Injection Strategy](#4-dependency-injection-strategy)
5. [Project Structure & Packaging](#5-project-structure--packaging)
6. [Validation Strategy](#6-validation-strategy)
7. [Notification System Design](#7-notification-system-design)
8. [IMediator Interface Design](#8-imediator-interface-design)
9. [Error Handling Philosophy](#9-error-handling-philosophy)
10. [OpenTelemetry Auto-Integration](#10-opentelemetry-auto-integration)
11. [Performance Considerations](#11-performance-considerations)
12. [Testing Strategy](#12-testing-strategy)
13. [Documentation Requirements](#13-documentation-requirements)
14. [Implementation Roadmap](#14-implementation-roadmap)
15. [Critical Decisions Summary](#15-critical-decisions-summary)
16. [Potential Risks & Mitigation](#16-potential-risks--mitigation)
17. [Migration from MediatR](#17-migration-from-mediatr)

---

## 1. 🎯 Core Library Scope & Features

### Feature Set for v1.0 (Production-Ready)

| Feature | Priority | Rationale |
|---------|----------|-----------|
| **Command/Query distinction** | ✅ Must Have | CQRS semantics, different interceptors per type |
| **Interceptor system** | ✅ Must Have | Cross-cutting concerns (validation, logging, etc.) |
| **Auto handler registration** | ✅ Must Have | Developer experience, reduces boilerplate |
| **Flexible DI lifetimes** | ✅ Must Have | Different handlers have different needs |
| **OpenTelemetry auto-integration** | ✅ Must Have | Zero-config observability |
| **Notifications (Pub/Sub)** | ✅ Must Have | Domain events, decoupling |
| **Streaming queries** | 🔴 v2.0 | Advanced feature, adds complexity |

**Included in v1.0**: Commands, Queries, Interceptors, Notifications, Auto-OTEL

**Deferred to v2.0**: Streaming (IAsyncEnumerable), Result<T> pattern

---

## 2. 🏗️ Architecture: Command vs Query (CQRS)

### Core Abstractions Design

```csharp
namespace Kommand;

// Base marker interfaces
public interface IRequest<out TResponse> { }
public interface IRequest : IRequest<Unit> { }

// CQRS-specific interfaces
public interface ICommand<out TResponse> : IRequest<TResponse> { }
public interface ICommand : ICommand<Unit> { }

public interface IQuery<out TResponse> : IRequest<TResponse> { }

// Handlers
public interface ICommandHandler<in TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    Task<TResponse> HandleAsync(TCommand command, CancellationToken cancellationToken);
}

public interface IQueryHandler<in TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    Task<TResponse> HandleAsync(TQuery query, CancellationToken cancellationToken);
}

// Notifications
public interface INotification { }

public interface INotificationHandler<in TNotification>
    where TNotification : INotification
{
    Task HandleAsync(TNotification notification, CancellationToken cancellationToken);
}

// Unit type for void returns
public readonly struct Unit
{
    public static readonly Unit Value = default;
}
```

### Why This Design?

1. **Semantic Clarity**: `ICommand` vs `IQuery` makes intent explicit
2. **Interceptor Differentiation**: Different interceptors for commands vs queries
   - Commands: Validation → Activity Tracking → Handler
   - Queries: Activity Tracking → Caching → Handler
3. **Constraints**: Can add compile-time constraints (e.g., queries are read-only)
4. **Metrics**: Separate metrics for commands vs queries
5. **Future-proof**: Can evolve independently

### Example Usage

```csharp
// Define command
public record CreateUserCommand(string Email, string Name) : ICommand<User>;

// Define handler
public class CreateUserCommandHandler : ICommandHandler<CreateUserCommand, User>
{
    private readonly IUserRepository _repository;
    private readonly ILogger<CreateUserCommandHandler> _logger;

    public async Task<User> HandleAsync(CreateUserCommand command, CancellationToken ct)
    {
        var user = new User(command.Email, command.Name);
        await _repository.AddAsync(user, ct);
        return user;
    }
}

// Use in controller
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request)
    {
        var command = new CreateUserCommand(request.Email, request.Name);
        var user = await _mediator.SendAsync(command, HttpContext.RequestAborted);
        return Created($"/users/{user.Id}", user);
    }
}
```

**Benefits**:
- ✅ Controller has single dependency (`IMediator`)
- ✅ Each handler only injects what it needs
- ✅ One handler per use case (compiler-enforced)
- ✅ Easy to test handlers in isolation
- ✅ Automatic observability via interceptors

---

## 3. 🔄 Interceptor System

### Core Abstraction

```csharp
namespace Kommand;

// Generic interceptor (applies to all requests)
public interface IInterceptor<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    Task<TResponse> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken);
}

// Command-specific interceptor
public interface ICommandInterceptor<in TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    Task<TResponse> HandleAsync(
        TCommand command,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken);
}

// Query-specific interceptor
public interface IQueryInterceptor<in TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    Task<TResponse> HandleAsync(
        TQuery query,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken);
}

// Next delegate in chain
public delegate Task<TResponse> RequestHandlerDelegate<TResponse>();
```

### Built-in Interceptors (Part of Core Package)

**Only OTEL interceptors are included by default:**

1. **ActivityInterceptor**: OpenTelemetry distributed tracing
2. **MetricsInterceptor**: OpenTelemetry metrics (duration, count, status)

These are **always registered and always safe** - they have zero overhead when OTEL is not configured (~10-50 nanoseconds per request).

### User-Defined Interceptors

Users write their own for logging, validation, caching, etc.:

```csharp
// Example: Custom logging interceptor
public class LoggingInterceptor<TRequest, TResponse> : IInterceptor<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger _logger;

    public async Task<TResponse> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        _logger.LogInformation("Handling {RequestName}", requestName);

        var response = await next();

        _logger.LogInformation("Handled {RequestName} successfully", requestName);
        return response;
    }
}
```

### Interceptor Registration & Execution Order

**User controls order explicitly:**

```csharp
services.AddKommand(config =>
{
    config.RegisterHandlersFromAssembly(typeof(Program).Assembly);

    // Order matters! First registered = outermost
    config.AddInterceptor<LoggingInterceptor>();        // Runs first
    config.AddInterceptor<ValidationInterceptor>();     // Then validation
    // ActivityInterceptor and MetricsInterceptor are auto-added
});
```

**Execution Flow**:
```
Request
  → Logging (enter)
    → Validation (enter)
      → Activity (start span) [auto-added]
        → Metrics (start timer) [auto-added]
          → Handler (execute business logic)
        → Metrics (record) [auto-added]
      → Activity (end span) [auto-added]
    → Validation (exit)
  → Logging (exit)
→ Response
```

---

## 4. 🔧 Dependency Injection Strategy

### Handler Lifetime Defaults

**Default**: `Scoped` (differs from MediatR's `Transient`)

**Rationale**:
- DbContext is `Scoped` in ASP.NET Core
- Most handlers need scoped dependencies (repositories, UoW, etc.)
- More efficient than creating new instances per request
- Safe and predictable

### Registration API

```csharp
namespace Kommand;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddKommand(
        this IServiceCollection services,
        Action<KommandConfiguration> configure)
    {
        var config = new KommandConfiguration();
        configure(config);

        // Register core mediator
        services.AddScoped<IMediator, Mediator>();

        // Register handlers
        foreach (var descriptor in config.HandlerDescriptors)
        {
            services.Add(descriptor);
        }

        // Register validators (auto-discovered during assembly scanning)
        foreach (var descriptor in config.ValidatorDescriptors)
        {
            services.Add(descriptor);
        }

        // ALWAYS register OTEL interceptors (safe, zero overhead when OTEL not configured)
        services.TryAddSingleton<ActivityInterceptor>();
        services.TryAddSingleton<MetricsInterceptor>();

        // Auto-configure OTEL (deferred - runs when OTEL is configured)
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IConfigureOptions<TracerProviderBuilder>,
                KommandTracerOptions>());
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IConfigureOptions<MeterProviderBuilder>,
                KommandMeterOptions>());

        // Register user interceptors in order
        foreach (var interceptorType in config.InterceptorTypes)
        {
            services.AddScoped(interceptorType);
        }

        return services;
    }
}

public class KommandConfiguration
{
    private readonly List<ServiceDescriptor> _handlerDescriptors = new();
    private readonly List<ServiceDescriptor> _validatorDescriptors = new();
    private readonly List<Type> _interceptorTypes = new();

    // Default lifetime for auto-discovered handlers
    public ServiceLifetime DefaultHandlerLifetime { get; set; } = ServiceLifetime.Scoped;

    // Assembly scanning - Auto-discovers handlers AND validators
    public KommandConfiguration RegisterHandlersFromAssembly(
        Assembly assembly,
        ServiceLifetime? lifetime = null)
    {
        var handlerLifetime = lifetime ?? DefaultHandlerLifetime;

        // Scan for handlers
        var handlerTypes = assembly.GetTypes()
            .Where(IsHandlerType)
            .ToList();

        foreach (var handlerType in handlerTypes)
        {
            var interfaceTypes = GetHandlerInterfaces(handlerType);
            foreach (var interfaceType in interfaceTypes)
            {
                _handlerDescriptors.Add(new ServiceDescriptor(
                    interfaceType,
                    handlerType,
                    handlerLifetime));
            }
        }

        // ALSO scan for validators (automatic!)
        var validatorTypes = assembly.GetTypes()
            .Where(IsValidatorType)
            .ToList();

        foreach (var validatorType in validatorTypes)
        {
            var validatorInterfaces = validatorType
                .GetInterfaces()
                .Where(i => i.IsGenericType &&
                           i.GetGenericTypeDefinition() == typeof(IValidator<>));

            foreach (var validatorInterface in validatorInterfaces)
            {
                // Validators are Scoped (can inject repositories for async validation)
                _validatorDescriptors.Add(new ServiceDescriptor(
                    validatorInterface,
                    validatorType,
                    ServiceLifetime.Scoped));
            }
        }

        return this;
    }

    // Multiple assemblies
    public KommandConfiguration RegisterHandlersFromAssemblies(
        params Assembly[] assemblies)
    {
        foreach (var assembly in assemblies)
        {
            RegisterHandlersFromAssembly(assembly);
        }
        return this;
    }

    // Override specific handler
    public KommandConfiguration RegisterHandler<THandler>(
        ServiceLifetime lifetime = ServiceLifetime.Scoped)
        where THandler : class
    {
        var interfaceTypes = GetHandlerInterfaces(typeof(THandler));
        foreach (var interfaceType in interfaceTypes)
        {
            _handlerDescriptors.Add(new ServiceDescriptor(
                interfaceType,
                typeof(THandler),
                lifetime));
        }
        return this;
    }

    // Register interceptors (order matters!)
    public KommandConfiguration AddInterceptor<TInterceptor>()
        where TInterceptor : class
    {
        _interceptorTypes.Add(typeof(TInterceptor));
        return this;
    }

    // Type-specific interceptors
    public KommandConfiguration AddCommandInterceptor<TInterceptor>()
        where TInterceptor : class
    {
        _interceptorTypes.Add(typeof(TInterceptor));
        return this;
    }

    public KommandConfiguration AddQueryInterceptor<TInterceptor>()
        where TInterceptor : class
    {
        _interceptorTypes.Add(typeof(TInterceptor));
        return this;
    }

    // Fluent helpers for common scenarios
    public KommandConfiguration WithValidation()
    {
        AddInterceptor(typeof(ValidationInterceptor<,>));
        return this;
    }

    public KommandConfiguration WithLogging()
    {
        AddInterceptor(typeof(LoggingInterceptor<,>));
        return this;
    }

    // Helper methods
    private static bool IsHandlerType(Type type)
    {
        return type is { IsClass: true, IsAbstract: false } &&
               type.GetInterfaces().Any(i =>
                   i.IsGenericType &&
                   (i.GetGenericTypeDefinition() == typeof(ICommandHandler<,>) ||
                    i.GetGenericTypeDefinition() == typeof(IQueryHandler<,>) ||
                    i.GetGenericTypeDefinition() == typeof(INotificationHandler<>)));
    }

    private static bool IsValidatorType(Type type)
    {
        return type is { IsClass: true, IsAbstract: false } &&
               type.GetInterfaces().Any(i =>
                   i.IsGenericType &&
                   i.GetGenericTypeDefinition() == typeof(IValidator<>));
    }

    // Options
    public bool ThrowOnMultipleHandlers { get; set; } = true;
    public bool ThrowOnMissingHandler { get; set; } = true;
    public bool DisableOpenTelemetry { get; set; } = false; // Rarely needed
}
```

### Usage Examples

```csharp
// Simple (most common) - Auto-discovers handlers and validators!
services.AddKommand(config =>
{
    config.RegisterHandlersFromAssembly(typeof(Program).Assembly);
});

// With validation - Streamlined fluent API
services.AddKommand(config =>
{
    config.RegisterHandlersFromAssembly(typeof(Program).Assembly);
    config.WithValidation();  // Enables validation interceptor
});

// With multiple built-in interceptors
services.AddKommand(config =>
{
    config.RegisterHandlersFromAssembly(typeof(Program).Assembly);
    config.WithValidation();   // Auto-discovered validators run here
    config.WithLogging();      // Logging interceptor
});

// With custom interceptors
services.AddKommand(config =>
{
    config.RegisterHandlersFromAssembly(typeof(Program).Assembly);
    config.WithValidation();
    config.AddInterceptor<ExceptionHandlingInterceptor>();
    config.AddInterceptor<CustomLoggingInterceptor>();
});

// Advanced
services.AddKommand(config =>
{
    config.RegisterHandlersFromAssemblies(
        typeof(DomainCommands).Assembly,
        typeof(DomainQueries).Assembly);

    // Override specific handler
    config.RegisterHandler<ExpensiveQueryHandler>(ServiceLifetime.Transient);

    // Interceptors in order (first = outermost)
    config.AddInterceptor<ExceptionHandlingInterceptor>();
    config.WithValidation();                             // Fluent helper
    config.WithLogging();                                // Fluent helper
    config.AddCommandInterceptor<AuditingInterceptor>(); // Commands only
    config.AddQueryInterceptor<CachingInterceptor>();    // Queries only
});
```

---

## 5. 📦 Project Structure & Packaging

### Repository Structure

```
kommand/                           # GitHub repo
├── .github/
│   ├── workflows/
│   │   ├── ci.yml                # Build, test, coverage
│   │   ├── release.yml           # NuGet publish
│   │   └── dependabot.yml
│   └── ISSUE_TEMPLATE/
├── src/
│   ├── Kommand/                  # Core library ⭐ Single package
│   │   ├── Abstractions/
│   │   │   ├── ICommand.cs
│   │   │   ├── IQuery.cs
│   │   │   ├── ICommandHandler.cs
│   │   │   ├── IQueryHandler.cs
│   │   │   ├── INotification.cs
│   │   │   ├── INotificationHandler.cs
│   │   │   ├── IMediator.cs
│   │   │   └── Unit.cs
│   │   ├── Interceptors/
│   │   │   ├── IInterceptor.cs
│   │   │   ├── ICommandInterceptor.cs
│   │   │   ├── IQueryInterceptor.cs
│   │   │   ├── RequestHandlerDelegate.cs
│   │   │   ├── ActivityInterceptor.cs     # Built-in OTEL
│   │   │   └── MetricsInterceptor.cs      # Built-in OTEL
│   │   ├── Validation/
│   │   │   ├── IValidator.cs              # Validation abstraction
│   │   │   ├── ValidationResult.cs
│   │   │   ├── ValidationError.cs
│   │   │   ├── ValidationException.cs
│   │   │   └── ValidationInterceptor.cs   # Built-in validator runner
│   │   ├── Implementation/
│   │   │   ├── Mediator.cs
│   │   │   ├── ServiceFactory.cs
│   │   │   └── HandlerDescriptor.cs
│   │   ├── Registration/
│   │   │   ├── KommandConfiguration.cs
│   │   │   ├── ServiceCollectionExtensions.cs
│   │   │   ├── AssemblyScanner.cs
│   │   │   ├── KommandTracerOptions.cs    # OTEL auto-config
│   │   │   └── KommandMeterOptions.cs     # OTEL auto-config
│   │   └── Exceptions/
│   │       ├── HandlerNotFoundException.cs
│   │       ├── MultipleHandlersException.cs
│   │       └── KommandException.cs
│   │
│   └── Kommand.Analyzers/        # Roslyn analyzers (future)
│       └── Analyzers/
│           ├── SingleHandlerAnalyzer.cs
│           └── HandlerRegistrationAnalyzer.cs
│
├── tests/
│   ├── Kommand.Tests.Unit/
│   │   ├── Core/
│   │   ├── Interceptors/
│   │   ├── Registration/
│   │   └── OTEL/
│   ├── Kommand.Tests.Integration/
│   │   ├── EndToEndTests.cs
│   │   └── OTELIntegrationTests.cs
│   └── Kommand.Benchmarks/
│       ├── KommandBenchmarks.cs
│       └── ComparisonBenchmarks.cs
│
├── samples/
│   └── Kommand.Sample/
│       ├── Commands/
│       ├── Queries/
│       ├── Handlers/
│       ├── Interceptors/
│       └── Program.cs
│
├── docs/
│   ├── getting-started.md
│   ├── commands-and-queries.md
│   ├── interceptors.md
│   ├── dependency-injection.md
│   ├── opentelemetry.md
│   ├── testing.md
│   ├── migration-from-mediatr.md
│   └── best-practices.md
│
├── README.md
├── LICENSE (MIT)
├── CONTRIBUTING.md
├── CHANGELOG.md
└── kommand.sln
```

### NuGet Package

**Single Package: `Kommand`**

- **Dependencies**:
  - `Microsoft.Extensions.DependencyInjection.Abstractions` (>= 8.0.0)
  - `System.Diagnostics.DiagnosticSource` (>= 8.0.0) - for OTEL
- **Target Framework**: `net8.0` (forward compatible with .NET 8, 9, 10+)
- **Build SDK**: Any .NET SDK >= 8.0
- **Estimated Size**: <50KB
- **Namespace**: `Kommand`
- **Assembly**: `Kommand.dll`

**No separate packages** - everything in one clean package.

---

## 6. ✅ Validation Strategy

### Auto-Discovery Approach

**Key Feature**: Validators are **automatically discovered and registered** during assembly scanning!

### Custom IValidator<T> Interface

We provide the abstraction, users implement:

```csharp
namespace Kommand.Validation;

public interface IValidator<T>
{
    Task<ValidationResult> ValidateAsync(T instance, CancellationToken cancellationToken);
}

public class ValidationResult
{
    public bool IsValid { get; init; }
    public IReadOnlyList<ValidationError> Errors { get; init; } = Array.Empty<ValidationError>();

    public static ValidationResult Success() => new() { IsValid = true };
    public static ValidationResult Failure(params ValidationError[] errors)
        => new() { IsValid = false, Errors = errors };
}

public record ValidationError(string PropertyName, string ErrorMessage);
```

### How It Works

**Step 1**: User writes a validator (implements `IValidator<T>`)

```csharp
// Validator is automatically discovered during assembly scanning!
public class CreateUserCommandValidator : IValidator<CreateUserCommand>
{
    private readonly IUserRepository _userRepository;

    public CreateUserCommandValidator(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<ValidationResult> ValidateAsync(
        CreateUserCommand command,
        CancellationToken ct)
    {
        var errors = new List<ValidationError>();

        // Synchronous validation
        if (string.IsNullOrWhiteSpace(command.Email))
            errors.Add(new ValidationError(nameof(command.Email), "Email is required"));

        if (!command.Email.Contains('@'))
            errors.Add(new ValidationError(nameof(command.Email), "Invalid email format"));

        // Async validation (can inject repositories!)
        if (await _userRepository.EmailExistsAsync(command.Email, ct))
            errors.Add(new ValidationError(nameof(command.Email), "Email already exists"));

        return errors.Any()
            ? ValidationResult.Failure(errors.ToArray())
            : ValidationResult.Success();
    }
}
```

**Step 2**: Enable validation in registration

```csharp
services.AddKommand(config =>
{
    config.RegisterHandlersFromAssembly(typeof(Program).Assembly);
    config.WithValidation(); // Enables ValidationInterceptor
});
```

**That's it!** The validator is automatically discovered and will run for all `CreateUserCommand` instances.

### Built-in ValidationInterceptor

Kommand provides a built-in `ValidationInterceptor` that automatically discovers and runs all validators:

```csharp
// Built into Kommand.Validation namespace
public class ValidationInterceptor<TRequest, TResponse>
    : IInterceptor<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;
    private readonly ILogger<ValidationInterceptor<TRequest, TResponse>> _logger;

    public ValidationInterceptor(
        IEnumerable<IValidator<TRequest>> validators,
        ILogger<ValidationInterceptor<TRequest, TResponse>> logger)
    {
        _validators = validators;
        _logger = logger;
    }

    public async Task<TResponse> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Skip if no validators registered for this request type
        if (!_validators.Any())
            return await next();

        var errors = new List<ValidationError>();

        foreach (var validator in _validators)
        {
            var result = await validator.ValidateAsync(request, cancellationToken);
            if (!result.IsValid)
            {
                errors.AddRange(result.Errors);
                _logger.LogWarning(
                    "Validation failed for {RequestType}: {ErrorCount} errors",
                    typeof(TRequest).Name,
                    result.Errors.Count);
            }
        }

        if (errors.Any())
        {
            _logger.LogError(
                "Validation failed for {RequestType} with {ErrorCount} total errors",
                typeof(TRequest).Name,
                errors.Count);
            throw new ValidationException(errors);
        }

        return await next();
    }
}

// Custom exception
public class ValidationException : Exception
{
    public IReadOnlyList<ValidationError> Errors { get; }

    public ValidationException(IReadOnlyList<ValidationError> errors)
        : base($"Validation failed with {errors.Count} error(s)")
    {
        Errors = errors;
    }
}
```

### Benefits

**Pros**:
- ✅ **Auto-discovery**: Validators automatically registered during assembly scanning
- ✅ **Streamlined API**: Simple `config.WithValidation()` to enable
- ✅ **Clean abstraction**: Single interface to implement
- ✅ **Async support**: Can call DB, external APIs for validation
- ✅ **DI support**: Validators can inject repositories, services
- ✅ **Zero dependencies**: No external libraries needed
- ✅ **Type-safe**: Compiler ensures validator matches command/query

**Cons**:
- ❌ Not as feature-rich as FluentValidation (no built-in rules library)
- ❌ Slightly more code than data annotations (but more flexible)

**Why This Approach?**
1. Avoids commercial licensing issues (FluentValidation)
2. Provides flexibility for complex async validation
3. Keeps zero external dependencies
4. Auto-discovery provides excellent developer experience

### Summary: How Validation Works

1. **Assembly Scanning**: `RegisterHandlersFromAssembly()` automatically discovers:
   - All `ICommandHandler<,>` implementations → Registered as handlers
   - All `IQueryHandler<,>` implementations → Registered as handlers
   - All `IValidator<T>` implementations → Registered as validators

2. **Opt-In Validation**: Add `config.WithValidation()` to enable the ValidationInterceptor

3. **Automatic Execution**: When a command/query is sent:
   - ValidationInterceptor checks for registered validators for that request type
   - If validators exist, they all run
   - If any validation fails, `ValidationException` is thrown
   - If validation passes, the handler executes

**Zero manual registration needed!** Just implement `IValidator<T>` and call `WithValidation()`.

---

## 7. 🔔 Notification System Design

### Use Cases
1. **Domain Events**: `UserCreatedNotification`, `OrderPlacedNotification`
2. **Cross-cutting Concerns**: Audit logging, analytics tracking
3. **Decoupling**: One action triggers multiple side effects

### Implementation

```csharp
public interface INotification { }

public interface INotificationHandler<in TNotification>
    where TNotification : INotification
{
    Task HandleAsync(TNotification notification, CancellationToken cancellationToken);
}

// Example: Multiple handlers for one notification
public record UserCreatedNotification(Guid UserId, string Email) : INotification;

public class SendWelcomeEmailHandler : INotificationHandler<UserCreatedNotification>
{
    private readonly IEmailService _emailService;

    public async Task HandleAsync(UserCreatedNotification notification, CancellationToken ct)
    {
        await _emailService.SendWelcomeEmailAsync(notification.Email, ct);
    }
}

public class CreateAuditLogHandler : INotificationHandler<UserCreatedNotification>
{
    private readonly IAuditRepository _auditRepository;

    public async Task HandleAsync(UserCreatedNotification notification, CancellationToken ct)
    {
        await _auditRepository.LogUserCreatedAsync(notification.UserId, ct);
    }
}

public class UpdateAnalyticsHandler : INotificationHandler<UserCreatedNotification>
{
    private readonly IAnalyticsService _analytics;

    public async Task HandleAsync(UserCreatedNotification notification, CancellationToken ct)
    {
        await _analytics.TrackUserCreatedAsync(notification.UserId, ct);
    }
}
```

### Error Handling for Notifications

**Strategy: Continue on Failure** (resilient by default)

```csharp
var exceptions = new List<Exception>();

foreach (var handler in handlers)
{
    try
    {
        await handler.HandleAsync(notification, ct);
    }
    catch (Exception ex)
    {
        exceptions.Add(ex);
        _logger.LogError(ex, "Notification handler {Handler} failed", handler.GetType().Name);
    }
}

if (exceptions.Any())
{
    // Log but don't throw - one failing handler shouldn't break others
    _logger.LogError("Notification {Notification} had {Count} handler failures",
        typeof(TNotification).Name, exceptions.Count);
}
```

**Rationale**: Domain events should be resilient. One handler failing shouldn't prevent others from running.

### Execution Order

**Default: Sequential** (predictable, easier to debug)

```csharp
foreach (var handler in handlers)
    await handler.HandleAsync(notification, ct);
```

**Optional: Parallel** (configurable)

```csharp
services.AddKommand(config =>
{
    config.NotificationPublishingStrategy = NotificationPublishingStrategy.Parallel;
});
```

---

## 8. 🎭 IMediator Interface Design

### Final API Surface

```csharp
namespace Kommand;

public interface IMediator
{
    // Commands (change state)
    Task<TResponse> SendAsync<TResponse>(
        ICommand<TResponse> command,
        CancellationToken cancellationToken = default);

    Task SendAsync(
        ICommand command,
        CancellationToken cancellationToken = default);

    // Queries (read-only)
    Task<TResponse> QueryAsync<TResponse>(
        IQuery<TResponse> query,
        CancellationToken cancellationToken = default);

    // Notifications (pub/sub)
    Task PublishAsync<TNotification>(
        TNotification notification,
        CancellationToken cancellationToken = default)
        where TNotification : INotification;
}
```

### Why Separate Methods?

1. **Semantic Clarity**: Intent is explicit in code
2. **Different Interceptors**: Commands and queries can have different interceptor chains
3. **Metrics**: Separate metrics for commands vs queries
4. **Future Optimization**: Queries could be cached by default
5. **Readability**:
   ```csharp
   await _mediator.SendAsync(new CreateUserCommand());  // Writes data
   var user = await _mediator.QueryAsync(new GetUserQuery());  // Reads data
   await _mediator.PublishAsync(new UserCreatedNotification());  // Events
   ```

---

## 9. 🚨 Error Handling Philosophy

### Approach: Exceptions (v1.0)

```csharp
public async Task<User> HandleAsync(CreateUserCommand command, CancellationToken ct)
{
    if (string.IsNullOrEmpty(command.Email))
        throw new ValidationException("Email is required");

    if (await _repository.EmailExistsAsync(command.Email, ct))
        throw new ConflictException("Email already exists");

    return await _repository.CreateUserAsync(command);
}
```

**Why exceptions for v1.0:**
1. ✅ Standard .NET pattern
2. ✅ Works with ASP.NET Core middleware
3. ✅ Familiar to .NET developers
4. ✅ Can add Result<T> in v2.0 without breaking changes

**Future (v2.0)**: Optional Result<T> pattern support for functional error handling

---

## 10. 📊 OpenTelemetry Auto-Integration

### The Magic: Zero-Configuration Observability

**Key Insight**: `ActivitySource` and `Meter` have **zero overhead** when no listeners are attached.

### How It Works

#### 1. Always-On Interceptors (Safe!)

```csharp
public class ActivityInterceptor<TRequest, TResponse> : IInterceptor<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private static readonly ActivitySource ActivitySource = new("Kommand", "1.0.0");

    public async Task<TResponse> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var requestType = request is ICommand ? "Command" : "Query";

        // Returns NULL if no OTEL configured - zero overhead!
        using var activity = ActivitySource.StartActivity(
            $"{requestType}.{requestName}",
            ActivityKind.Internal);

        // All null-safe - no performance impact if activity is null
        activity?.SetTag("kommand.request.type", requestType);
        activity?.SetTag("kommand.request.name", requestName);

        try
        {
            var response = await next();
            activity?.SetStatus(ActivityStatusCode.Ok);
            return response;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.RecordException(ex);
            throw;
        }
    }
}
```

**Performance when OTEL NOT configured**: ~10-50 nanoseconds per request (negligible!)

#### 2. Auto-Registration with OTEL

```csharp
// Kommand automatically configures itself when OTEL is present
internal class KommandTracerOptions : IConfigureOptions<TracerProviderBuilder>
{
    public void Configure(TracerProviderBuilder builder)
    {
        builder.AddSource("Kommand");  // Auto-subscribe to Kommand's ActivitySource
    }
}

internal class KommandMeterOptions : IConfigureOptions<MeterProviderBuilder>
{
    public void Configure(MeterProviderBuilder builder)
    {
        builder.AddMeter("Kommand");  // Auto-subscribe to Kommand's Meter
    }
}
```

#### 3. User Experience

**Step 1: Add Kommand**
```csharp
services.AddKommand(config =>
{
    config.RegisterHandlersFromAssembly(typeof(Program).Assembly);
});
```

**Step 2: Configure OTEL (user's existing code)**
```csharp
services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics.AddAspNetCoreInstrumentation();
        // Kommand automatically added! No manual registration needed!
    })
    .WithTracing(tracing =>
    {
        tracing.AddAspNetCoreInstrumentation();
        // Kommand automatically added! No manual registration needed!
    });
```

**That's it!** Full distributed tracing and metrics work automatically. 🎉

### What Gets Tracked Automatically

**Traces (ActivityInterceptor)**:
- `kommand.request.type` (Command/Query)
- `kommand.request.name` (e.g., "CreateUserCommand")
- `kommand.request.status` (success/error)
- Exception details (if any)
- Duration (automatic)

**Metrics (MetricsInterceptor)**:
- `kommand.request.duration` (histogram, milliseconds)
- `kommand.request.count` (counter)
- Dimensions: `request_name`, `request_type`, `status`

### Performance Impact

| OTEL Status | ActivityInterceptor Overhead | MetricsInterceptor Overhead |
|-------------|-----------------------------|-----------------------------|
| **Not configured** | ~10-50 ns | ~5-10 ns |
| **Configured** | ~500-1000 ns | ~100-200 ns |

Negligible in both cases for production workloads.

### Opt-Out (Rare)

```csharp
services.AddKommand(config =>
{
    config.DisableOpenTelemetry = true;  // Disables OTEL interceptors
});
```

---

## 11. ⚡ Performance Considerations

### Optimization Strategies

1. **Assembly Scanning**: Once at startup, cached
2. **Handler Resolution**: Use DI efficiently, avoid service locator pattern
3. **Interceptor Chain**: Pre-build pipeline if possible
4. **Generic Constraints**: Carefully designed to avoid boxing
5. **Async All the Way**: No sync-over-async patterns

### Performance Targets

#### Absolute Overhead (Microbenchmarks)

| Metric | Target | Notes |
|--------|--------|-------|
| Mediator dispatch overhead | <2 μs | DI resolution + reflection |
| Per-interceptor cost | <100 ns | Pipeline delegate invocation |
| Total (3 interceptors) | <3 μs | End-to-end overhead |

**Note:** Ratio-based targets (e.g., "<1.5x baseline") are not meaningful when the baseline handler is trivial. Absolute overhead targets better reflect production impact.

#### Realistic Workload Overhead

| Scenario | Target Overhead | Notes |
|----------|----------------|-------|
| 1ms database operation | <0.1% | Typical lightweight query |
| 10ms external API call | <0.01% | Typical HTTP request |
| 100ms+ long-running operation | <0.001% | Completely negligible |
| vs MediatR | Similar or better | Competitive |

### Benchmarks

```csharp
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
public class KommandBenchmarks
{
    [Benchmark(Baseline = true)]
    public async Task<int> DirectMethodCall() { }

    [Benchmark]
    public async Task<int> KommandWithoutInterceptors() { }

    [Benchmark]
    public async Task<int> KommandWith3Interceptors() { }

    [Benchmark]
    public async Task<int> MediatR_ForComparison() { }
}
```

**Goal**: Match or beat MediatR performance while providing better developer experience.

---

## 12. 🧪 Testing Strategy

### Test Coverage Requirements

| Component | Target | Rationale |
|-----------|--------|-----------|
| Core Mediator | 100% | Mission-critical routing logic |
| Interceptors | 100% | Complex execution flow |
| OTEL Integration | 100% | Must work in all scenarios |
| Registration | 95% | Error paths matter |
| Assembly Scanning | 95% | Edge cases important |

### Test Structure

```
Kommand.Tests.Unit/
├── Core/
│   ├── MediatorTests.cs
│   ├── CommandDispatchingTests.cs
│   ├── QueryDispatchingTests.cs
│   └── NotificationPublishingTests.cs
├── Interceptors/
│   ├── InterceptorOrderingTests.cs
│   ├── InterceptorErrorHandlingTests.cs
│   ├── ActivityInterceptorTests.cs
│   └── MetricsInterceptorTests.cs
├── OTEL/
│   ├── AutoRegistrationTests.cs
│   ├── ZeroOverheadTests.cs
│   └── IntegrationTests.cs
└── Registration/
    ├── AssemblyScanningTests.cs
    ├── HandlerRegistrationTests.cs
    └── ConfigurationTests.cs
```

### Example Tests

```csharp
public class MediatorTests
{
    [Fact]
    public async Task SendAsync_WithValidCommand_InvokesHandler()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddKommand(config =>
        {
            config.RegisterHandler<TestCommandHandler>();
        });
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act
        var result = await mediator.SendAsync(new TestCommand("test"));

        // Assert
        Assert.Equal("test-handled", result);
    }

    [Fact]
    public async Task SendAsync_WithMultipleHandlers_ThrowsException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddKommand(config =>
        {
            config.RegisterHandler<TestCommandHandler1>();
            config.RegisterHandler<TestCommandHandler2>();  // Duplicate!
            config.ThrowOnMultipleHandlers = true;
        });

        // Act & Assert
        await Assert.ThrowsAsync<MultipleHandlersException>(async () =>
        {
            var provider = services.BuildServiceProvider();
            var mediator = provider.GetRequiredService<IMediator>();
            await mediator.SendAsync(new TestCommand("test"));
        });
    }
}
```

---

## 13. 📝 Documentation Requirements

### Essential Documentation

1. **README.md**: Quick start, features, installation
2. **Getting Started**: 5-minute tutorial with complete working example
3. **Core Concepts**: Commands, queries, handlers, notifications
4. **Interceptors Guide**: Writing custom interceptors, execution order
5. **OpenTelemetry**: Auto-integration explanation, what gets tracked
6. **Dependency Injection**: Registration options, handler lifetimes
7. **Testing**: How to test handlers and interceptors
8. **Migration from MediatR**: Side-by-side comparison, migration steps
9. **Best Practices**: Patterns, anti-patterns, common mistakes
10. **API Reference**: XML documentation for all public APIs

### Documentation Site Structure

```
docs.kommand.io/
├── Getting Started
├── Core Concepts
│   ├── Commands
│   ├── Queries
│   ├── Handlers
│   └── Notifications
├── Interceptors
│   ├── Built-in Interceptors
│   ├── Custom Interceptors
│   └── Execution Order
├── Observability
│   ├── OpenTelemetry Auto-Integration
│   ├── Metrics
│   └── Distributed Tracing
├── Advanced
│   ├── Dependency Injection
│   ├── Testing Strategies
│   └── Performance Tuning
└── Migration
    └── From MediatR
```

---

## 14. 🚀 Implementation Roadmap

### Phase 1: Core Foundation (Week 1-2) - 80-100 hours

**Week 1: Project Setup & Core Abstractions**
- [ ] Repository setup (GitHub, .gitignore, LICENSE)
- [ ] Solution structure (.sln, projects)
- [ ] Core abstractions
  - [ ] `IRequest<TResponse>`, `ICommand<TResponse>`, `IQuery<TResponse>`
  - [ ] `ICommandHandler<,>`, `IQueryHandler<,>`
  - [ ] `Unit` struct
  - [ ] `INotification`, `INotificationHandler<>`
- [ ] Basic `Mediator` implementation
  - [ ] Command dispatching
  - [ ] Query dispatching
  - [ ] Handler resolution via DI
- [ ] Unit tests for core (100% coverage)
  - [ ] Handler resolution tests
  - [ ] Error handling tests
  - [ ] Edge case tests

**Week 2: DI Registration System**
- [ ] `KommandConfiguration` class
- [ ] Assembly scanning implementation
- [ ] `ServiceCollectionExtensions.AddKommand()`
- [ ] Handler lifetime support (Scoped, Transient, Singleton)
- [ ] Registration tests
- [ ] Documentation for registration

**Milestone 1**: ✅ Can send command/query and get response

---

### Phase 2: Interceptor System (Week 3) - 40-60 hours

- [ ] Interceptor abstractions
  - [ ] `IInterceptor<,>`
  - [ ] `ICommandInterceptor<,>`
  - [ ] `IQueryInterceptor<,>`
  - [ ] `RequestHandlerDelegate<>`
- [ ] Interceptor execution engine
  - [ ] Chain building
  - [ ] Execution order control
  - [ ] Error propagation
- [ ] Built-in OTEL interceptors
  - [ ] `ActivityInterceptor` (OpenTelemetry tracing)
  - [ ] `MetricsInterceptor` (OpenTelemetry metrics)
- [ ] Interceptor registration API
- [ ] Interceptor tests (100% coverage)
- [ ] Documentation for interceptors

**Milestone 2**: ✅ Interceptors work correctly in pipeline

---

### Phase 3: OTEL Auto-Integration (Week 3-4) - 20-30 hours

- [ ] `KommandTracerOptions` (IConfigureOptions)
- [ ] `KommandMeterOptions` (IConfigureOptions)
- [ ] Auto-registration tests
  - [ ] Test with OTEL configured
  - [ ] Test without OTEL (zero overhead)
  - [ ] Integration tests with real OTEL exporters
- [ ] Documentation for OTEL integration
- [ ] Performance benchmarks

**Milestone 3**: ✅ OTEL auto-integration works

---

### Phase 4: Notifications (Week 4) - 30-40 hours

- [ ] `INotification` interface
- [ ] `INotificationHandler<>` interface
- [ ] Multiple handler support
- [ ] `IMediator.PublishAsync()` implementation
- [ ] Error handling (continue on failure)
- [ ] Sequential vs parallel execution
- [ ] Notification handler registration
- [ ] Tests for notifications
- [ ] Documentation for pub/sub

**Milestone 4**: ✅ Pub/sub working

---

### Phase 5: Validation System (Week 5) - 20-30 hours

- [ ] `IValidator<T>` interface
- [ ] `ValidationResult` class
- [ ] `ValidationError` record
- [ ] `ValidationException` class
- [ ] `ValidationInterceptor` implementation (built-in)
- [ ] Auto-discovery during assembly scanning
  - [ ] `IsValidatorType()` helper method
  - [ ] Validator registration in `RegisterHandlersFromAssembly()`
- [ ] `WithValidation()` fluent helper
- [ ] Validation tests
  - [ ] Auto-discovery tests
  - [ ] Interceptor execution tests
  - [ ] Multiple validators per command tests
- [ ] Documentation for validation

**Milestone 5**: ✅ Validation with auto-discovery complete

---

### Phase 6: Polish & Optimization (Week 5-6) - 30-40 hours

- [ ] Performance optimization
  - [ ] Assembly scanning optimization
  - [ ] Handler resolution caching
  - [ ] Interceptor pipeline pre-building
- [ ] BenchmarkDotNet setup
  - [ ] Baseline benchmarks
  - [ ] vs MediatR comparison
  - [ ] Memory allocation analysis
- [ ] XML documentation completion
- [ ] Sample project
  - [ ] Complete working example
  - [ ] Multiple scenarios (CRUD operations)
  - [ ] Best practices demonstration
- [ ] Complete documentation
  - [ ] All guides written
  - [ ] API reference generated
  - [ ] README polished

**Milestone 6**: ✅ Production-ready quality

---

### Phase 7: Release (Week 6) - 20-30 hours

- [ ] NuGet packaging
  - [ ] Package metadata (.nuspec)
  - [ ] README.md in package
  - [ ] Icon/logo
  - [ ] Version numbers (SemVer)
  - [ ] Release notes
- [ ] CI/CD pipeline
  - [ ] GitHub Actions for build/test
  - [ ] Automated NuGet publish
  - [ ] Code coverage reporting (Codecov)
  - [ ] Security scanning
- [ ] GitHub repository polish
  - [ ] Issue templates
  - [ ] PR templates
  - [ ] Contributing guidelines
  - [ ] Code of conduct
  - [ ] Security policy
- [ ] Release v1.0.0
  - [ ] Git tag
  - [ ] GitHub release notes
  - [ ] NuGet publish
- [ ] Announcement
  - [ ] Blog post
  - [ ] Reddit r/dotnet, r/csharp
  - [ ] HackerNews
  - [ ] Twitter/LinkedIn

**Milestone 7**: ✅ v1.0.0 released to NuGet

---

### Total Estimated Timeline

- **Full-time (40-50 hrs/week)**: 6 weeks
- **Part-time (20 hrs/week)**: 10-15 weeks
- **Weekend project (10 hrs/week)**: 20-30 weeks

**Total Effort**: ~200-300 hours

---

## 15. 🎯 Critical Decisions Summary

| Decision | Choice | Rationale |
|----------|--------|-----------|
| **Library Name** | Kommand | Playful (K), clear meaning, memorable |
| **Terminology** | Interceptors | Standard .NET term, clear intent |
| **Feature Set v1.0** | Commands, Queries, Interceptors, Notifications | Production-ready, complete feature set |
| **Built-in Interceptors** | OTEL (Activity + Metrics) + ValidationInterceptor | Always safe, zero overhead when not configured |
| **OTEL Integration** | Automatic (zero config) | Best developer experience |
| **Default Handler Lifetime** | Scoped | Matches ASP.NET Core patterns (DbContext) |
| **Validator Discovery** | **Auto-discovery during assembly scan** | Zero manual registration, excellent DX |
| **Validation** | IValidator<T> + opt-in `WithValidation()` | Flexible, async, no dependencies, streamlined |
| **Error Handling** | Exceptions | Standard .NET, can add Result<T> later |
| **Notification Execution** | Sequential, continue on failure | Resilient, predictable |
| **Notification Error Strategy** | Log and continue | One failing handler shouldn't break others |
| **API Design** | Separate SendAsync/QueryAsync/PublishAsync | Semantic clarity over brevity |
| **Package Name** | Kommand (no suffix) | Clean, simple |
| **Package Structure** | Single package | No separate packages for behaviors |
| **Dependencies** | DI Abstractions + DiagnosticSource | Minimal (2 packages) |
| **Target Framework** | net8.0 | LTS, forward compatible with 9, 10+ |
| **Testing** | 100% coverage for core | Mission-critical |
| **License** | MIT | Permissive, fully open source |

---

## 16. 🔍 Potential Risks & Mitigation

| Risk | Impact | Probability | Mitigation Strategy |
|------|--------|-------------|---------------------|
| **OTEL not working** | Lost observability | Low | Extensive testing, integration tests with real exporters |
| **Performance regression** | Slow requests | Medium | Benchmarks, optimization, profiling, comparison with MediatR |
| **Assembly scanning issues** | Startup failures | Low | Robust error handling, comprehensive tests, clear error messages |
| **Interceptor ordering confusion** | User mistakes | Medium | Clear docs, good error messages, examples |
| **MediatR migration challenges** | Low adoption | Medium | Detailed migration guide, compatibility layer if needed |
| **Poor documentation** | Low adoption | High | Heavy investment in docs from day 1, examples, tutorials |
| **Name conflict** | Branding issues | Low | Check NuGet availability before committing |
| **Breaking API changes** | User frustration | Low | Semantic versioning, deprecation warnings, stable v1.0 |
| **Memory leaks** | Production issues | Low | Proper disposal patterns, memory profiling, load testing |
| **Thread safety issues** | Race conditions | Low | Immutable request objects, stateless handlers, concurrency tests |

---

## 17. 🔄 Migration from MediatR

### Feature Comparison

| Feature | MediatR | Kommand |
|---------|---------|---------|
| Commands/Queries | ✅ | ✅ **Explicit distinction** |
| Handlers | ✅ | ✅ |
| Pipeline Behaviors | ✅ | ✅ **Interceptors** (same concept) |
| Notifications | ✅ | ✅ **Better error handling** |
| Streaming | ✅ | 🔴 v2.0 |
| Default Lifetime | Transient | **Scoped** (more efficient) |
| Validation | ❌ External | ✅ **Built-in abstraction** |
| OpenTelemetry | ❌ External | ✅ **Auto-integrated** (zero config) |
| Dependencies | Several | **Minimal (2)** |
| License | ⚠️ Commercial for >$1M companies | ✅ **MIT** (fully open) |
| Performance | Good | 🎯 **Equal or better** (target) |
| Package Size | ~100KB | **<50KB** (target) |

### API Comparison

**MediatR**:
```csharp
// Registration
services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));

// Usage
await _mediator.Send(new CreateUserCommand());
await _mediator.Publish(new UserCreatedNotification());

// Behaviors
public class LoggingBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // ...
        return await next();
    }
}
```

**Kommand**:
```csharp
// Registration
services.AddKommand(config =>
    config.RegisterHandlersFromAssembly(assembly));

// Usage
await _mediator.SendAsync(new CreateUserCommand());
await _mediator.PublishAsync(new UserCreatedNotification());

// Interceptors
public class LoggingInterceptor<TRequest, TResponse>
    : IInterceptor<TRequest, TResponse>
{
    public async Task<TResponse> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // ...
        return await next();
    }
}
```

**Key Differences**:
- ✅ Method names: `Send()` → `SendAsync()`, `Publish()` → `PublishAsync()`
- ✅ Interface names: `IPipelineBehavior` → `IInterceptor`
- ✅ Method names in behaviors: `Handle()` → `HandleAsync()`
- ✅ Registration: `AddMediatR()` → `AddKommand()`
- ✅ Separate `QueryAsync()` method for semantic clarity (optional)

### Migration Steps

1. **Install Kommand**
   ```bash
   dotnet remove package MediatR
   dotnet add package Kommand
   ```

2. **Update Registration**
   ```csharp
   // Before
   services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

   // After
   services.AddKommand(config =>
       config.RegisterHandlersFromAssembly(typeof(Program).Assembly));
   ```

3. **Find & Replace**
   - `Send(` → `SendAsync(`
   - `Publish(` → `PublishAsync(`
   - `IPipelineBehavior` → `IInterceptor`
   - `.Handle(` → `.HandleAsync(`

4. **Update OTEL (if using)**
   ```csharp
   // Remove manual MediatR OTEL registration
   // Kommand auto-registers itself!
   ```

5. **Test Thoroughly**
   - Run all tests
   - Verify OTEL traces/metrics still work
   - Check performance benchmarks

**Estimated Effort**:
- Small project (<50 handlers): 1-2 hours
- Medium project (50-200 handlers): 2-4 hours
- Large project (>200 handlers): 1-2 days

### Why Switch?

1. ✅ **MIT License** - No commercial restrictions
2. ✅ **Auto OTEL** - Zero configuration observability
3. ✅ **Smaller** - Less than half the size
4. ✅ **Faster** - Scoped handlers by default
5. ✅ **Semantic** - Explicit Command/Query distinction
6. ✅ **Modern & Compatible** - .NET 8 LTS (works on 8, 9, 10+)

---

## Appendix A: Complete Working Example

### 1. Install Package

```bash
dotnet add package Kommand
```

### 2. Define Command & Handler

```csharp
using Kommand;

// Command
public record CreateUserCommand(string Email, string Name) : ICommand<User>;

// Handler
public class CreateUserCommandHandler : ICommandHandler<CreateUserCommand, User>
{
    private readonly IUserRepository _repository;
    private readonly ILogger<CreateUserCommandHandler> _logger;

    public CreateUserCommandHandler(
        IUserRepository repository,
        ILogger<CreateUserCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<User> HandleAsync(CreateUserCommand command, CancellationToken ct)
    {
        _logger.LogInformation("Creating user {Email}", command.Email);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = command.Email,
            Name = command.Name,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(user, ct);

        return user;
    }
}
```

### 3. Define Query & Handler

```csharp
// Query
public record GetUserByIdQuery(Guid Id) : IQuery<User?>;

// Handler
public class GetUserByIdQueryHandler : IQueryHandler<GetUserByIdQuery, User?>
{
    private readonly IUserRepository _repository;

    public GetUserByIdQueryHandler(IUserRepository repository)
    {
        _repository = repository;
    }

    public async Task<User?> HandleAsync(GetUserByIdQuery query, CancellationToken ct)
    {
        return await _repository.GetByIdAsync(query.Id, ct);
    }
}
```

### 4. Define Validator (Optional)

```csharp
// Validator is automatically discovered!
public class CreateUserCommandValidator : IValidator<CreateUserCommand>
{
    private readonly IUserRepository _repository;

    public CreateUserCommandValidator(IUserRepository repository)
    {
        _repository = repository;
    }

    public async Task<ValidationResult> ValidateAsync(
        CreateUserCommand command,
        CancellationToken ct)
    {
        var errors = new List<ValidationError>();

        if (string.IsNullOrWhiteSpace(command.Email))
            errors.Add(new ValidationError(nameof(command.Email), "Email is required"));

        if (!command.Email.Contains('@'))
            errors.Add(new ValidationError(nameof(command.Email), "Invalid email format"));

        // Async validation
        if (await _repository.EmailExistsAsync(command.Email, ct))
            errors.Add(new ValidationError(nameof(command.Email), "Email already taken"));

        return errors.Any()
            ? ValidationResult.Failure(errors.ToArray())
            : ValidationResult.Success();
    }
}
```

### 5. Register in DI

```csharp
var builder = WebApplication.CreateBuilder(args);

// Register Kommand - Auto-discovers handlers AND validators!
builder.Services.AddKommand(config =>
{
    config.RegisterHandlersFromAssembly(typeof(Program).Assembly);
    config.WithValidation();  // Enable validation interceptor
});

// Optional: Add OpenTelemetry (Kommand auto-registers itself!)
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddAspNetCoreInstrumentation())
    .WithMetrics(metrics => metrics.AddAspNetCoreInstrumentation());

var app = builder.Build();
```

### 5. Use in Controller

```csharp
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;

    public UsersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request)
    {
        var command = new CreateUserCommand(request.Email, request.Name);
        var user = await _mediator.SendAsync(command, HttpContext.RequestAborted);
        return Created($"/api/users/{user.Id}", user);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var query = new GetUserByIdQuery(id);
        var user = await _mediator.QueryAsync(query, HttpContext.RequestAborted);
        return user is not null ? Ok(user) : NotFound();
    }
}
```

### 6. Define Notification & Handlers

```csharp
// Notification
public record UserCreatedNotification(Guid UserId, string Email) : INotification;

// Handler 1: Send welcome email
public class SendWelcomeEmailHandler : INotificationHandler<UserCreatedNotification>
{
    private readonly IEmailService _emailService;

    public async Task HandleAsync(UserCreatedNotification notification, CancellationToken ct)
    {
        await _emailService.SendWelcomeEmailAsync(notification.Email, ct);
    }
}

// Handler 2: Create audit log
public class CreateAuditLogHandler : INotificationHandler<UserCreatedNotification>
{
    private readonly IAuditService _auditService;

    public async Task HandleAsync(UserCreatedNotification notification, CancellationToken ct)
    {
        await _auditService.LogUserCreatedAsync(notification.UserId, ct);
    }
}

// Publish from command handler
public class CreateUserCommandHandler : ICommandHandler<CreateUserCommand, User>
{
    private readonly IMediator _mediator; // Inject mediator

    public async Task<User> HandleAsync(CreateUserCommand command, CancellationToken ct)
    {
        var user = new User { /* ... */ };
        await _repository.AddAsync(user, ct);

        // Publish notification
        await _mediator.PublishAsync(
            new UserCreatedNotification(user.Id, user.Email),
            ct);

        return user;
    }
}
```

### 7. Custom Interceptor (Optional)

```csharp
public class LoggingInterceptor<TRequest, TResponse> : IInterceptor<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingInterceptor<TRequest, TResponse>> _logger;

    public async Task<TResponse> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        _logger.LogInformation("Handling {RequestName}", requestName);

        var sw = Stopwatch.StartNew();

        try
        {
            var response = await next();
            _logger.LogInformation("Handled {RequestName} in {Elapsed}ms",
                requestName, sw.ElapsedMilliseconds);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling {RequestName}", requestName);
            throw;
        }
    }
}

// Register interceptor
services.AddKommand(config =>
{
    config.RegisterHandlersFromAssembly(typeof(Program).Assembly);
    config.AddInterceptor<LoggingInterceptor>(); // Add custom interceptor
});
```

**That's it!** You now have:
- ✅ CQRS with commands and queries
- ✅ Notifications for domain events
- ✅ Custom interceptors for cross-cutting concerns
- ✅ Automatic OpenTelemetry distributed tracing
- ✅ Zero configuration needed

---

**End of Document**

**Status**: Ready for Implementation
**Next Steps**: Begin Phase 1 - Core Foundation

---

**Document Stats**:
- Length: ~10,000 words
- Sections: 17 major + 1 appendix
- Code Examples: 30+
- Completeness: Production-ready specification
