# Changelog

All notable changes to Kommand will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.0.0] - TBD

### Added

#### Core Features
- **CQRS Support** - Explicit separation of commands (`ICommand<TResponse>`) and queries (`IQuery<TResponse>`)
  - Commands for write operations that change state
  - Queries for read-only data retrieval
  - Void commands using `Unit` type for consistency
- **Mediator Pattern** - Single entry point (`IMediator`) for dispatching all requests
  - `SendAsync<TResponse>(ICommand<TResponse>)` - Execute commands with results
  - `SendAsync(ICommand)` - Execute void commands
  - `QueryAsync<TResponse>(IQuery<TResponse>)` - Execute queries
  - `PublishAsync<TNotification>(TNotification)` - Publish notifications to multiple handlers
- **Pub/Sub Notifications** - Domain events with `INotification` and `INotificationHandler<T>`
  - Multiple handlers can subscribe to the same notification
  - Fire-and-forget behavior with resilient error handling
  - Continues execution even if individual handlers fail

#### Interceptor System
- **Flexible Interceptor Pipeline** - Cross-cutting concerns via `IInterceptor<TRequest, TResponse>`
  - `ICommandInterceptor<TCommand, TResponse>` - Command-specific interceptors
  - `IQueryInterceptor<TQuery, TResponse>` - Query-specific interceptors
  - Reverse-order execution (first registered = outermost layer)
  - Support for multiple interceptors per request
  - Built-in validation, metrics, and activity tracing interceptors

#### OpenTelemetry Integration
- **Zero-Config Observability** - Built-in OpenTelemetry support with zero overhead when disabled
  - `ActivityInterceptor<TRequest, TResponse>` - Automatic distributed tracing spans
    - Captures request type, handler type, and operation duration
    - Compatible with any OTEL-enabled APM (Jaeger, Zipkin, Application Insights, etc.)
  - `MetricsInterceptor<TRequest, TResponse>` - Built-in performance metrics
    - Request duration histograms
    - Request counters by type
    - Success/failure tracking
  - Auto-registration when OpenTelemetry is configured in your application
  - ~10-50ns overhead when OTEL is not configured (null-safe pattern)

#### Validation System
- **Custom Validation Framework** - No external dependencies (no FluentValidation required)
  - `IValidator<TRequest>` - Async validation interface
  - `ValidationResult` - Rich error details with property names and messages
  - `ValidationException` - Strongly-typed validation failures
  - `ValidationInterceptor<TRequest, TResponse>` - Automatic validation execution
  - Error collection (not fail-fast) - collects all validation errors before throwing
  - Auto-discovery via assembly scanning
  - Async validation support for database checks and complex rules

#### Dependency Injection
- **Auto-Discovery** - Assembly scanning automatically registers handlers and validators
  - `RegisterHandlersFromAssembly(Assembly)` - Discovers all handlers in an assembly
  - Supports `ICommandHandler<,>`, `IQueryHandler<,>`, `INotificationHandler<>`, and `IValidator<>`
  - **Scoped lifetime by default** (not Transient) - enables DbContext injection
  - Configurable lifetimes per handler type if needed
- **Fluent Configuration API** - `KommandConfiguration` for pipeline setup
  - `WithValidation()` - Enable validation interceptor
  - `AddInterceptor<T>()` - Register custom interceptors
  - Chainable API for clean configuration

#### Developer Experience
- **Comprehensive XML Documentation** - Full IntelliSense support for all public APIs
  - Detailed summaries, remarks, and examples
  - Usage guidance and best practices
  - Exception documentation
- **Strongly-Typed Exceptions** - Better error handling and debugging
  - `KommandException` - Base class for all Kommand exceptions
  - `HandlerNotFoundException` - Thrown when no handler is registered (includes `RequestType` property)
  - `ValidationException` - Validation failures with detailed error collection
- **Unit Type** - `Unit` struct for void operations (similar to F# and functional programming)
  - Maintains type system consistency
  - Avoids `Task<object>` anti-pattern

### Technical Details

#### Architecture
- **Zero External Dependencies** - Only Microsoft.Extensions abstractions
  - `Microsoft.Extensions.DependencyInjection.Abstractions` (8.0.0)
  - `System.Diagnostics.DiagnosticSource` (8.0.0)
  - `Microsoft.Extensions.Logging.Abstractions` (8.0.0)
- **Target Framework** - .NET 8.0 LTS (forward compatible with .NET 9, 10+)
- **Single Package** - Everything in one NuGet package: `Kommand`
- **Package Size** - <50KB with zero runtime dependencies

#### Performance
- **Low Overhead** - Designed for production use
  - <1.5x overhead vs direct handler calls (without interceptors)
  - <2.0x overhead with interceptor pipeline
  - Reflection-based handler resolution (cached in future versions)
  - Null-safe OTEL pattern ensures zero cost when not configured

#### Quality
- **Comprehensive Test Suite** - 110+ tests with high coverage
  - Unit tests for all core components
  - Integration tests for end-to-end scenarios
  - Error handling and edge case coverage
  - >80% code coverage
- **Production Ready** - Battle-tested patterns
  - Clean build with zero warnings (`TreatWarningsAsErrors`)
  - Full XML documentation for IntelliSense
  - Deterministic builds
  - SourceLink support for debugging

### License
- **MIT License** - Free and open-source forever
  - No commercial restrictions
  - Open source and community-friendly

---

## Release Notes

### About Kommand

Kommand is a production-ready CQRS/Mediator library originally developed for internal use at Atherio. After using it successfully in production, we decided to open source it to benefit the .NET community.

**Key Design Principles:**
- Explicit CQRS semantics (ICommand vs IQuery) for clearer intent
- Built-in OpenTelemetry integration with zero configuration
- Custom validation system with zero external dependencies
- Scoped lifetime by default (better for DbContext and transaction management)
- Single package with comprehensive features

**Maintenance:** Kommand is primarily maintained to serve Atherio's internal requirements. Development priorities are driven by our internal needs, though we welcome community contributions.

### Getting Started

```bash
dotnet add package Kommand
```

```csharp
// 1. Define a command
public record CreateUserCommand(string Email, string Name) : ICommand<User>;

// 2. Define a handler
public class CreateUserCommandHandler : ICommandHandler<CreateUserCommand, User>
{
    public async Task<User> HandleAsync(CreateUserCommand command, CancellationToken ct)
    {
        return new User { Email = command.Email, Name = command.Name };
    }
}

// 3. Register in DI (Program.cs)
builder.Services.AddKommand(config =>
{
    config.RegisterHandlersFromAssembly(typeof(Program).Assembly);
});

// 4. Use in your code
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;

    public UsersController(IMediator mediator) => _mediator = mediator;

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request)
    {
        var user = await _mediator.SendAsync(
            new CreateUserCommand(request.Email, request.Name));
        return Ok(user);
    }
}
```

That's it! Handlers, validators, and OTEL support are automatically configured.

---

## Future Releases

### [1.1.0] - Planned
- Assembly scanning optimization with caching

### [2.0.0] - Planned
- Result<T> pattern for error handling
- Streaming query support (`IAsyncEnumerable<T>`)
- Enhanced performance with source generators

---

[Unreleased]: https://github.com/atherio-org/Kommand/compare/v1.0.0...HEAD
[1.0.0]: https://github.com/atherio-org/Kommand/releases/tag/v1.0.0
