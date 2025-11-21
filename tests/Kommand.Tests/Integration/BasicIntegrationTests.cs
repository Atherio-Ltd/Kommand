namespace Kommand.Tests.Integration;

using Kommand;
using Kommand.Abstractions;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Integration tests that verify end-to-end functionality of the mediator pattern
/// with real DI container and handler resolution.
/// </summary>
public class BasicIntegrationTests
{
    /// <summary>
    /// Verifies that SendAsync correctly resolves and invokes a command handler
    /// that returns a result.
    /// </summary>
    [Fact]
    public async Task SendAsync_WithCommandReturningResult_ShouldInvokeHandlerAndReturnResult()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddKommand(config =>
        {
            config.RegisterHandlersFromAssembly(typeof(TestCommand).Assembly);
        });
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act
        var result = await mediator.SendAsync(new TestCommand("test"), CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test-handled", result);
    }

    /// <summary>
    /// Verifies that SendAsync correctly handles void commands (commands that return Unit).
    /// </summary>
    [Fact]
    public async Task SendAsync_WithVoidCommand_ShouldInvokeHandlerWithoutReturningValue()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<VoidCommandTracker>(); // Track command execution
        services.AddKommand(config =>
        {
            config.RegisterHandlersFromAssembly(typeof(VoidCommand).Assembly);
        });
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        var tracker = provider.GetRequiredService<VoidCommandTracker>();

        // Act
        await mediator.SendAsync(new VoidCommand("void-test"), CancellationToken.None);

        // Assert
        Assert.True(tracker.WasExecuted);
        Assert.Equal("void-test", tracker.LastValue);
    }

    /// <summary>
    /// Verifies that QueryAsync correctly resolves and invokes a query handler.
    /// </summary>
    [Fact]
    public async Task QueryAsync_WithRegisteredHandler_ShouldInvokeHandlerAndReturnResult()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddKommand(config =>
        {
            config.RegisterHandlersFromAssembly(typeof(TestQuery).Assembly);
        });
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act
        var result = await mediator.QueryAsync(new TestQuery(42), CancellationToken.None);

        // Assert
        Assert.Equal(84, result);
    }

    /// <summary>
    /// Verifies that PublishAsync invokes all registered notification handlers.
    /// </summary>
    [Fact]
    public async Task PublishAsync_WithMultipleHandlers_ShouldInvokeAllHandlers()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<TestNotificationTracker>();
        services.AddKommand(config =>
        {
            config.RegisterHandlersFromAssembly(typeof(TestNotification).Assembly);
        });
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        var tracker = provider.GetRequiredService<TestNotificationTracker>();

        // Act
        await mediator.PublishAsync(new TestNotification(), CancellationToken.None);

        // Assert
        Assert.Equal(2, tracker.CallCount);
    }

    /// <summary>
    /// Verifies that handlers are registered with Scoped lifetime by default.
    /// </summary>
    [Fact]
    public void RegisterHandlersFromAssembly_WithDefaultLifetime_ShouldRegisterHandlersAsScoped()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddKommand(config =>
        {
            config.RegisterHandlersFromAssembly(typeof(TestCommand).Assembly);
        });

        // Act
        var handlerDescriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(ICommandHandler<TestCommand, string>));

        // Assert
        Assert.NotNull(handlerDescriptor);
        Assert.Equal(ServiceLifetime.Scoped, handlerDescriptor.Lifetime);
    }

    /// <summary>
    /// Verifies that IMediator is registered with Scoped lifetime.
    /// </summary>
    [Fact]
    public void AddKommand_ShouldRegisterMediatorAsScoped()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddKommand(config => { });

        // Act
        var mediatorDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IMediator));

        // Assert
        Assert.NotNull(mediatorDescriptor);
        Assert.Equal(ServiceLifetime.Scoped, mediatorDescriptor.Lifetime);
    }
}

// ============================================================================
// Test Fixtures: Commands
// ============================================================================

/// <summary>
/// Test command that returns a string result.
/// </summary>
public record TestCommand(string Value) : ICommand<string>;

/// <summary>
/// Handler for TestCommand that appends "-handled" to the command value.
/// </summary>
public class TestCommandHandler : ICommandHandler<TestCommand, string>
{
    public Task<string> HandleAsync(TestCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        return Task.FromResult(command.Value + "-handled");
    }
}

/// <summary>
/// Test command that returns Unit (void command pattern).
/// </summary>
public record VoidCommand(string Value) : ICommand;

/// <summary>
/// Tracker to verify void command execution.
/// </summary>
public class VoidCommandTracker
{
    public bool WasExecuted { get; set; }
    public string? LastValue { get; set; }
}

/// <summary>
/// Handler for VoidCommand that tracks execution in VoidCommandTracker.
/// </summary>
public class VoidCommandHandler : ICommandHandler<VoidCommand, Unit>
{
    private readonly VoidCommandTracker _tracker;

    public VoidCommandHandler(VoidCommandTracker tracker)
    {
        _tracker = tracker;
    }

    public Task<Unit> HandleAsync(VoidCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        _tracker.WasExecuted = true;
        _tracker.LastValue = command.Value;
        return Task.FromResult(Unit.Value);
    }
}

// ============================================================================
// Test Fixtures: Queries
// ============================================================================

/// <summary>
/// Test query that returns an integer result (doubles the input value).
/// </summary>
public record TestQuery(int Value) : IQuery<int>;

/// <summary>
/// Handler for TestQuery that doubles the query value.
/// </summary>
public class TestQueryHandler : IQueryHandler<TestQuery, int>
{
    public Task<int> HandleAsync(TestQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        return Task.FromResult(query.Value * 2);
    }
}

// ============================================================================
// Test Fixtures: Notifications
// ============================================================================

/// <summary>
/// Test notification for pub/sub testing.
/// </summary>
public record TestNotification : INotification;

/// <summary>
/// Tracker to verify notification handler execution.
/// </summary>
public class TestNotificationTracker
{
    public int CallCount { get; private set; }
    public void Increment() => CallCount++;
}

/// <summary>
/// First handler for TestNotification.
/// </summary>
public class TestNotificationHandler1 : INotificationHandler<TestNotification>
{
    private readonly TestNotificationTracker _tracker;

    public TestNotificationHandler1(TestNotificationTracker tracker)
    {
        _tracker = tracker;
    }

    public Task HandleAsync(TestNotification notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        _tracker.Increment();
        return Task.CompletedTask;
    }
}

/// <summary>
/// Second handler for TestNotification.
/// </summary>
public class TestNotificationHandler2 : INotificationHandler<TestNotification>
{
    private readonly TestNotificationTracker _tracker;

    public TestNotificationHandler2(TestNotificationTracker tracker)
    {
        _tracker = tracker;
    }

    public Task HandleAsync(TestNotification notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        _tracker.Increment();
        return Task.CompletedTask;
    }
}
