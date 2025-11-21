namespace Kommand.Tests.Unit;

using Kommand;
using Kommand.Abstractions;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Unit tests for the interceptor pipeline functionality.
/// Verifies that interceptors execute in correct order and wrap handler execution properly.
/// </summary>
public class InterceptorTests
{
    /// <summary>
    /// Verifies that when no interceptors are registered, the handler executes directly without wrapping.
    /// </summary>
    [Fact]
    public async Task SendAsync_WithNoInterceptors_ShouldInvokeHandlerDirectly()
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
        Assert.Equal("test-handled", result);
    }

    /// <summary>
    /// Verifies that a single interceptor wraps handler execution and can execute logic before/after the handler.
    /// </summary>
    [Fact]
    public async Task SendAsync_WithOneInterceptor_ShouldExecuteInterceptorAroundHandler()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<ExecutionTracker>();
        services.AddKommand(config =>
        {
            config.RegisterHandlersFromAssembly(typeof(TestCommand).Assembly);
            config.AddInterceptor<TrackingInterceptor>();
        });

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        var tracker = provider.GetRequiredService<ExecutionTracker>();

        // Act
        await mediator.SendAsync(new TestCommand("test"), CancellationToken.None);

        // Assert
        Assert.Equal(3, tracker.ExecutionLog.Count);
        Assert.Equal("Interceptor-Enter", tracker.ExecutionLog[0]);
        Assert.Equal("Handler", tracker.ExecutionLog[1]);
        Assert.Equal("Interceptor-Exit", tracker.ExecutionLog[2]);
    }

    /// <summary>
    /// Verifies that multiple interceptors execute in correct order:
    /// First registered = outermost (executes first on entry, last on exit)
    /// Last registered = innermost (executes last on entry, first on exit)
    /// </summary>
    [Fact]
    public async Task SendAsync_WithMultipleInterceptors_ShouldExecuteInCorrectOrder()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<ExecutionTracker>();
        services.AddKommand(config =>
        {
            config.RegisterHandlersFromAssembly(typeof(TestCommand).Assembly);
            config.AddInterceptor<Interceptor1>(); // First registered = outermost
            config.AddInterceptor<Interceptor2>(); // Second registered = innermost
        });

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        var tracker = provider.GetRequiredService<ExecutionTracker>();

        // Act
        await mediator.SendAsync(new TestCommand("test"), CancellationToken.None);

        // Assert - Verify execution order
        Assert.Equal(5, tracker.ExecutionLog.Count);
        Assert.Equal("Interceptor1-Enter", tracker.ExecutionLog[0]); // Outermost enters first
        Assert.Equal("Interceptor2-Enter", tracker.ExecutionLog[1]); // Innermost enters second
        Assert.Equal("Handler", tracker.ExecutionLog[2]);             // Handler executes
        Assert.Equal("Interceptor2-Exit", tracker.ExecutionLog[3]);  // Innermost exits first
        Assert.Equal("Interceptor1-Exit", tracker.ExecutionLog[4]);  // Outermost exits last
    }

    /// <summary>
    /// Verifies that interceptors work with queries as well as commands.
    /// </summary>
    [Fact]
    public async Task QueryAsync_WithInterceptor_ShouldExecuteInterceptor()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<ExecutionTracker>();
        services.AddKommand(config =>
        {
            config.RegisterHandlersFromAssembly(typeof(TestQuery).Assembly);
            config.AddInterceptor<QueryTrackingInterceptor>();
        });

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        var tracker = provider.GetRequiredService<ExecutionTracker>();

        // Act
        var result = await mediator.QueryAsync(new TestQuery(21), CancellationToken.None);

        // Assert
        Assert.Equal(42, result);
        Assert.Contains("QueryInterceptor-Enter", tracker.ExecutionLog);
        Assert.Contains("QueryHandler", tracker.ExecutionLog);
        Assert.Contains("QueryInterceptor-Exit", tracker.ExecutionLog);
    }

    /// <summary>
    /// Verifies that interceptor exceptions propagate to the caller correctly.
    /// </summary>
    [Fact]
    public async Task SendAsync_WhenInterceptorThrows_ShouldPropagateException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddKommand(config =>
        {
            config.RegisterHandlersFromAssembly(typeof(TestCommand).Assembly);
            config.AddInterceptor<ThrowingInterceptor>();
        });

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => mediator.SendAsync(new TestCommand("test"), CancellationToken.None));

        Assert.Equal("Interceptor intentionally threw", exception.Message);
    }

    /// <summary>
    /// Verifies that when an interceptor short-circuits by not calling next(),
    /// the handler and subsequent interceptors don't execute.
    /// </summary>
    [Fact]
    public async Task SendAsync_WhenInterceptorShortCircuits_ShouldNotExecuteHandler()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<ExecutionTracker>();
        services.AddKommand(config =>
        {
            config.RegisterHandlersFromAssembly(typeof(TestCommand).Assembly);
            config.AddInterceptor<ShortCircuitInterceptor>();
        });

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        var tracker = provider.GetRequiredService<ExecutionTracker>();

        // Act
        var result = await mediator.SendAsync(new TestCommand("test"), CancellationToken.None);

        // Assert
        Assert.Equal("short-circuited", result);
        Assert.Contains("ShortCircuit-Enter", tracker.ExecutionLog);
        Assert.DoesNotContain("Handler", tracker.ExecutionLog); // Handler should NOT execute
    }

    /// <summary>
    /// Verifies that interceptors work with void commands (ICommand without response).
    /// </summary>
    [Fact]
    public async Task SendAsync_VoidCommand_WithInterceptor_ShouldExecuteInterceptor()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<ExecutionTracker>();
        services.AddKommand(config =>
        {
            config.RegisterHandlersFromAssembly(typeof(VoidTestCommand).Assembly);
            config.AddInterceptor<VoidCommandInterceptor>();
        });

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        var tracker = provider.GetRequiredService<ExecutionTracker>();

        // Act
        await mediator.SendAsync(new VoidTestCommand("test"), CancellationToken.None);

        // Assert
        Assert.Equal(3, tracker.ExecutionLog.Count);
        Assert.Equal("VoidInterceptor-Enter", tracker.ExecutionLog[0]);
        Assert.Equal("VoidHandler", tracker.ExecutionLog[1]);
        Assert.Equal("VoidInterceptor-Exit", tracker.ExecutionLog[2]);
    }

    /// <summary>
    /// Verifies that ICommandInterceptor interface works correctly (command-specific interceptor).
    /// </summary>
    [Fact]
    public async Task SendAsync_WithCommandInterceptor_ShouldExecuteInterceptor()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<ExecutionTracker>();
        services.AddKommand(config =>
        {
            config.RegisterHandlersFromAssembly(typeof(TestCommand).Assembly);
            config.AddInterceptor<CommandSpecificInterceptor>();
        });

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        var tracker = provider.GetRequiredService<ExecutionTracker>();

        // Act
        await mediator.SendAsync(new TestCommand("test"), CancellationToken.None);

        // Assert
        Assert.Contains("CommandInterceptor-Enter", tracker.ExecutionLog);
        Assert.Contains("Handler", tracker.ExecutionLog);
        Assert.Contains("CommandInterceptor-Exit", tracker.ExecutionLog);
    }

    /// <summary>
    /// Verifies that IQueryInterceptor interface works correctly (query-specific interceptor).
    /// </summary>
    [Fact]
    public async Task QueryAsync_WithQueryInterceptor_ShouldExecuteInterceptor()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<ExecutionTracker>();
        services.AddKommand(config =>
        {
            config.RegisterHandlersFromAssembly(typeof(TestQuery).Assembly);
            config.AddInterceptor<QuerySpecificInterceptor>();
        });

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        var tracker = provider.GetRequiredService<ExecutionTracker>();

        // Act
        var result = await mediator.QueryAsync(new TestQuery(21), CancellationToken.None);

        // Assert
        Assert.Equal(42, result);
        Assert.Contains("QuerySpecificInterceptor-Enter", tracker.ExecutionLog);
        Assert.Contains("QueryHandler", tracker.ExecutionLog);
        Assert.Contains("QuerySpecificInterceptor-Exit", tracker.ExecutionLog);
    }

    /// <summary>
    /// Verifies that an interceptor can modify the response from the handler.
    /// </summary>
    [Fact]
    public async Task SendAsync_InterceptorModifyingResponse_ShouldReturnModifiedValue()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddKommand(config =>
        {
            config.RegisterHandlersFromAssembly(typeof(TestCommand).Assembly);
            config.AddInterceptor<ResponseModifyingInterceptor>();
        });

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act
        var result = await mediator.SendAsync(new TestCommand("test"), CancellationToken.None);

        // Assert - Interceptor should have appended "-modified"
        Assert.Equal("test-handled-modified", result);
    }

    /// <summary>
    /// Verifies that when an inner interceptor throws, outer interceptors can catch and handle the exception.
    /// </summary>
    [Fact]
    public async Task SendAsync_InnerInterceptorThrows_OuterInterceptorCanCatch()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<ExecutionTracker>();
        services.AddKommand(config =>
        {
            config.RegisterHandlersFromAssembly(typeof(TestCommand).Assembly);
            config.AddInterceptor<ExceptionCatchingInterceptor>(); // Outer - catches exceptions
            config.AddInterceptor<ThrowingInterceptor>(); // Inner - throws exception
        });

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        var tracker = provider.GetRequiredService<ExecutionTracker>();

        // Act
        var result = await mediator.SendAsync(new TestCommand("test"), CancellationToken.None);

        // Assert - Outer interceptor should catch and return fallback
        Assert.Equal("exception-caught", result);
        Assert.Contains("OuterInterceptor-Enter", tracker.ExecutionLog);
        Assert.Contains("OuterInterceptor-CaughtException", tracker.ExecutionLog);
        Assert.Contains("OuterInterceptor-Exit", tracker.ExecutionLog);
    }

    /// <summary>
    /// Verifies that interceptors can inject realistic dependencies like ILogger.
    /// </summary>
    [Fact]
    public async Task SendAsync_InterceptorWithDependencyInjection_ShouldInjectServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<FakeLogger>();
        services.AddKommand(config =>
        {
            config.RegisterHandlersFromAssembly(typeof(TestCommand).Assembly);
            config.AddInterceptor<LoggingDependencyInterceptor>();
        });

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        var logger = provider.GetRequiredService<FakeLogger>();

        // Act
        await mediator.SendAsync(new TestCommand("test"), CancellationToken.None);

        // Assert
        Assert.Contains("Executing command: TestCommand", logger.Messages);
        Assert.Contains("Command completed: TestCommand", logger.Messages);
    }

    /// <summary>
    /// Verifies that CancellationToken is properly propagated through the interceptor pipeline.
    /// </summary>
    [Fact]
    public async Task SendAsync_CancellationToken_ShouldPropagateThroughPipeline()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<CancellationTokenCapture>();
        services.AddKommand(config =>
        {
            config.RegisterHandlersFromAssembly(typeof(CancellableCommand).Assembly);
            config.AddInterceptor<CancellationTokenInterceptor>();
        });

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        var capture = provider.GetRequiredService<CancellationTokenCapture>();

        var cts = new CancellationTokenSource();

        // Act
        await mediator.SendAsync(new CancellableCommand(), cts.Token);

        // Assert
        Assert.True(capture.InterceptorReceivedToken);
        Assert.True(capture.HandlerReceivedToken);
        Assert.Equal(cts.Token, capture.CapturedToken);
    }

    /// <summary>
    /// Verifies that interceptors can perform async operations (e.g., delays, I/O).
    /// </summary>
    [Fact]
    public async Task SendAsync_AsyncInterceptor_ShouldAwaitAsyncOperations()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<ExecutionTracker>();
        services.AddKommand(config =>
        {
            config.RegisterHandlersFromAssembly(typeof(TestCommand).Assembly);
            config.AddInterceptor<AsyncDelayInterceptor>();
        });

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        var tracker = provider.GetRequiredService<ExecutionTracker>();

        var startTime = DateTime.UtcNow;

        // Act
        await mediator.SendAsync(new TestCommand("test"), CancellationToken.None);

        var duration = DateTime.UtcNow - startTime;

        // Assert - Should have delayed at least 50ms
        Assert.True(duration.TotalMilliseconds >= 50, $"Expected delay >= 50ms, got {duration.TotalMilliseconds}ms");
        Assert.Contains("AsyncInterceptor-BeforeDelay", tracker.ExecutionLog);
        Assert.Contains("AsyncInterceptor-AfterDelay", tracker.ExecutionLog);
    }

    /// <summary>
    /// Verifies that 3 or more interceptors execute in correct order.
    /// </summary>
    [Fact]
    public async Task SendAsync_WithThreeInterceptors_ShouldExecuteInCorrectOrder()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<ExecutionTracker>();
        services.AddKommand(config =>
        {
            config.RegisterHandlersFromAssembly(typeof(TestCommand).Assembly);
            config.AddInterceptor<Interceptor1>(); // Outermost
            config.AddInterceptor<Interceptor2>(); // Middle
            config.AddInterceptor<Interceptor3>(); // Innermost
        });

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        var tracker = provider.GetRequiredService<ExecutionTracker>();

        // Act
        await mediator.SendAsync(new TestCommand("test"), CancellationToken.None);

        // Assert - Verify execution order with 3 interceptors
        Assert.Equal(7, tracker.ExecutionLog.Count);
        Assert.Equal("Interceptor1-Enter", tracker.ExecutionLog[0]); // Outermost enters first
        Assert.Equal("Interceptor2-Enter", tracker.ExecutionLog[1]); // Middle enters second
        Assert.Equal("Interceptor3-Enter", tracker.ExecutionLog[2]); // Innermost enters third
        Assert.Equal("Handler", tracker.ExecutionLog[3]);             // Handler executes
        Assert.Equal("Interceptor3-Exit", tracker.ExecutionLog[4]);  // Innermost exits first
        Assert.Equal("Interceptor2-Exit", tracker.ExecutionLog[5]);  // Middle exits second
        Assert.Equal("Interceptor1-Exit", tracker.ExecutionLog[6]);  // Outermost exits last
    }
}

// ============================================================================
// Test Fixtures: Test Command and Handler
// ============================================================================

public record TestCommand(string Value) : ICommand<string>;

public class TestCommandHandler : ICommandHandler<TestCommand, string>
{
    private readonly ExecutionTracker? _tracker;

    public TestCommandHandler(ExecutionTracker? tracker = null)
    {
        _tracker = tracker;
    }

    public Task<string> HandleAsync(TestCommand command, CancellationToken cancellationToken)
    {
        _tracker?.Log("Handler");
        return Task.FromResult(command.Value + "-handled");
    }
}

// ============================================================================
// Test Fixtures: Test Query and Handler
// ============================================================================

public record TestQuery(int Value) : IQuery<int>;

public class TestQueryHandler : IQueryHandler<TestQuery, int>
{
    private readonly ExecutionTracker? _tracker;

    public TestQueryHandler(ExecutionTracker? tracker = null)
    {
        _tracker = tracker;
    }

    public Task<int> HandleAsync(TestQuery query, CancellationToken cancellationToken)
    {
        _tracker?.Log("QueryHandler");
        return Task.FromResult(query.Value * 2);
    }
}

// ============================================================================
// Test Fixtures: Tracking Interceptor
// ============================================================================

public class TrackingInterceptor : IInterceptor<TestCommand, string>
{
    private readonly ExecutionTracker _tracker;

    public TrackingInterceptor(ExecutionTracker tracker)
    {
        _tracker = tracker;
    }

    public async Task<string> HandleAsync(
        TestCommand request,
        RequestHandlerDelegate<string> next,
        CancellationToken cancellationToken)
    {
        _tracker.Log("Interceptor-Enter");
        var result = await next();
        _tracker.Log("Interceptor-Exit");
        return result;
    }
}

// ============================================================================
// Test Fixtures: Multiple Interceptors for Order Testing
// ============================================================================

public class Interceptor1 : IInterceptor<TestCommand, string>
{
    private readonly ExecutionTracker _tracker;

    public Interceptor1(ExecutionTracker tracker)
    {
        _tracker = tracker;
    }

    public async Task<string> HandleAsync(
        TestCommand request,
        RequestHandlerDelegate<string> next,
        CancellationToken cancellationToken)
    {
        _tracker.Log("Interceptor1-Enter");
        var result = await next();
        _tracker.Log("Interceptor1-Exit");
        return result;
    }
}

public class Interceptor2 : IInterceptor<TestCommand, string>
{
    private readonly ExecutionTracker _tracker;

    public Interceptor2(ExecutionTracker tracker)
    {
        _tracker = tracker;
    }

    public async Task<string> HandleAsync(
        TestCommand request,
        RequestHandlerDelegate<string> next,
        CancellationToken cancellationToken)
    {
        _tracker.Log("Interceptor2-Enter");
        var result = await next();
        _tracker.Log("Interceptor2-Exit");
        return result;
    }
}

// ============================================================================
// Test Fixtures: Query Interceptor
// ============================================================================

public class QueryTrackingInterceptor : IInterceptor<TestQuery, int>
{
    private readonly ExecutionTracker _tracker;

    public QueryTrackingInterceptor(ExecutionTracker tracker)
    {
        _tracker = tracker;
    }

    public async Task<int> HandleAsync(
        TestQuery request,
        RequestHandlerDelegate<int> next,
        CancellationToken cancellationToken)
    {
        _tracker.Log("QueryInterceptor-Enter");
        var result = await next();
        _tracker.Log("QueryInterceptor-Exit");
        return result;
    }
}

// ============================================================================
// Test Fixtures: Throwing Interceptor
// ============================================================================

public class ThrowingInterceptor : IInterceptor<TestCommand, string>
{
    public Task<string> HandleAsync(
        TestCommand request,
        RequestHandlerDelegate<string> next,
        CancellationToken cancellationToken)
    {
        throw new InvalidOperationException("Interceptor intentionally threw");
    }
}

// ============================================================================
// Test Fixtures: Short-Circuit Interceptor
// ============================================================================

public class ShortCircuitInterceptor : IInterceptor<TestCommand, string>
{
    private readonly ExecutionTracker _tracker;

    public ShortCircuitInterceptor(ExecutionTracker tracker)
    {
        _tracker = tracker;
    }

    public Task<string> HandleAsync(
        TestCommand request,
        RequestHandlerDelegate<string> next,
        CancellationToken cancellationToken)
    {
        _tracker.Log("ShortCircuit-Enter");
        // Don't call next() - short-circuit the pipeline
        return Task.FromResult("short-circuited");
    }
}

// ============================================================================
// Test Fixtures: Execution Tracker
// ============================================================================

public class ExecutionTracker
{
    private readonly List<string> _log = new();

    public IReadOnlyList<string> ExecutionLog => _log;

    public void Log(string message) => _log.Add(message);
}

// ============================================================================
// Test Fixtures: Void Command
// ============================================================================

public record VoidTestCommand(string Value) : ICommand;

public class VoidTestCommandHandler : ICommandHandler<VoidTestCommand, Unit>
{
    private readonly ExecutionTracker _tracker;

    public VoidTestCommandHandler(ExecutionTracker tracker)
    {
        _tracker = tracker;
    }

    public Task<Unit> HandleAsync(VoidTestCommand command, CancellationToken cancellationToken)
    {
        _tracker.Log("VoidHandler");
        return Task.FromResult(Unit.Value);
    }
}

public class VoidCommandInterceptor : IInterceptor<VoidTestCommand, Unit>
{
    private readonly ExecutionTracker _tracker;

    public VoidCommandInterceptor(ExecutionTracker tracker)
    {
        _tracker = tracker;
    }

    public async Task<Unit> HandleAsync(
        VoidTestCommand request,
        RequestHandlerDelegate<Unit> next,
        CancellationToken cancellationToken)
    {
        _tracker.Log("VoidInterceptor-Enter");
        var result = await next();
        _tracker.Log("VoidInterceptor-Exit");
        return result;
    }
}

// ============================================================================
// Test Fixtures: ICommandInterceptor and IQueryInterceptor
// ============================================================================

public class CommandSpecificInterceptor : ICommandInterceptor<TestCommand, string>
{
    private readonly ExecutionTracker _tracker;

    public CommandSpecificInterceptor(ExecutionTracker tracker)
    {
        _tracker = tracker;
    }

    public async Task<string> HandleAsync(
        TestCommand command,
        RequestHandlerDelegate<string> next,
        CancellationToken cancellationToken)
    {
        _tracker.Log("CommandInterceptor-Enter");
        var result = await next();
        _tracker.Log("CommandInterceptor-Exit");
        return result;
    }
}

public class QuerySpecificInterceptor : IQueryInterceptor<TestQuery, int>
{
    private readonly ExecutionTracker _tracker;

    public QuerySpecificInterceptor(ExecutionTracker tracker)
    {
        _tracker = tracker;
    }

    public async Task<int> HandleAsync(
        TestQuery query,
        RequestHandlerDelegate<int> next,
        CancellationToken cancellationToken)
    {
        _tracker.Log("QuerySpecificInterceptor-Enter");
        var result = await next();
        _tracker.Log("QuerySpecificInterceptor-Exit");
        return result;
    }
}

// ============================================================================
// Test Fixtures: Response Modifying Interceptor
// ============================================================================

public class ResponseModifyingInterceptor : IInterceptor<TestCommand, string>
{
    public async Task<string> HandleAsync(
        TestCommand request,
        RequestHandlerDelegate<string> next,
        CancellationToken cancellationToken)
    {
        var result = await next();
        return result + "-modified";
    }
}

// ============================================================================
// Test Fixtures: Exception Catching Interceptor
// ============================================================================

public class ExceptionCatchingInterceptor : IInterceptor<TestCommand, string>
{
    private readonly ExecutionTracker _tracker;

    public ExceptionCatchingInterceptor(ExecutionTracker tracker)
    {
        _tracker = tracker;
    }

    public async Task<string> HandleAsync(
        TestCommand request,
        RequestHandlerDelegate<string> next,
        CancellationToken cancellationToken)
    {
        _tracker.Log("OuterInterceptor-Enter");
        try
        {
            return await next();
        }
        catch (InvalidOperationException)
        {
            _tracker.Log("OuterInterceptor-CaughtException");
            _tracker.Log("OuterInterceptor-Exit");
            return "exception-caught";
        }
    }
}

// ============================================================================
// Test Fixtures: Dependency Injection with Logger
// ============================================================================

public class FakeLogger
{
    private readonly List<string> _messages = new();

    public IReadOnlyList<string> Messages => _messages;

    public void Log(string message) => _messages.Add(message);
}

public class LoggingDependencyInterceptor : IInterceptor<TestCommand, string>
{
    private readonly FakeLogger _logger;

    public LoggingDependencyInterceptor(FakeLogger logger)
    {
        _logger = logger;
    }

    public async Task<string> HandleAsync(
        TestCommand request,
        RequestHandlerDelegate<string> next,
        CancellationToken cancellationToken)
    {
        _logger.Log($"Executing command: {request.GetType().Name}");
        var result = await next();
        _logger.Log($"Command completed: {request.GetType().Name}");
        return result;
    }
}

// ============================================================================
// Test Fixtures: CancellationToken Propagation
// ============================================================================

public record CancellableCommand : ICommand<string>;

public class CancellableCommandHandler : ICommandHandler<CancellableCommand, string>
{
    private readonly CancellationTokenCapture _capture;

    public CancellableCommandHandler(CancellationTokenCapture capture)
    {
        _capture = capture;
    }

    public Task<string> HandleAsync(CancellableCommand command, CancellationToken cancellationToken)
    {
        _capture.HandlerReceivedToken = !cancellationToken.Equals(default(CancellationToken));
        _capture.CapturedToken = cancellationToken;
        return Task.FromResult("handled");
    }
}

public class CancellationTokenCapture
{
    public bool InterceptorReceivedToken { get; set; }
    public bool HandlerReceivedToken { get; set; }
    public CancellationToken CapturedToken { get; set; }
}

public class CancellationTokenInterceptor : IInterceptor<CancellableCommand, string>
{
    private readonly CancellationTokenCapture _capture;

    public CancellationTokenInterceptor(CancellationTokenCapture capture)
    {
        _capture = capture;
    }

    public async Task<string> HandleAsync(
        CancellableCommand request,
        RequestHandlerDelegate<string> next,
        CancellationToken cancellationToken)
    {
        _capture.InterceptorReceivedToken = !cancellationToken.Equals(default(CancellationToken));
        return await next();
    }
}

// ============================================================================
// Test Fixtures: Async Delay Interceptor
// ============================================================================

public class AsyncDelayInterceptor : IInterceptor<TestCommand, string>
{
    private readonly ExecutionTracker _tracker;

    public AsyncDelayInterceptor(ExecutionTracker tracker)
    {
        _tracker = tracker;
    }

    public async Task<string> HandleAsync(
        TestCommand request,
        RequestHandlerDelegate<string> next,
        CancellationToken cancellationToken)
    {
        _tracker.Log("AsyncInterceptor-BeforeDelay");
        await Task.Delay(50, cancellationToken); // Async operation
        _tracker.Log("AsyncInterceptor-AfterDelay");
        return await next();
    }
}

// ============================================================================
// Test Fixtures: Third Interceptor for 3-Interceptor Test
// ============================================================================

public class Interceptor3 : IInterceptor<TestCommand, string>
{
    private readonly ExecutionTracker _tracker;

    public Interceptor3(ExecutionTracker tracker)
    {
        _tracker = tracker;
    }

    public async Task<string> HandleAsync(
        TestCommand request,
        RequestHandlerDelegate<string> next,
        CancellationToken cancellationToken)
    {
        _tracker.Log("Interceptor3-Enter");
        var result = await next();
        _tracker.Log("Interceptor3-Exit");
        return result;
    }
}
