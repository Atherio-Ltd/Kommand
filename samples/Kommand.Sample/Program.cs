using Kommand;
using Kommand.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

Console.WriteLine("=== Kommand OpenTelemetry Integration Sample ===\n");

// Create host builder with dependency injection
var builder = Host.CreateApplicationBuilder(args);

// STEP 1: Register Kommand (no OTEL-specific configuration needed!)
builder.Services.AddKommand(config =>
{
    config.RegisterHandlersFromAssembly(typeof(Program).Assembly);
});

// STEP 2: Add OpenTelemetry - automatically discovers Kommand's ActivitySource and Meter
// This is the ONLY configuration needed to enable distributed tracing and metrics!
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(
        serviceName: "Kommand.Sample",
        serviceVersion: "1.0.0"))
    .WithTracing(tracing => tracing
        .AddSource("Kommand") // Subscribe to Kommand's distributed traces
        .AddConsoleExporter() // Export traces to console (in production, use Jaeger, Zipkin, etc.)
    )
    .WithMetrics(metrics => metrics
        .AddMeter("Kommand") // Subscribe to Kommand's metrics
        .AddConsoleExporter() // Export metrics to console (in production, use Prometheus, OTLP, etc.)
    );

// Build the host
var host = builder.Build();

// Get the mediator from DI
var mediator = host.Services.GetRequiredService<IMediator>();

Console.WriteLine("Executing commands with OpenTelemetry enabled...\n");

// STEP 3: Execute various commands - traces and metrics will be automatically created!

// Example 1: Simple command
Console.WriteLine("1. Executing CreateUser command...");
var user = await mediator.SendAsync(
    new CreateUserCommand("alice@example.com", "Alice Johnson"),
    CancellationToken.None);
Console.WriteLine($"   Result: User created with ID {user.Id}\n");

// Example 2: Query
Console.WriteLine("2. Executing GetUser query...");
var queriedUser = await mediator.QueryAsync(
    new GetUserQuery(user.Id),
    CancellationToken.None);
Console.WriteLine($"   Result: Found user {queriedUser?.Name}\n");

// Example 3: Command that will fail (to show error tracing)
Console.WriteLine("3. Executing FailingCommand (will demonstrate error tracing)...");
try
{
    await mediator.SendAsync(new FailingTestCommand(), CancellationToken.None);
}
catch (InvalidOperationException ex)
{
    Console.WriteLine($"   Expected error: {ex.Message}\n");
}

// Example 4: Slow command (to show duration tracking)
Console.WriteLine("4. Executing SlowCommand (will take ~100ms)...");
await mediator.SendAsync(new SlowCommand(), CancellationToken.None);
Console.WriteLine("   Completed!\n");

Console.WriteLine("\n=== Execution Complete ===");
Console.WriteLine("\nNOTE: In the console output above, you should see:");
Console.WriteLine("  - Activity traces showing command execution with timings");
Console.WriteLine("  - Metrics showing request counts and durations");
Console.WriteLine("  - Error status for the FailingCommand");
Console.WriteLine("\nIn production, configure OpenTelemetry exporters for:");
Console.WriteLine("  - Jaeger or Zipkin for distributed tracing");
Console.WriteLine("  - Prometheus or Application Insights for metrics");

// ============================================================================
// Sample Commands, Queries, and Handlers
// ============================================================================

/// <summary>
/// Sample command to create a user.
/// </summary>
public record CreateUserCommand(string Email, string Name) : ICommand<User>;

/// <summary>
/// Handler for CreateUserCommand.
/// </summary>
public class CreateUserCommandHandler : ICommandHandler<CreateUserCommand, User>
{
    public Task<User> HandleAsync(CreateUserCommand command, CancellationToken cancellationToken)
    {
        // Simulate user creation
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = command.Email,
            Name = command.Name,
            CreatedAt = DateTime.UtcNow
        };

        return Task.FromResult(user);
    }
}

/// <summary>
/// Sample query to get a user by ID.
/// </summary>
public record GetUserQuery(Guid Id) : IQuery<User?>;

/// <summary>
/// Handler for GetUserQuery.
/// </summary>
public class GetUserQueryHandler : IQueryHandler<GetUserQuery, User?>
{
    public Task<User?> HandleAsync(GetUserQuery query, CancellationToken cancellationToken)
    {
        // Simulate user retrieval
        // In a real app, this would query a database
        return Task.FromResult<User?>(new User
        {
            Id = query.Id,
            Email = "alice@example.com",
            Name = "Alice Johnson",
            CreatedAt = DateTime.UtcNow
        });
    }
}

/// <summary>
/// Sample command that fails (to demonstrate error tracing).
/// </summary>
public record FailingTestCommand : ICommand<Unit>;

/// <summary>
/// Handler that always fails (to demonstrate error tracing).
/// </summary>
public class FailingTestCommandHandler : ICommandHandler<FailingTestCommand, Unit>
{
    public Task<Unit> HandleAsync(FailingTestCommand command, CancellationToken cancellationToken)
    {
        throw new InvalidOperationException("This command is designed to fail to demonstrate error tracing!");
    }
}

/// <summary>
/// Sample command that takes time to execute (to demonstrate duration tracking).
/// </summary>
public record SlowCommand : ICommand<Unit>;

/// <summary>
/// Handler with artificial delay (to demonstrate duration tracking).
/// </summary>
public class SlowCommandHandler : ICommandHandler<SlowCommand, Unit>
{
    public async Task<Unit> HandleAsync(SlowCommand command, CancellationToken cancellationToken)
    {
        // Simulate slow operation
        await Task.Delay(100, cancellationToken);
        return Unit.Value;
    }
}

/// <summary>
/// Sample user entity.
/// </summary>
public class User
{
    public Guid Id { get; set; }
    public required string Email { get; set; }
    public required string Name { get; set; }
    public DateTime CreatedAt { get; set; }
}
