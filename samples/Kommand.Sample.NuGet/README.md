# Kommand Comprehensive Sample (NuGet Package)

This sample demonstrates **all major features** of the Kommand CQRS/Mediator library in a single console application.

**Note:** This sample uses the **Kommand NuGet package** (version 1.0.0-alpha.1) rather than a project reference. This demonstrates how to consume Kommand as an end user would.

## What This Sample Demonstrates

### ✅ Core Features
- **Commands with Results** - `CreateUserCommand` returns a `User` object
- **Void Commands (Unit)** - `UpdateUserCommand` returns `Unit` (void pattern)
- **Queries (Single Object)** - `GetUserQuery` returns a single `User?`
- **Queries (Collections)** - `ListUsersQuery` returns `List<User>`

### ✅ Validation System
- **Async Validation** - `CreateUserCommandValidator` performs async database checks
- **Error Collection** - Collects ALL validation errors before throwing (not fail-fast)
- **Property-Level Errors** - Each error has a `PropertyName` and `ErrorMessage`
- **Auto-Discovery** - Validators are automatically found and registered

### ✅ Notifications (Pub/Sub)
- **Domain Events** - `UserCreatedNotification` published after user creation
- **Multiple Handlers** - Both `UserCreatedEmailNotificationHandler` and `UserCreatedAuditHandler` execute
- **Fire-and-Forget** - Notifications don't fail if no handlers exist

### ✅ Interceptors
- **Custom Interceptor** - `LoggingInterceptor` logs all requests with timing
- **OpenTelemetry** - Built-in `ActivityInterceptor` and `MetricsInterceptor` for distributed tracing
- **Pipeline Order** - Interceptors execute in reverse registration order

### ✅ Dependency Injection
- **Auto-Discovery** - Handlers, validators, and notification handlers automatically registered
- **Scoped Lifetime** - Supports DbContext injection (demonstrated with `IUserRepository`)
- **Constructor Injection** - All dependencies injected via DI

## Project Structure

```
Kommand.Sample/
├── Commands/
│   ├── CreateUserCommand.cs          # Command with result
│   └── UpdateUserCommand.cs          # Void command (Unit)
├── Queries/
│   ├── GetUserQuery.cs               # Query returning single object
│   └── ListUsersQuery.cs             # Query returning collection
├── Handlers/
│   ├── CreateUserCommandHandler.cs
│   ├── UpdateUserCommandHandler.cs
│   ├── GetUserQueryHandler.cs
│   ├── ListUsersQueryHandler.cs
│   ├── UserCreatedEmailNotificationHandler.cs
│   └── UserCreatedAuditHandler.cs
├── Validators/
│   └── CreateUserCommandValidator.cs # Async validation with DB check
├── Notifications/
│   └── UserCreatedNotification.cs    # Domain event
├── Interceptors/
│   └── LoggingInterceptor.cs         # Custom logging interceptor
├── Infrastructure/
│   ├── IUserRepository.cs            # Repository interface
│   └── InMemoryUserRepository.cs     # In-memory implementation for demo
├── Models/
│   └── User.cs                       # Domain model
└── Program.cs                         # Main application demonstrating all features
```

## How to Run

### Prerequisites
- .NET 10.0 SDK or later
- Terminal/Command Prompt
- Internet connection (to restore the Kommand NuGet package)

### Running the Sample

```bash
# Navigate to the sample directory
cd samples/Kommand.Sample.NuGet

# Restore NuGet packages
dotnet restore

# Run the application
dotnet run
```

### Expected Output

The application will display:

1. **Setup Confirmation** - Kommand configuration summary
2. **Feature 1** - Create a user successfully
   - Shows validation passing
   - Shows notification handlers executing (Email + Audit)
3. **Feature 2** - Validation with async DB check
   - Attempts to create duplicate email
   - Shows validation error for duplicate email
4. **Feature 3** - Validation error collection
   - Creates user with multiple errors
   - Shows ALL errors collected (not fail-fast)
5. **Feature 4** - Query single object
   - Retrieves user by ID
6. **Feature 5** - Query collection
   - Lists all users
7. **Feature 6** - Void command
   - Updates user name (returns Unit)

### Sample Output

```
╔═══════════════════════════════════════════════════════════╗
║   Kommand Comprehensive Sample                            ║
║   Demonstrating all features of Kommand library          ║
╚═══════════════════════════════════════════════════════════╝

Setting up Kommand with all features...

✓ Kommand configured with:
  • Command/Query handlers (auto-discovered)
  • Validators (auto-discovered)
  • Notification handlers (auto-discovered)
  • Custom logging interceptor
  • Built-in OpenTelemetry interceptors
  • Validation interceptor

─────────────────────────────────────────────────────
FEATURE 1: Command with Result
─────────────────────────────────────────────────────
Creating a new user...
info: Executing Command: CreateUserCommand
  [Email Handler] Welcome email sent to alice@example.com
  [Audit Handler] Audit log created for user <guid>
info: Command CreateUserCommand completed successfully in 45ms
✓ User created successfully:
  • ID: <guid>
  • Email: alice@example.com
  • Name: Alice Johnson
  • CreatedAt: 2025-01-21T10:30:00.0000000Z

Notice: Two notification handlers executed (email + audit)
...
```

## Key Code Patterns

### Registering Kommand

```csharp
builder.Services.AddKommand(config =>
{
    // Auto-discover all handlers and validators
    config.RegisterHandlersFromAssembly(typeof(Program).Assembly);

    // Add custom interceptor
    config.AddInterceptor<LoggingInterceptor<,>>();

    // Enable validation
    config.WithValidation();
});
```

### Creating a Command Handler

```csharp
public class CreateUserCommandHandler : ICommandHandler<CreateUserCommand, User>
{
    private readonly IUserRepository _repository;
    private readonly IMediator _mediator;

    public CreateUserCommandHandler(IUserRepository repository, IMediator mediator)
    {
        _repository = repository;
        _mediator = mediator;
    }

    public async Task<User> HandleAsync(CreateUserCommand command, CancellationToken ct)
    {
        var user = new User { /* ... */ };
        await _repository.AddAsync(user, ct);

        // Publish notification
        await _mediator.PublishAsync(new UserCreatedNotification(...), ct);

        return user;
    }
}
```

### Creating a Validator with Async DB Check

```csharp
public class CreateUserCommandValidator : IValidator<CreateUserCommand>
{
    private readonly IUserRepository _repository;

    public async Task<ValidationResult> ValidateAsync(
        CreateUserCommand request,
        CancellationToken ct = default)
    {
        var errors = new List<ValidationError>();

        // Async database check
        if (await _repository.EmailExistsAsync(request.Email, ct))
        {
            errors.Add(new ValidationError("Email", "Email already exists"));
        }

        return new ValidationResult(errors);
    }
}
```

### Handling Validation Errors

```csharp
try
{
    var user = await mediator.SendAsync(new CreateUserCommand(...));
}
catch (ValidationException ex)
{
    // All validation errors collected here
    foreach (var error in ex.Errors)
    {
        Console.WriteLine($"{error.PropertyName}: {error.ErrorMessage}");
    }
}
```

## Real-World Application

In a real application, you would:

1. **Replace InMemoryUserRepository** with Entity Framework `DbContext`
   ```csharp
   public class ApplicationDbContext : DbContext
   {
       public DbSet<User> Users { get; set; }
   }
   ```

2. **Use ASP.NET Core** for HTTP endpoints
   ```csharp
   [ApiController]
   [Route("api/users")]
   public class UsersController : ControllerBase
   {
       private readonly IMediator _mediator;

       [HttpPost]
       public async Task<IActionResult> Create([FromBody] CreateUserRequest request)
       {
           try
           {
               var user = await _mediator.SendAsync(
                   new CreateUserCommand(request.Email, request.Name));
               return Created($"/api/users/{user.Id}", user);
           }
           catch (ValidationException ex)
           {
               return BadRequest(ex.Errors);
           }
       }
   }
   ```

3. **Configure Production OpenTelemetry** exporters

   **Note:** This sample uses `AddConsoleExporter()` for demonstration purposes only. In production, use exporters appropriate for your infrastructure:

   ```csharp
   // Production: Jaeger (APM)
   .WithTracing(tracing => tracing
       .AddSource("Kommand")
       .AddJaegerExporter())

   // Production: Application Insights (Azure)
   .WithTracing(tracing => tracing
       .AddSource("Kommand")
       .AddAzureMonitorTraceExporter())

   // Production: OTLP (Grafana, Datadog, New Relic, etc.)
   .WithTracing(tracing => tracing
       .AddSource("Kommand")
       .AddOtlpExporter())

   // Production: Prometheus (metrics)
   .WithMetrics(metrics => metrics
       .AddMeter("Kommand")
       .AddPrometheusExporter())
   ```

## Learning Resources

- **Main Repository**: https://github.com/atherio-org/Kommand
- **Architecture Documentation**: See `docs/ARCHITECTURE.md` in the repository
- **CQRS Pattern**: https://martinfowler.com/bliki/CQRS.html
- **Mediator Pattern**: https://refactoring.guru/design-patterns/mediator

## Notes

- All handlers use **Scoped** lifetime (not Transient) to support DbContext injection
- Validators can inject repositories and perform async database checks
- Notifications execute all handlers sequentially, swallowing exceptions to ensure resilience
- OpenTelemetry is optional - remove it if you don't need observability
- The sample uses console app for simplicity, but Kommand is designed for ASP.NET Core

## License

This sample is part of the Kommand library, licensed under MIT.
