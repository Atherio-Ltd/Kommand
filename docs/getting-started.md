# Getting Started with Kommand

This guide will walk you through setting up Kommand and building your first command-handler application.

## Prerequisites

- .NET 8.0 SDK or later (works with .NET 8, 9, 10+)
- Basic understanding of C# and dependency injection

## Installation

Add the Kommand package to your project:

```bash
dotnet add package Kommand
```

That's it! Kommand has zero external dependencies beyond the standard Microsoft DI abstractions.

## Your First Command

Let's build a simple user creation feature using Kommand.

### Step 1: Define Your Command

Commands represent **write operations** that change state. Create a command using a record:

```csharp
using Kommand.Abstractions;

namespace MyApp.Commands;

public record CreateUserCommand(string Email, string Name) : ICommand<User>;
```

**Key points:**
- Commands inherit from `ICommand<TResponse>` where `TResponse` is the return type
- Use `ICommand<Unit>` for void commands (commands that don't return a value)
- Records are recommended for immutability and simplicity

### Step 2: Create Your Handler

Handlers contain the business logic for executing commands:

```csharp
using Kommand.Abstractions;

namespace MyApp.Handlers;

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

    public async Task<User> HandleAsync(
        CreateUserCommand command,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating user with email: {Email}", command.Email);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = command.Email,
            Name = command.Name,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(user, cancellationToken);

        _logger.LogInformation("User created successfully: {UserId}", user.Id);

        return user;
    }
}
```

**Key points:**
- Handlers can inject any scoped dependencies (DbContext, repositories, etc.)
- All handlers are registered as **Scoped** by default (ideal for use with DbContext)
- Always use `CancellationToken` for async operations

### Step 3: Register Kommand with Dependency Injection

In your `Program.cs`:

```csharp
using Kommand;

var builder = WebApplication.CreateBuilder(args);

// Register your application services
builder.Services.AddScoped<IUserRepository, UserRepository>();

// Register Kommand and auto-discover handlers
builder.Services.AddKommand(config =>
{
    // Scan assembly for handlers, validators, and notification handlers
    config.RegisterHandlersFromAssembly(typeof(Program).Assembly);
});

var app = builder.Build();
```

**What happens here:**
- `RegisterHandlersFromAssembly()` scans your assembly for all handlers
- Handlers, validators, and notification handlers are automatically registered
- Everything is registered with the correct lifetime (Scoped)

### Step 4: Use the Mediator

Inject `IMediator` and send commands:

```csharp
using Kommand.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace MyApp.Controllers;

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
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        var command = new CreateUserCommand(request.Email, request.Name);

        var user = await _mediator.SendAsync(command, HttpContext.RequestAborted);

        return CreatedAtAction(
            nameof(GetUser),
            new { id = user.Id },
            user);
    }
}
```

**Key points:**
- Inject `IMediator` into any service, controller, or handler
- Use `SendAsync()` for commands that return a value
- Pass `CancellationToken` from the HTTP context for proper cancellation support

That's it! You've built your first feature with Kommand.

## Working with Queries

Queries represent **read operations** that don't change state.

### Define a Query

```csharp
using Kommand.Abstractions;

namespace MyApp.Queries;

public record GetUserQuery(Guid UserId) : IQuery<User?>;
```

### Create a Query Handler

```csharp
using Kommand.Abstractions;

namespace MyApp.Handlers;

public class GetUserQueryHandler : IQueryHandler<GetUserQuery, User?>
{
    private readonly IUserRepository _repository;

    public GetUserQueryHandler(IUserRepository repository)
    {
        _repository = repository;
    }

    public async Task<User?> HandleAsync(
        GetUserQuery query,
        CancellationToken cancellationToken)
    {
        return await _repository.GetByIdAsync(query.UserId, cancellationToken);
    }
}
```

### Execute the Query

```csharp
[HttpGet("{id}")]
public async Task<IActionResult> GetUser(Guid id)
{
    var query = new GetUserQuery(id);
    var user = await _mediator.QueryAsync(query, HttpContext.RequestAborted);

    return user is not null ? Ok(user) : NotFound();
}
```

**Key points:**
- Use `IQuery<TResponse>` for read operations
- Use `QueryAsync()` to execute queries
- Queries should not modify state

## Adding Validation

Kommand includes a built-in validation system with async support.

### Step 1: Create a Validator

```csharp
using Kommand.Validation;

namespace MyApp.Validators;

public class CreateUserCommandValidator : IValidator<CreateUserCommand>
{
    private readonly IUserRepository _repository;

    public CreateUserCommandValidator(IUserRepository repository)
    {
        _repository = repository;
    }

    public async Task<ValidationResult> ValidateAsync(
        CreateUserCommand request,
        CancellationToken cancellationToken)
    {
        var errors = new List<ValidationError>();

        // Basic validation
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            errors.Add(new ValidationError(
                nameof(request.Email),
                "Email is required"));
        }
        else if (!IsValidEmail(request.Email))
        {
            errors.Add(new ValidationError(
                nameof(request.Email),
                "Email format is invalid"));
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            errors.Add(new ValidationError(
                nameof(request.Name),
                "Name is required"));
        }
        else if (request.Name.Length < 2)
        {
            errors.Add(new ValidationError(
                nameof(request.Name),
                "Name must be at least 2 characters"));
        }

        // Async database check
        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            var emailExists = await _repository.EmailExistsAsync(
                request.Email,
                cancellationToken);

            if (emailExists)
            {
                errors.Add(new ValidationError(
                    nameof(request.Email),
                    "Email already exists"));
            }
        }

        return errors.Count > 0
            ? ValidationResult.Failure(errors)
            : ValidationResult.Success();
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}
```

### Step 2: Enable Validation

In your `Program.cs`:

```csharp
builder.Services.AddKommand(config =>
{
    config.RegisterHandlersFromAssembly(typeof(Program).Assembly);
    config.WithValidation(); // Enable validation interceptor
});
```

**What happens:**
- All validators are automatically discovered and registered
- Validators run **before** the handler
- If validation fails, a `ValidationException` is thrown
- All validation errors are collected (not fail-fast)

### Step 3: Handle Validation Errors

```csharp
using Kommand.Validation;

[HttpPost]
public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
{
    try
    {
        var command = new CreateUserCommand(request.Email, request.Name);
        var user = await _mediator.SendAsync(command, HttpContext.RequestAborted);

        return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
    }
    catch (ValidationException ex)
    {
        var errors = ex.Errors.ToDictionary(
            e => e.PropertyName,
            e => e.ErrorMessage);

        return BadRequest(new { errors });
    }
}
```

## Adding Custom Interceptors

Interceptors allow you to add cross-cutting concerns like logging, metrics, or caching.

### Create an Interceptor

```csharp
using Kommand.Interceptors;
using Microsoft.Extensions.Logging;

namespace MyApp.Interceptors;

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
        var requestType = typeof(TRequest).Name;

        _logger.LogInformation("Executing {RequestType}", requestType);

        var sw = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            var response = await next();

            sw.Stop();

            _logger.LogInformation(
                "Completed {RequestType} in {ElapsedMs}ms",
                requestType,
                sw.ElapsedMilliseconds);

            return response;
        }
        catch (Exception ex)
        {
            sw.Stop();

            _logger.LogError(
                ex,
                "Failed {RequestType} after {ElapsedMs}ms",
                requestType,
                sw.ElapsedMilliseconds);

            throw;
        }
    }
}
```

### Register the Interceptor

```csharp
builder.Services.AddKommand(config =>
{
    config.RegisterHandlersFromAssembly(typeof(Program).Assembly);
    config.AddInterceptor(typeof(LoggingInterceptor<,>));
    config.WithValidation();
});
```

**Interceptor execution order:**
- Interceptors execute in **reverse registration order**
- First registered = outermost (executes first on entry, last on exit)
- Built-in interceptors (Activity, Metrics, Validation) are always innermost

## Configuring OpenTelemetry (Optional)

Kommand includes **built-in OpenTelemetry support** that works automatically - you just need to configure your preferred exporter.

**Important:** OpenTelemetry configuration is **completely optional**. If you don't configure it, Kommand still works perfectly with only ~10-50ns overhead per request.

### Choose Your Exporter

OpenTelemetry supports many exporters. Choose based on your infrastructure:

#### Option 1: Console (Development/Debugging)

```bash
dotnet add package OpenTelemetry.Extensions.Hosting
dotnet add package OpenTelemetry.Exporter.Console
```

```csharp
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("MyApp"))
    .WithTracing(tracing => tracing
        .AddSource("Kommand")
        .AddConsoleExporter())
    .WithMetrics(metrics => metrics
        .AddMeter("Kommand")
        .AddConsoleExporter());
```

#### Option 2: Jaeger (Production APM)

```bash
dotnet add package OpenTelemetry.Extensions.Hosting
dotnet add package OpenTelemetry.Exporter.Jaeger
```

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddSource("Kommand")
        .AddJaegerExporter(options =>
        {
            options.AgentHost = "localhost";
            options.AgentPort = 6831;
        }));
```

#### Option 3: Application Insights (Azure)

```bash
dotnet add package OpenTelemetry.Extensions.Hosting
dotnet add package Azure.Monitor.OpenTelemetry.Exporter
```

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddSource("Kommand")
        .AddAzureMonitorTraceExporter(options =>
        {
            options.ConnectionString = configuration["ApplicationInsights:ConnectionString"];
        }));
```

#### Option 4: OTLP (OpenTelemetry Protocol)

Works with many backends (Grafana, Datadog, New Relic, etc.)

```bash
dotnet add package OpenTelemetry.Extensions.Hosting
dotnet add package OpenTelemetry.Exporter.OpenTelemetryProtocol
```

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddSource("Kommand")
        .AddOtlpExporter(options =>
        {
            options.Endpoint = new Uri("http://localhost:4317");
        }));
```

#### Option 5: Zipkin

```bash
dotnet add package OpenTelemetry.Extensions.Hosting
dotnet add package OpenTelemetry.Exporter.Zipkin
```

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddSource("Kommand")
        .AddZipkinExporter(options =>
        {
            options.Endpoint = new Uri("http://localhost:9411/api/v2/spans");
        }));
```

### What Kommand Exports

**When you configure any exporter above**, Kommand automatically exports:

- **Traces**: Activity spans for each command/query with detailed tags
- **Metrics**: Request counts, durations, validation failures

**When you don't configure OpenTelemetry:** Minimal overhead (~10-50ns per request)

### Exported Data

**Traces include:**
- `kommand.request.type`: "Command" or "Query"
- `kommand.request.name`: Full type name
- `kommand.handler.name`: Handler type name
- `kommand.validation.enabled`: "true" or "false"
- `kommand.validation.failed`: "true" if validation failed

**Metrics include:**
- `kommand.requests`: Total request count (counter)
- `kommand.request.duration`: Request duration in milliseconds (histogram)
- `kommand.validation.failures`: Validation failure count (counter)

## Working with Notifications (Domain Events)

Notifications allow you to publish events to multiple handlers.

### Define a Notification

```csharp
using Kommand.Abstractions;

namespace MyApp.Notifications;

public record UserCreatedNotification(
    Guid UserId,
    string Email,
    string Name) : INotification;
```

### Create Notification Handlers

```csharp
// Send welcome email
public class SendWelcomeEmailHandler : INotificationHandler<UserCreatedNotification>
{
    private readonly IEmailService _emailService;

    public SendWelcomeEmailHandler(IEmailService emailService)
    {
        _emailService = emailService;
    }

    public async Task HandleAsync(
        UserCreatedNotification notification,
        CancellationToken cancellationToken)
    {
        await _emailService.SendWelcomeEmailAsync(
            notification.Email,
            notification.Name,
            cancellationToken);
    }
}

// Log to audit trail
public class AuditUserCreationHandler : INotificationHandler<UserCreatedNotification>
{
    private readonly IAuditLog _auditLog;

    public AuditUserCreationHandler(IAuditLog auditLog)
    {
        _auditLog = auditLog;
    }

    public async Task HandleAsync(
        UserCreatedNotification notification,
        CancellationToken cancellationToken)
    {
        await _auditLog.LogAsync(
            $"User created: {notification.UserId}",
            cancellationToken);
    }
}
```

### Publish the Notification

```csharp
public async Task<User> HandleAsync(
    CreateUserCommand command,
    CancellationToken cancellationToken)
{
    var user = new User { /* ... */ };
    await _repository.AddAsync(user, cancellationToken);

    // Publish to all handlers
    await _mediator.PublishAsync(
        new UserCreatedNotification(user.Id, user.Email, user.Name),
        cancellationToken);

    return user;
}
```

**Key points:**
- Multiple handlers can subscribe to the same notification
- Handlers execute sequentially
- If one handler fails, others still execute (continue-on-failure)
- Exceptions are logged but not propagated

## Troubleshooting

### Handler Not Found Exception

**Problem:** `HandlerNotFoundException: No handler registered for command 'MyCommand'`

**Solutions:**
1. Ensure you called `config.RegisterHandlersFromAssembly(assembly)`
2. Verify your handler implements the correct interface (`ICommandHandler<,>` or `IQueryHandler<,>`)
3. Check that the handler class is public and not abstract
4. Ensure the assembly containing the handler is passed to `RegisterHandlersFromAssembly()`

### Validator Not Executing

**Problem:** Validators are defined but not running

**Solutions:**
1. Ensure you called `config.WithValidation()` in your Kommand configuration
2. Verify the validator implements `IValidator<TRequest>`
3. Check that the validator is in the assembly passed to `RegisterHandlersFromAssembly()`

### DbContext Disposed Exception

**Problem:** `ObjectDisposedException` when accessing DbContext in handler

**Solutions:**
1. Verify your handler is registered as Scoped (Kommand does this by default)
2. If manually registering handlers, use `AddScoped()` not `AddTransient()`
3. Ensure DbContext is registered as Scoped in DI

### Interceptor Not Executing

**Problem:** Custom interceptor doesn't run

**Solutions:**
1. Verify you called `config.AddInterceptor(typeof(MyInterceptor<,>))`
2. Check that the interceptor implements `IInterceptor<TRequest, TResponse>`
3. Ensure the interceptor is registered as an open generic type: `typeof(MyInterceptor<,>)`

### Validation Errors Not Detailed

**Problem:** `ValidationException` doesn't contain detailed errors

**Solutions:**
1. Ensure your validator returns `ValidationResult.Failure(errors)` not `ValidationResult.Success()`
2. Check that you're creating `ValidationError` objects with property names and messages
3. Verify you're collecting all errors before returning the result

## Next Steps

- **Explore the [Sample Project](../samples/Kommand.Sample/)** - See all features in action
- **Read the [Architecture Document](../MEDIATOR_ARCHITECTURE_PLAN.md)** - Understand design decisions
- **Review [Performance Benchmarks](../tests/Kommand.Benchmarks/)** - See performance characteristics

## Need Help?

- **Report Issues**: [GitHub Issues](https://github.com/Atherio-Ltd/Kommand/issues)
- **Ask Questions**: [GitHub Discussions](https://github.com/Atherio-Ltd/Kommand/discussions)
- **Contribute**: See [CONTRIBUTING.md](../CONTRIBUTING.md)
