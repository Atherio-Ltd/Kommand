namespace Kommand.Tests.Integration;

using Kommand;
using Kommand.Abstractions;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Integration tests that verify error handling and edge cases in the mediator implementation.
/// </summary>
public class MediatorErrorHandlingTests
{
    /// <summary>
    /// Verifies that SendAsync throws ArgumentNullException when command is null.
    /// </summary>
    [Fact]
    public async Task SendAsync_WithNullCommand_ShouldThrowArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddKommand(config => { });
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => mediator.SendAsync<string>(null!, CancellationToken.None));
    }

    /// <summary>
    /// Verifies that SendAsync (void overload) throws ArgumentNullException when command is null.
    /// </summary>
    [Fact]
    public async Task SendAsync_VoidOverloadWithNullCommand_ShouldThrowArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddKommand(config => { });
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => mediator.SendAsync((ICommand)null!, CancellationToken.None));
    }

    /// <summary>
    /// Verifies that SendAsync throws InvalidOperationException when no handler is registered.
    /// </summary>
    [Fact]
    public async Task SendAsync_WithNoHandlerRegistered_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddKommand(config => { }); // No handlers registered
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => mediator.SendAsync(new UnregisteredCommand("test"), CancellationToken.None));

        Assert.Contains("No handler registered for command type", exception.Message);
        Assert.Contains("UnregisteredCommand", exception.Message);
    }

    /// <summary>
    /// Verifies that QueryAsync throws ArgumentNullException when query is null.
    /// </summary>
    [Fact]
    public async Task QueryAsync_WithNullQuery_ShouldThrowArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddKommand(config => { });
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => mediator.QueryAsync<string>(null!, CancellationToken.None));
    }

    /// <summary>
    /// Verifies that QueryAsync throws InvalidOperationException when no handler is registered.
    /// </summary>
    [Fact]
    public async Task QueryAsync_WithNoHandlerRegistered_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddKommand(config => { }); // No handlers registered
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => mediator.QueryAsync(new UnregisteredQuery(), CancellationToken.None));

        Assert.Contains("No handler registered for query type", exception.Message);
        Assert.Contains("UnregisteredQuery", exception.Message);
    }

    /// <summary>
    /// Verifies that PublishAsync throws ArgumentNullException when notification is null.
    /// </summary>
    [Fact]
    public async Task PublishAsync_WithNullNotification_ShouldThrowArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddKommand(config => { });
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => mediator.PublishAsync<INotification>(null!, CancellationToken.None));
    }

    /// <summary>
    /// Verifies that PublishAsync completes successfully when no handlers are registered.
    /// This is fire-and-forget behavior - no handlers is valid for notifications.
    /// </summary>
    [Fact]
    public async Task PublishAsync_WithNoHandlersRegistered_ShouldCompleteSuccessfully()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddKommand(config => { }); // No handlers registered
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act & Assert - Should not throw
        await mediator.PublishAsync(new UnregisteredNotification(), CancellationToken.None);
    }

    /// <summary>
    /// Verifies that handler exceptions propagate to the caller for commands.
    /// </summary>
    [Fact]
    public async Task SendAsync_WhenHandlerThrows_ShouldPropagateException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddKommand(config =>
        {
            config.RegisterHandlersFromAssembly(typeof(ThrowingCommand).Assembly);
        });
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => mediator.SendAsync(new ThrowingCommand(), CancellationToken.None));

        Assert.Equal("Handler intentionally threw", exception.Message);
    }

    /// <summary>
    /// Verifies that handler exceptions propagate to the caller for queries.
    /// </summary>
    [Fact]
    public async Task QueryAsync_WhenHandlerThrows_ShouldPropagateException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddKommand(config =>
        {
            config.RegisterHandlersFromAssembly(typeof(ThrowingQuery).Assembly);
        });
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => mediator.QueryAsync(new ThrowingQuery(), CancellationToken.None));

        Assert.Equal("Query handler intentionally threw", exception.Message);
    }

    /// <summary>
    /// Verifies that when one notification handler throws, other handlers still execute.
    /// This tests the resilience behavior of notification publishing.
    /// </summary>
    [Fact]
    public async Task PublishAsync_WhenOneHandlerThrows_ShouldExecuteOtherHandlers()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<NotificationExecutionTracker>();
        services.AddKommand(config =>
        {
            config.RegisterHandlersFromAssembly(typeof(ResilientNotification).Assembly);
        });
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        var tracker = provider.GetRequiredService<NotificationExecutionTracker>();

        // Act - Should not throw even though one handler fails
        await mediator.PublishAsync(new ResilientNotification(), CancellationToken.None);

        // Assert - All non-throwing handlers should have executed
        Assert.Equal(2, tracker.ExecutedHandlers.Count);
        Assert.Contains("Handler1", tracker.ExecutedHandlers);
        Assert.Contains("Handler3", tracker.ExecutedHandlers);
        Assert.DoesNotContain("Handler2", tracker.ExecutedHandlers); // This one throws
    }

    /// <summary>
    /// Verifies that PublishAsync doesn't throw even when all handlers throw exceptions.
    /// </summary>
    [Fact]
    public async Task PublishAsync_WhenAllHandlersThrow_ShouldNotThrow()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddKommand(config =>
        {
            config.RegisterHandlersFromAssembly(typeof(AllThrowingNotification).Assembly);
        });
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act & Assert - Should not throw
        await mediator.PublishAsync(new AllThrowingNotification(), CancellationToken.None);
    }

    /// <summary>
    /// Verifies that cancellation token is properly passed to command handlers.
    /// </summary>
    [Fact]
    public async Task SendAsync_ShouldPassCancellationTokenToHandler()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<CancellationTokenVerifier>();
        services.AddKommand(config =>
        {
            config.RegisterHandlersFromAssembly(typeof(CancellationTestCommand).Assembly);
        });
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        var verifier = provider.GetRequiredService<CancellationTokenVerifier>();

        using var cts = new CancellationTokenSource();

        // Act
        await mediator.SendAsync(new CancellationTestCommand(), cts.Token);

        // Assert
        Assert.True(verifier.ReceivedToken);
        Assert.Equal(cts.Token, verifier.Token);
    }
}

// ============================================================================
// Test Fixtures: Unregistered Types
// ============================================================================

public record UnregisteredCommand(string Value) : ICommand<string>;
public record UnregisteredQuery : IQuery<int>;
public record UnregisteredNotification : INotification;

// ============================================================================
// Test Fixtures: Throwing Handlers
// ============================================================================

public record ThrowingCommand : ICommand<string>;

public class ThrowingCommandHandler : ICommandHandler<ThrowingCommand, string>
{
    public Task<string> HandleAsync(ThrowingCommand command, CancellationToken cancellationToken)
    {
        throw new InvalidOperationException("Handler intentionally threw");
    }
}

public record ThrowingQuery : IQuery<int>;

public class ThrowingQueryHandler : IQueryHandler<ThrowingQuery, int>
{
    public Task<int> HandleAsync(ThrowingQuery query, CancellationToken cancellationToken)
    {
        throw new InvalidOperationException("Query handler intentionally threw");
    }
}

// ============================================================================
// Test Fixtures: Resilient Notifications
// ============================================================================

public record ResilientNotification : INotification;

public class NotificationExecutionTracker
{
    public List<string> ExecutedHandlers { get; } = new();
}

public class ResilientNotificationHandler1 : INotificationHandler<ResilientNotification>
{
    private readonly NotificationExecutionTracker _tracker;

    public ResilientNotificationHandler1(NotificationExecutionTracker tracker)
    {
        _tracker = tracker;
    }

    public Task HandleAsync(ResilientNotification notification, CancellationToken cancellationToken)
    {
        _tracker.ExecutedHandlers.Add("Handler1");
        return Task.CompletedTask;
    }
}

public class ResilientNotificationHandler2 : INotificationHandler<ResilientNotification>
{
    public Task HandleAsync(ResilientNotification notification, CancellationToken cancellationToken)
    {
        throw new InvalidOperationException("Handler2 intentionally threw");
    }
}

public class ResilientNotificationHandler3 : INotificationHandler<ResilientNotification>
{
    private readonly NotificationExecutionTracker _tracker;

    public ResilientNotificationHandler3(NotificationExecutionTracker tracker)
    {
        _tracker = tracker;
    }

    public Task HandleAsync(ResilientNotification notification, CancellationToken cancellationToken)
    {
        _tracker.ExecutedHandlers.Add("Handler3");
        return Task.CompletedTask;
    }
}

// ============================================================================
// Test Fixtures: All Throwing Notification
// ============================================================================

public record AllThrowingNotification : INotification;

public class AllThrowingNotificationHandler1 : INotificationHandler<AllThrowingNotification>
{
    public Task HandleAsync(AllThrowingNotification notification, CancellationToken cancellationToken)
    {
        throw new InvalidOperationException("Handler1 threw");
    }
}

public class AllThrowingNotificationHandler2 : INotificationHandler<AllThrowingNotification>
{
    public Task HandleAsync(AllThrowingNotification notification, CancellationToken cancellationToken)
    {
        throw new InvalidOperationException("Handler2 threw");
    }
}

// ============================================================================
// Test Fixtures: Cancellation Token Verification
// ============================================================================

public record CancellationTestCommand : ICommand<string>;

public class CancellationTokenVerifier
{
    public bool ReceivedToken { get; set; }
    public CancellationToken Token { get; set; }
}

public class CancellationTestCommandHandler : ICommandHandler<CancellationTestCommand, string>
{
    private readonly CancellationTokenVerifier _verifier;

    public CancellationTestCommandHandler(CancellationTokenVerifier verifier)
    {
        _verifier = verifier;
    }

    public Task<string> HandleAsync(CancellationTestCommand command, CancellationToken cancellationToken)
    {
        _verifier.ReceivedToken = true;
        _verifier.Token = cancellationToken;
        return Task.FromResult("success");
    }
}
