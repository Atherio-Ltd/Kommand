# Kommand

A lightweight, production-ready CQRS mediator for .NET 8+ with built-in OpenTelemetry support.

![.NET](https://img.shields.io/badge/.NET-8.0%20LTS-512BD4)
![Forward Compatible](https://img.shields.io/badge/compatible-.NET%208%2C%209%2C%2010%2B-success)
![License](https://img.shields.io/badge/license-MIT-blue)
![Build](https://img.shields.io/github/actions/workflow/status/Atherio-Ltd/Kommand/ci.yml?branch=main)
![NuGet](https://img.shields.io/nuget/v/Kommand)
![Coverage](https://img.shields.io/codecov/c/github/Atherio-Ltd/Kommand)

## About Kommand

### Origin

Kommand was originally developed for **internal use at Atherio** as part of our system architecture. We built it to meet our specific needs for a clean, performant CQRS implementation with built-in observability and validation.

After using it successfully in production, we decided to open source Kommand to benefit the broader .NET community. We believe in giving back to the ecosystem that has given us so much.

### Maintenance & Support

**Please note:** Kommand is primarily maintained to serve Atherio's internal requirements. While we're committed to keeping the library open source and available to everyone, our development priorities are driven by our internal needs.

This means:
- ✅ **We will maintain and improve Kommand** as we use it in production
- ✅ **Bug fixes and updates** will continue as we encounter and resolve issues
- ✅ **The library will remain free and open source forever** (MIT License)
- ⚠️ **Feature requests may not be prioritized** unless they align with our internal roadmap
- ⚠️ **We don't make commitments on timelines** for external feature requests
- ⚠️ **Support is provided on a best-effort basis**

We welcome community contributions! If you need a feature that we haven't prioritized, we encourage you to submit a pull request.

### Why We Built It

We needed a CQRS mediator library that offered:

1. **Zero External Dependencies** - Only Microsoft's DI abstractions, no third-party packages
2. **Built-in Observability** - OpenTelemetry integration out of the box for production monitoring
3. **Production Performance** - Sub-microsecond overhead that doesn't impact real workloads
4. **Scoped Handlers by Default** - Better integration with EF Core and database contexts
5. **Explicit CQRS** - Clear separation between commands and queries
6. **Custom Validation** - Async validation with database access without external dependencies
7. **MIT License** - Truly free and open source, forever

## Features

- ✅ **CQRS**: Explicit command and query separation with `ICommand<TResponse>` and `IQuery<TResponse>`
- ✅ **Zero Dependencies**: Only `Microsoft.Extensions.DependencyInjection.Abstractions` and `System.Diagnostics.DiagnosticSource`
- ✅ **Auto-Discovery**: Handlers and validators automatically registered from assemblies
- ✅ **Interceptors**: Cross-cutting concerns (validation, logging, metrics) with reverse-order execution
- ✅ **OpenTelemetry**: Zero-config distributed tracing and metrics with ~10-50ns overhead when not configured
- ✅ **Pub/Sub**: Domain events with `INotification` and multiple handlers
- ✅ **Custom Validation**: Built-in async validation system with auto-discovery (no FluentValidation required)
- ✅ **Scoped by Default**: Handlers use Scoped lifetime to support DbContext injection
- ✅ **MIT License**: Fully open source and free forever

## Quick Start

### Installation

```bash
dotnet add package Kommand --version 1.0.0-alpha.1
```

> **Note:** Kommand is currently in pre-release. If you're using an IDE's NuGet package manager (Visual Studio, Rider, VS Code), make sure to enable the "Include prerelease" option when searching for the package.

### Basic Usage

**1. Define a command:**

```csharp
using Kommand.Abstractions;

public record CreateUserCommand(string Email, string Name) : ICommand<User>;
```

**2. Create a handler:**

```csharp
public class CreateUserCommandHandler : ICommandHandler<CreateUserCommand, User>
{
    private readonly IUserRepository _repository;

    public CreateUserCommandHandler(IUserRepository repository)
    {
        _repository = repository;
    }

    public async Task<User> HandleAsync(CreateUserCommand command, CancellationToken ct)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = command.Email,
            Name = command.Name
        };

        await _repository.AddAsync(user, ct);
        return user;
    }
}
```

**3. Register with DI (Program.cs):**

```csharp
builder.Services.AddKommand(config =>
{
    config.RegisterHandlersFromAssembly(typeof(Program).Assembly);
    config.WithValidation(); // Optional: Enable validation
});
```

**4. Use in your application:**

```csharp
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;

    public UsersController(IMediator mediator) => _mediator = mediator;

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request)
    {
        var command = new CreateUserCommand(request.Email, request.Name);
        var user = await _mediator.SendAsync(command, HttpContext.RequestAborted);
        return Created($"/users/{user.Id}", user);
    }
}
```

That's it! Handlers, validators, and interceptors are automatically discovered. OpenTelemetry traces and metrics are automatically included.

## Advanced Features

### Validation

Define validators that run before handlers:

```csharp
public class CreateUserCommandValidator : IValidator<CreateUserCommand>
{
    private readonly IUserRepository _repository;

    public CreateUserCommandValidator(IUserRepository repository)
    {
        _repository = repository;
    }

    public async Task<ValidationResult> ValidateAsync(
        CreateUserCommand request,
        CancellationToken ct)
    {
        var errors = new List<ValidationError>();

        if (string.IsNullOrWhiteSpace(request.Email))
            errors.Add(new ValidationError(nameof(request.Email), "Email is required"));

        // Async database check
        if (await _repository.EmailExistsAsync(request.Email, ct))
            errors.Add(new ValidationError(nameof(request.Email), "Email already exists"));

        return errors.Count > 0
            ? ValidationResult.Failure(errors)
            : ValidationResult.Success();
    }
}
```

Enable validation in your configuration:

```csharp
builder.Services.AddKommand(config =>
{
    config.RegisterHandlersFromAssembly(typeof(Program).Assembly);
    config.WithValidation(); // Validators auto-discovered and executed
});
```

### Custom Interceptors

Create interceptors for cross-cutting concerns:

```csharp
public class LoggingInterceptor<TRequest, TResponse> : IInterceptor<TRequest, TResponse>
{
    private readonly ILogger<LoggingInterceptor<TRequest, TResponse>> _logger;

    public LoggingInterceptor(ILogger<LoggingInterceptor<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> InterceptAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Executing {RequestType}", typeof(TRequest).Name);
        var response = await next();
        _logger.LogInformation("Completed {RequestType}", typeof(TRequest).Name);
        return response;
    }
}
```

Register interceptors:

```csharp
config.AddInterceptor(typeof(LoggingInterceptor<,>));
```

### OpenTelemetry Integration

Kommand includes built-in OpenTelemetry support with **zero configuration required**. The library automatically creates traces and metrics - you just need to configure your preferred exporter.

**Optional:** Configure OpenTelemetry in your application (choose your exporter):

```csharp
// Example 1: Console (for development/debugging)
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("MyApp"))
    .WithTracing(tracing => tracing
        .AddSource("Kommand")
        .AddConsoleExporter())
    .WithMetrics(metrics => metrics
        .AddMeter("Kommand")
        .AddConsoleExporter());

// Example 2: Jaeger (production APM)
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddSource("Kommand")
        .AddJaegerExporter());

// Example 3: Application Insights (Azure)
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddSource("Kommand")
        .AddAzureMonitorTraceExporter(options =>
        {
            options.ConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
        }));

// Example 4: OTLP (OpenTelemetry Protocol - works with many backends)
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddSource("Kommand")
        .AddOtlpExporter());
```

**What Kommand exports:**
- **Traces**: Activity spans for each command/query with detailed tags
- **Metrics**: Request counts, durations, and validation failures

**When OTEL is not configured:** ~10-50ns overhead per request (negligible).

### Domain Events (Pub/Sub)

Publish notifications to multiple handlers:

```csharp
// Define notification
public record UserCreatedNotification(Guid UserId, string Email) : INotification;

// Create handlers (multiple handlers can subscribe)
public class SendWelcomeEmailHandler : INotificationHandler<UserCreatedNotification>
{
    public async Task HandleAsync(UserCreatedNotification notification, CancellationToken ct)
    {
        // Send welcome email
    }
}

public class AuditLogHandler : INotificationHandler<UserCreatedNotification>
{
    public async Task HandleAsync(UserCreatedNotification notification, CancellationToken ct)
    {
        // Log to audit trail
    }
}

// Publish from command handler
await _mediator.PublishAsync(
    new UserCreatedNotification(user.Id, user.Email),
    cancellationToken);
```

## Performance

Kommand is designed for production use with minimal overhead:

| Metric | Result |
|--------|--------|
| Mediator dispatch overhead | 685 ns (0.685 μs) |
| Per-interceptor cost | 74 ns |
| Total with 3 interceptors | 915 ns (0.915 μs) |
| 1ms DB operation overhead | ~0.07% |
| 10ms API call overhead | ~0.009% |
| OTEL when not configured | ~10-50ns |

See [benchmarks](tests/Kommand.Benchmarks/) for detailed performance analysis.

## Documentation

- **[Getting Started Guide](docs/getting-started.md)** - Step-by-step tutorial
- **[Architecture Document](docs/ARCHITECTURE.md)** - Complete design specification
- **[Sample Project](samples/Kommand.Sample/)** - Working example demonstrating all features
- **[CHANGELOG](CHANGELOG.md)** - Version history and release notes

## Requirements

- **.NET 8.0 or later** (including .NET 9, 10+)
- **Package Size**: <50KB
- **Dependencies**:
  - `Microsoft.Extensions.DependencyInjection.Abstractions` (8.0.0)
  - `System.Diagnostics.DiagnosticSource` (8.0.0)

## Contributing

Contributions are welcome! Please see [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

- **Report bugs**: [GitHub Issues](https://github.com/Atherio-Ltd/Kommand/issues)
- **Suggest features**: [GitHub Discussions](https://github.com/Atherio-Ltd/Kommand/discussions)
- **Submit PRs**: Follow the [contribution guidelines](CONTRIBUTING.md)

## License

Kommand is licensed under the [MIT License](LICENSE). It is and will always be free and open source.

---

**Built with ❤️ by Atherio for the .NET community**
