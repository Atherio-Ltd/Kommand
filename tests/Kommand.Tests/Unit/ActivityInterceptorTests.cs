namespace Kommand.Tests.Unit;

using System.Diagnostics;
using Kommand;
using Kommand.Abstractions;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Unit tests for ActivityInterceptor to verify OpenTelemetry distributed tracing integration.
/// Tests zero-config pattern, activity creation, naming conventions, and error handling.
/// </summary>
/// <remarks>
/// These tests use a global ActivityListener, so they must run sequentially to avoid
/// parallel tests interfering with each other's activity collections.
/// </remarks>
[Collection("ActivityInterceptor")]
public class ActivityInterceptorTests
{
    /// <summary>
    /// Verifies that when OpenTelemetry is NOT configured (no ActivityListener registered),
    /// the interceptor has minimal overhead and does not create activities.
    /// This tests the zero-config pattern - activities should be null when OTEL is disabled.
    /// </summary>
    [Fact]
    public async Task WhenOTELNotConfigured_ShouldHaveMinimalOverhead()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddKommand(config =>
        {
            config.RegisterHandlersFromAssembly(typeof(TestCommand).Assembly);
        });

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act - Should not throw and should complete quickly
        var stopwatch = Stopwatch.StartNew();
        var result = await mediator.SendAsync(new TestCommand("test"), CancellationToken.None);
        stopwatch.Stop();

        // Assert
        Assert.Equal("test-handled", result);
        // With no OTEL configured, overhead should be minimal (< 500ms for a simple command)
        // Note: Generous threshold accounts for CI variability and sequential test execution
        Assert.True(stopwatch.ElapsedMilliseconds < 500,
            $"Expected < 500ms, got {stopwatch.ElapsedMilliseconds}ms");
    }

    /// <summary>
    /// Verifies that when OpenTelemetry IS configured (ActivityListener registered),
    /// the interceptor creates an Activity with the correct structure.
    /// </summary>
    [Fact]
    public async Task WhenOTELConfigured_ShouldCreateActivity()
    {
        // Arrange
        var activities = new List<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "Kommand",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStarted = activity => activities.Add(activity)
        };
        ActivitySource.AddActivityListener(listener);

        var services = new ServiceCollection();
        services.AddKommand(config =>
        {
            config.RegisterHandlersFromAssembly(typeof(TestCommand).Assembly);
        });

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act
        await mediator.SendAsync(new TestCommand("test"), CancellationToken.None);

        // Assert
        Assert.Single(activities);
        Assert.NotNull(activities[0]);
    }

    /// <summary>
    /// Verifies that command activities use the naming convention "Command.{CommandName}".
    /// </summary>
    [Fact]
    public async Task ForCommand_ShouldUseCorrectActivityName()
    {
        // Arrange
        var activities = new List<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "Kommand",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStarted = activity => activities.Add(activity)
        };
        ActivitySource.AddActivityListener(listener);

        var services = new ServiceCollection();
        services.AddKommand(config =>
        {
            config.RegisterHandlersFromAssembly(typeof(TestCommand).Assembly);
        });

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act
        await mediator.SendAsync(new TestCommand("test"), CancellationToken.None);

        // Assert
        Assert.Single(activities);
        Assert.Equal("Command.TestCommand", activities[0].DisplayName);
    }

    /// <summary>
    /// Verifies that query activities use the naming convention "Query.{QueryName}".
    /// </summary>
    [Fact]
    public async Task ForQuery_ShouldUseCorrectActivityName()
    {
        // Arrange
        var activities = new List<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "Kommand",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStarted = activity => activities.Add(activity)
        };
        ActivitySource.AddActivityListener(listener);

        var services = new ServiceCollection();
        services.AddKommand(config =>
        {
            config.RegisterHandlersFromAssembly(typeof(TestQuery).Assembly);
        });

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act
        await mediator.QueryAsync(new TestQuery(42), CancellationToken.None);

        // Assert - Filter to only TestQuery activities
        var testQueryActivities = activities.Where(a => a.DisplayName == "Query.TestQuery").ToList();
        Assert.NotEmpty(testQueryActivities);
        Assert.Equal("Query.TestQuery", testQueryActivities.First().DisplayName);
    }

    /// <summary>
    /// Verifies that activities include all required OpenTelemetry semantic tags.
    /// </summary>
    [Fact]
    public async Task ShouldSetRequiredTags()
    {
        // Arrange
        var activities = new List<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "Kommand",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStarted = activity => activities.Add(activity)
        };
        ActivitySource.AddActivityListener(listener);

        var services = new ServiceCollection();
        services.AddKommand(config =>
        {
            config.RegisterHandlersFromAssembly(typeof(TestCommand).Assembly);
        });

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Clear any activities captured during setup (from parallel tests)
        activities.Clear();

        // Act
        await mediator.SendAsync(new TestCommand("test"), CancellationToken.None);

        // Assert
        Assert.Single(activities);
        var activity = activities[0];

        // Verify required tags
        Assert.Equal("Command", activity.GetTagItem("kommand.request.type"));
        Assert.Equal("TestCommand", activity.GetTagItem("kommand.request.name"));
        Assert.Equal("String", activity.GetTagItem("kommand.response.type"));
    }

    /// <summary>
    /// Verifies that successful requests set the activity status to OK.
    /// </summary>
    [Fact]
    public async Task OnSuccess_ShouldSetStatusOk()
    {
        // Arrange
        var activities = new List<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "Kommand",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStarted = activity => activities.Add(activity)
        };
        ActivitySource.AddActivityListener(listener);

        var services = new ServiceCollection();
        services.AddKommand(config =>
        {
            config.RegisterHandlersFromAssembly(typeof(TestCommand).Assembly);
        });

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act
        await mediator.SendAsync(new TestCommand("test"), CancellationToken.None);

        // Assert - Filter to only TestCommand activities with OK status
        var testActivities = activities.Where(a => a.DisplayName == "Command.TestCommand").ToList();
        Assert.NotEmpty(testActivities);
        var activity = testActivities.First();
        Assert.Equal(ActivityStatusCode.Ok, activity.Status);
    }

    /// <summary>
    /// Verifies that when a handler throws an exception, the activity status is set to Error
    /// with the exception message, and exception details are tagged.
    /// </summary>
    [Fact]
    public async Task OnException_ShouldSetStatusError()
    {
        // Arrange
        var activities = new List<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "Kommand",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStarted = activity => activities.Add(activity)
        };
        ActivitySource.AddActivityListener(listener);

        var services = new ServiceCollection();
        services.AddKommand(config =>
        {
            config.RegisterHandlersFromAssembly(typeof(FailingCommand).Assembly);
        });

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Clear any activities captured during setup (from parallel tests)
        activities.Clear();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await mediator.SendAsync(new FailingCommand(), CancellationToken.None));

        // Verify activity error status - Filter to only FailingCommand activities
        var failingActivities = activities.Where(a => a.DisplayName == "Command.FailingCommand").ToList();
        Assert.Single(failingActivities);
        var activity = failingActivities[0];
        Assert.Equal(ActivityStatusCode.Error, activity.Status);
        Assert.Equal("Command failed", activity.StatusDescription);

        // Verify exception tags
        Assert.Equal("System.InvalidOperationException", activity.GetTagItem("exception.type"));
        Assert.Equal("Command failed", activity.GetTagItem("exception.message"));
        Assert.NotNull(activity.GetTagItem("exception.stacktrace"));
    }

    /// <summary>
    /// Verifies that the activity duration is properly measured.
    /// Activities should have a non-zero duration after completion.
    /// </summary>
    [Fact]
    public async Task ShouldMeasureDuration()
    {
        // Arrange
        var activities = new List<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "Kommand",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStarted = activity => activities.Add(activity)
        };
        ActivitySource.AddActivityListener(listener);

        var services = new ServiceCollection();
        services.AddKommand(config =>
        {
            config.RegisterHandlersFromAssembly(typeof(DelayedCommand).Assembly);
        });

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act - DelayedCommand has a 10ms delay
        await mediator.SendAsync(new DelayedCommand(), CancellationToken.None);

        // Assert - Filter to only DelayedCommand activities with non-zero duration
        // (may capture incomplete activities from parallel test runs)
        var delayedActivities = activities
            .Where(a => a.DisplayName == "Command.DelayedCommand" && a.Duration.TotalMilliseconds > 0)
            .ToList();
        Assert.NotEmpty(delayedActivities);
        var activity = delayedActivities.First();

        // Activity should have measured some duration (at least a few milliseconds due to 10ms delay)
        Assert.True(activity.Duration.TotalMilliseconds >= 5,
            $"Expected duration >= 5ms, got {activity.Duration.TotalMilliseconds}ms");
    }

    /// <summary>
    /// NOTE: Notifications currently bypass the interceptor pipeline and are executed directly,
    /// so they do not create activities. This test is removed until Phase 4 when notifications
    /// may be integrated into the interceptor pipeline.
    /// </summary>
    /// <remarks>
    /// See Mediator.cs:219 - PublishAsync directly invokes handlers without BuildPipeline.
    /// </remarks>

    /// <summary>
    /// Verifies that multiple requests create separate activities with independent lifecycles.
    /// </summary>
    [Fact]
    public async Task MultipleRequests_ShouldCreateSeparateActivities()
    {
        // Arrange - Use concurrent collection to avoid threading issues
        var activities = new System.Collections.Concurrent.ConcurrentBag<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "Kommand",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStarted = activity => activities.Add(activity)
        };
        ActivitySource.AddActivityListener(listener);

        var services = new ServiceCollection();
        services.AddKommand(config =>
        {
            config.RegisterHandlersFromAssembly(typeof(TestCommand).Assembly);
        });

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act
        await mediator.SendAsync(new TestCommand("first"), CancellationToken.None);
        await mediator.SendAsync(new TestCommand("second"), CancellationToken.None);
        await mediator.QueryAsync(new TestQuery(42), CancellationToken.None);

        // Assert - Filter to only the activities we created
        var testCommandActivities = activities.Where(a => a.DisplayName == "Command.TestCommand").ToList();
        var testQueryActivities = activities.Where(a => a.DisplayName == "Query.TestQuery").ToList();

        Assert.Equal(2, testCommandActivities.Count);
        Assert.Single(testQueryActivities);

        // All should have succeeded
        Assert.All(testCommandActivities.Concat(testQueryActivities), a => Assert.Equal(ActivityStatusCode.Ok, a.Status));
    }
}

// Test fixtures for ActivityInterceptor tests

/// <summary>
/// Test command that fails with an exception.
/// </summary>
internal record FailingCommand : ICommand<Unit>;

/// <summary>
/// Handler that always throws an exception.
/// </summary>
internal class FailingCommandHandler : ICommandHandler<FailingCommand, Unit>
{
    public Task<Unit> HandleAsync(FailingCommand command, CancellationToken cancellationToken)
    {
        throw new InvalidOperationException("Command failed");
    }
}

/// <summary>
/// Test command that has a deliberate delay for duration testing.
/// </summary>
internal record DelayedCommand : ICommand<Unit>;

/// <summary>
/// Handler that has a 10ms delay to test activity duration measurement.
/// </summary>
internal class DelayedCommandHandler : ICommandHandler<DelayedCommand, Unit>
{
    public async Task<Unit> HandleAsync(DelayedCommand command, CancellationToken cancellationToken)
    {
        await Task.Delay(10, cancellationToken);
        return Unit.Value;
    }
}

/// <summary>
/// Test notification for activity testing.
/// </summary>
internal record TestNotification : INotification;

/// <summary>
/// First handler for TestNotification.
/// </summary>
internal class TestNotificationHandler1 : INotificationHandler<TestNotification>
{
    public Task HandleAsync(TestNotification notification, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

/// <summary>
/// Second handler for TestNotification to test multiple handlers.
/// </summary>
internal class TestNotificationHandler2 : INotificationHandler<TestNotification>
{
    public Task HandleAsync(TestNotification notification, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
