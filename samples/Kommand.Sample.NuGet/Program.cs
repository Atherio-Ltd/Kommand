using Kommand;
using Kommand.Abstractions;
using Kommand.Sample.Commands;
using Kommand.Sample.Infrastructure;
using Kommand.Sample.Interceptors;
using Kommand.Sample.Queries;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

Console.WriteLine("╔═══════════════════════════════════════════════════════════╗");
Console.WriteLine("║   Kommand Comprehensive Sample                            ║");
Console.WriteLine("║   Demonstrating all features of Kommand library          ║");
Console.WriteLine("╚═══════════════════════════════════════════════════════════╝");
Console.WriteLine();

// ============================================================================
// STEP 1: Setup Dependency Injection with Kommand
// ============================================================================
Console.WriteLine("Setting up Kommand with all features...\n");

var builder = Host.CreateApplicationBuilder(args);

// Register application services (repository)
builder.Services.AddScoped<IUserRepository, InMemoryUserRepository>();

// Configure Kommand with all features
builder.Services.AddKommand(config =>
{
    // Auto-discover handlers and validators from assembly
    config.RegisterHandlersFromAssembly(typeof(Program).Assembly);

    // Add custom interceptor (logged execution)
    config.AddInterceptor(typeof(LoggingInterceptor<,>));

    // Enable validation - validators will be executed automatically
    config.WithValidation();
});

// Optional: Add OpenTelemetry for distributed tracing and metrics
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(
        serviceName: "Kommand.Sample",
        serviceVersion: "1.0.0"))
    .WithTracing(tracing => tracing
        .AddSource("Kommand")
        .AddConsoleExporter())
    .WithMetrics(metrics => metrics
        .AddMeter("Kommand")
        .AddConsoleExporter());

// Build the host
var host = builder.Build();
var mediator = host.Services.GetRequiredService<IMediator>();

Console.WriteLine("✓ Kommand configured with:");
Console.WriteLine("  • Command/Query handlers (auto-discovered)");
Console.WriteLine("  • Validators (auto-discovered)");
Console.WriteLine("  • Notification handlers (auto-discovered)");
Console.WriteLine("  • Custom logging interceptor");
Console.WriteLine("  • Built-in OpenTelemetry interceptors");
Console.WriteLine("  • Validation interceptor");
Console.WriteLine();

// ============================================================================
// STEP 2: Demonstrate Command with Result (CreateUserCommand)
// ============================================================================
Console.WriteLine("─────────────────────────────────────────────────────");
Console.WriteLine("FEATURE 1: Command with Result");
Console.WriteLine("─────────────────────────────────────────────────────");
Console.WriteLine("Creating a new user...");

try
{
    var user = await mediator.SendAsync(
        new CreateUserCommand("alice@example.com", "Alice Johnson"),
        CancellationToken.None);

    Console.WriteLine($"✓ User created successfully:");
    Console.WriteLine($"  • ID: {user.Id}");
    Console.WriteLine($"  • Email: {user.Email}");
    Console.WriteLine($"  • Name: {user.Name}");
    Console.WriteLine($"  • CreatedAt: {user.CreatedAt:O}");
    Console.WriteLine();
    Console.WriteLine("Notice: Two notification handlers executed (email + audit)");
}
catch (ValidationException ex)
{
    Console.WriteLine($"✗ Validation failed: {ex.Message}");
    foreach (var error in ex.Errors)
    {
        Console.WriteLine($"  • {error.PropertyName}: {error.ErrorMessage}");
    }
}
Console.WriteLine();

// ============================================================================
// STEP 3: Demonstrate Validation with Async DB Check
// ============================================================================
Console.WriteLine("─────────────────────────────────────────────────────");
Console.WriteLine("FEATURE 2: Validation with Async Database Check");
Console.WriteLine("─────────────────────────────────────────────────────");
Console.WriteLine("Attempting to create user with duplicate email...");

try
{
    await mediator.SendAsync(
        new CreateUserCommand("alice@example.com", "Alice Smith"),
        CancellationToken.None);

    Console.WriteLine("✗ Should have thrown validation exception!");
}
catch (ValidationException ex)
{
    Console.WriteLine($"✓ Validation correctly prevented duplicate email:");
    foreach (var error in ex.Errors)
    {
        Console.WriteLine($"  • {error.PropertyName}: {error.ErrorMessage}");
    }
}
Console.WriteLine();

// ============================================================================
// STEP 4: Demonstrate Validation Error Collection (Not Fail-Fast)
// ============================================================================
Console.WriteLine("─────────────────────────────────────────────────────");
Console.WriteLine("FEATURE 3: Validation Error Collection");
Console.WriteLine("─────────────────────────────────────────────────────");
Console.WriteLine("Creating user with multiple validation errors...");

try
{
    await mediator.SendAsync(
        new CreateUserCommand("", "A"), // Empty email + too short name
        CancellationToken.None);

    Console.WriteLine("✗ Should have thrown validation exception!");
}
catch (ValidationException ex)
{
    Console.WriteLine($"✓ Collected ALL validation errors (not fail-fast):");
    Console.WriteLine($"  Total errors: {ex.Errors.Count}");
    foreach (var error in ex.Errors)
    {
        Console.WriteLine($"  • {error.PropertyName}: {error.ErrorMessage}");
    }
}
Console.WriteLine();

// ============================================================================
// STEP 5: Demonstrate Query Returning Single Object
// ============================================================================
Console.WriteLine("─────────────────────────────────────────────────────");
Console.WriteLine("FEATURE 4: Query Returning Single Object");
Console.WriteLine("─────────────────────────────────────────────────────");

// First create a user to query
var createdUser = await mediator.SendAsync(
    new CreateUserCommand("bob@example.com", "Bob Smith"),
    CancellationToken.None);

Console.WriteLine($"Querying user by ID: {createdUser.Id}...");

var queriedUser = await mediator.QueryAsync(
    new GetUserQuery(createdUser.Id),
    CancellationToken.None);

if (queriedUser != null)
{
    Console.WriteLine($"✓ Found user: {queriedUser.Name} ({queriedUser.Email})");
}
else
{
    Console.WriteLine("✗ User not found");
}
Console.WriteLine();

// ============================================================================
// STEP 6: Demonstrate Query Returning Collection
// ============================================================================
Console.WriteLine("─────────────────────────────────────────────────────");
Console.WriteLine("FEATURE 5: Query Returning Collection");
Console.WriteLine("─────────────────────────────────────────────────────");
Console.WriteLine("Querying all users...");

var users = await mediator.QueryAsync(
    new ListUsersQuery(),
    CancellationToken.None);

Console.WriteLine($"✓ Found {users.Count} users:");
foreach (var u in users)
{
    Console.WriteLine($"  • {u.Name} ({u.Email})");
}
Console.WriteLine();

// ============================================================================
// STEP 7: Demonstrate Void Command (Unit)
// ============================================================================
Console.WriteLine("─────────────────────────────────────────────────────");
Console.WriteLine("FEATURE 6: Void Command (Unit Result)");
Console.WriteLine("─────────────────────────────────────────────────────");
Console.WriteLine($"Updating user {createdUser.Id} name...");

await mediator.SendAsync(
    new UpdateUserCommand(createdUser.Id, "Bob Johnson Jr."),
    CancellationToken.None);

Console.WriteLine("✓ User updated successfully");

// Verify update
var updatedUser = await mediator.QueryAsync(
    new GetUserQuery(createdUser.Id),
    CancellationToken.None);

Console.WriteLine($"  New name: {updatedUser?.Name}");
Console.WriteLine($"  UpdatedAt: {updatedUser?.UpdatedAt:O}");
Console.WriteLine();

// ============================================================================
// STEP 8: Summary
// ============================================================================
Console.WriteLine("═════════════════════════════════════════════════════");
Console.WriteLine("SUMMARY: All Kommand Features Demonstrated");
Console.WriteLine("═════════════════════════════════════════════════════");
Console.WriteLine("✓ Commands with results (CreateUserCommand)");
Console.WriteLine("✓ Void commands with Unit (UpdateUserCommand)");
Console.WriteLine("✓ Queries returning single objects (GetUserQuery)");
Console.WriteLine("✓ Queries returning collections (ListUsersQuery)");
Console.WriteLine("✓ Async validation with database checks");
Console.WriteLine("✓ Validation error collection (not fail-fast)");
Console.WriteLine("✓ Notifications with multiple handlers (pub/sub)");
Console.WriteLine("✓ Custom interceptors (LoggingInterceptor)");
Console.WriteLine("✓ OpenTelemetry integration (traces + metrics)");
Console.WriteLine("✓ Auto-discovery of handlers and validators");
Console.WriteLine("✓ Scoped lifetime (supports DbContext injection)");
Console.WriteLine();

Console.WriteLine("═════════════════════════════════════════════════════");
Console.WriteLine("TIP: Review the console output above to see:");
Console.WriteLine("  • Validation errors with property names");
Console.WriteLine("  • Notification handlers executing (email + audit)");
Console.WriteLine("  • OpenTelemetry traces and metrics");
Console.WriteLine("  • Custom interceptor logs");
Console.WriteLine("═════════════════════════════════════════════════════");
Console.WriteLine();

Console.WriteLine("Press any key to exit...");
Console.ReadKey();
