namespace Kommand.Tests.Unit;

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using Kommand;
using Kommand.Abstractions;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Unit tests for MetricsInterceptor to verify OpenTelemetry metrics collection.
/// Tests zero-config pattern, metric recording, and proper tagging.
/// </summary>
/// <remarks>
/// NOTE: Full metrics validation requires MeterListener setup which is complex.
/// These tests verify that metrics collection does not throw exceptions (smoke tests)
/// and validate basic functionality. In production, metrics would be validated via
/// observability platforms like Prometheus or Application Insights.
/// </remarks>
public class MetricsInterceptorTests
{
    /// <summary>
    /// Verifies that metrics collection works without throwing exceptions when OTEL is not configured.
    /// This is a smoke test to ensure the zero-config pattern works correctly.
    /// </summary>
    [Fact]
    public async Task WhenOTELNotConfigured_ShouldNotThrow()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddKommand(config =>
        {
            config.RegisterHandlersFromAssembly(typeof(TestCommand).Assembly);
        });

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act & Assert - Should complete without throwing
        var result = await mediator.SendAsync(new TestCommand("test"), CancellationToken.None);
        Assert.Equal("test-handled", result);
    }

    /// <summary>
    /// Verifies that the request counter is incremented for successful requests.
    /// Uses MeterListener to capture metrics data.
    /// </summary>
    [Fact]
    public async Task OnSuccess_ShouldIncrementRequestCounter()
    {
        // Arrange
        var measurements = new List<(string InstrumentName, long Value, KeyValuePair<string, object?>[] Tags)>();

        using var listener = new MeterListener
        {
            InstrumentPublished = (instrument, meterListener) =>
            {
                if (instrument.Meter.Name == "Kommand" && instrument.Name == "kommand.requests")
                {
                    meterListener.EnableMeasurementEvents(instrument);
                }
            }
        };

        listener.SetMeasurementEventCallback<long>((instrument, measurement, tags, state) =>
        {
            measurements.Add((instrument.Name, measurement, tags.ToArray()));
        });

        listener.Start();

        var services = new ServiceCollection();
        services.AddKommand(config =>
        {
            config.RegisterHandlersFromAssembly(typeof(TestCommand).Assembly);
        });

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act
        await mediator.SendAsync(new TestCommand("test"), CancellationToken.None);

        // Assert - Should have recorded at least one measurement
        var requestMeasurements = measurements.Where(m => m.InstrumentName == "kommand.requests").ToList();
        Assert.NotEmpty(requestMeasurements);

        // Verify the measurement value is 1 (one request)
        Assert.All(requestMeasurements, m => Assert.Equal(1L, m.Value));
    }

    /// <summary>
    /// Verifies that the failure counter is incremented when a handler throws an exception.
    /// </summary>
    [Fact]
    public async Task OnException_ShouldIncrementFailureCounter()
    {
        // Arrange
        var measurements = new List<(string InstrumentName, long Value)>();

        using var listener = new MeterListener
        {
            InstrumentPublished = (instrument, meterListener) =>
            {
                if (instrument.Meter.Name == "Kommand" && instrument.Name == "kommand.requests.failed")
                {
                    meterListener.EnableMeasurementEvents(instrument);
                }
            }
        };

        listener.SetMeasurementEventCallback<long>((instrument, measurement, tags, state) =>
        {
            measurements.Add((instrument.Name, measurement));
        });

        listener.Start();

        var services = new ServiceCollection();
        services.AddKommand(config =>
        {
            config.RegisterHandlersFromAssembly(typeof(FailingCommand).Assembly);
        });

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act & Assert - Command should throw
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await mediator.SendAsync(new FailingCommand(), CancellationToken.None));

        // Verify failure counter was incremented
        var failureMeasurements = measurements.Where(m => m.InstrumentName == "kommand.requests.failed").ToList();
        Assert.NotEmpty(failureMeasurements);
        Assert.All(failureMeasurements, m => Assert.Equal(1L, m.Value));
    }

    /// <summary>
    /// Verifies that request duration is recorded in the histogram.
    /// </summary>
    [Fact]
    public async Task ShouldRecordDuration()
    {
        // Arrange
        var measurements = new List<(string InstrumentName, double Value)>();

        using var listener = new MeterListener
        {
            InstrumentPublished = (instrument, meterListener) =>
            {
                if (instrument.Meter.Name == "Kommand" && instrument.Name == "kommand.request.duration")
                {
                    meterListener.EnableMeasurementEvents(instrument);
                }
            }
        };

        listener.SetMeasurementEventCallback<double>((instrument, measurement, tags, state) =>
        {
            measurements.Add((instrument.Name, measurement));
        });

        listener.Start();

        var services = new ServiceCollection();
        services.AddKommand(config =>
        {
            config.RegisterHandlersFromAssembly(typeof(DelayedCommand).Assembly);
        });

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act - DelayedCommand has a 10ms delay
        await mediator.SendAsync(new DelayedCommand(), CancellationToken.None);

        // Assert - Duration should be recorded
        var durationMeasurements = measurements.Where(m => m.InstrumentName == "kommand.request.duration").ToList();
        Assert.NotEmpty(durationMeasurements);

        // Duration should be at least a few milliseconds (accounting for the 10ms delay)
        Assert.All(durationMeasurements, m => Assert.True(m.Value > 0,
            $"Expected duration > 0ms, got {m.Value}ms"));
    }

    /// <summary>
    /// Verifies that metrics include correct tags (dimensions) for filtering and aggregation.
    /// </summary>
    [Fact]
    public async Task ShouldIncludeCorrectTags()
    {
        // Arrange - Use concurrent collection to avoid threading issues
        var tagsList = new System.Collections.Concurrent.ConcurrentBag<KeyValuePair<string, object?>[]>();

        using var listener = new MeterListener
        {
            InstrumentPublished = (instrument, meterListener) =>
            {
                if (instrument.Meter.Name == "Kommand" && instrument.Name == "kommand.requests")
                {
                    meterListener.EnableMeasurementEvents(instrument);
                }
            }
        };

        listener.SetMeasurementEventCallback<long>((instrument, measurement, tags, state) =>
        {
            tagsList.Add(tags.ToArray());
        });

        listener.Start();

        var services = new ServiceCollection();
        services.AddKommand(config =>
        {
            config.RegisterHandlersFromAssembly(typeof(TestCommand).Assembly);
        });

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act
        await mediator.SendAsync(new TestCommand("test"), CancellationToken.None);

        // Assert - Filter to only TestCommand measurements (may capture other tests' metrics)
        var testCommandTags = tagsList
            .Select(tags => tags.ToDictionary(kvp => kvp.Key, kvp => kvp.Value))
            .Where(tagDict => tagDict.GetValueOrDefault("kommand.request.name")?.ToString() == "TestCommand")
            .ToList();

        Assert.NotEmpty(testCommandTags);

        // Verify first matching measurement has all required tags
        var tagDict = testCommandTags.First();
        Assert.True(tagDict.ContainsKey("kommand.request.type"));
        Assert.True(tagDict.ContainsKey("kommand.request.name"));
        Assert.True(tagDict.ContainsKey("kommand.response.type"));

        // Verify tag values
        Assert.Equal("Command", tagDict["kommand.request.type"]);
        Assert.Equal("TestCommand", tagDict["kommand.request.name"]);
        Assert.Equal("String", tagDict["kommand.response.type"]);
    }

    /// <summary>
    /// Verifies that multiple requests accumulate in the counter (counter is additive).
    /// </summary>
    [Fact]
    public async Task MultipleRequests_ShouldAccumulateMetrics()
    {
        // Arrange
        var measurementCount = 0;

        using var listener = new MeterListener
        {
            InstrumentPublished = (instrument, meterListener) =>
            {
                if (instrument.Meter.Name == "Kommand" && instrument.Name == "kommand.requests")
                {
                    meterListener.EnableMeasurementEvents(instrument);
                }
            }
        };

        listener.SetMeasurementEventCallback<long>((instrument, measurement, tags, state) =>
        {
            Interlocked.Increment(ref measurementCount);
        });

        listener.Start();

        var services = new ServiceCollection();
        services.AddKommand(config =>
        {
            config.RegisterHandlersFromAssembly(typeof(TestCommand).Assembly);
        });

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act - Execute 5 commands
        for (int i = 0; i < 5; i++)
        {
            await mediator.SendAsync(new TestCommand($"test-{i}"), CancellationToken.None);
        }

        // Assert - Should have at least 5 measurements (may capture others from concurrent tests)
        Assert.True(measurementCount >= 5, $"Expected at least 5 measurements, got {measurementCount}");
    }

    /// <summary>
    /// Verifies that commands and queries are tagged differently to allow separate metrics.
    /// </summary>
    [Fact]
    public async Task CommandVsQuery_ShouldTagCorrectly()
    {
        // Arrange
        var requestTypes = new List<string>();

        using var listener = new MeterListener
        {
            InstrumentPublished = (instrument, meterListener) =>
            {
                if (instrument.Meter.Name == "Kommand" && instrument.Name == "kommand.requests")
                {
                    meterListener.EnableMeasurementEvents(instrument);
                }
            }
        };

        listener.SetMeasurementEventCallback<long>((instrument, measurement, tags, state) =>
        {
            var tagDict = tags.ToArray().ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            if (tagDict.TryGetValue("kommand.request.type", out var requestType))
            {
                requestTypes.Add(requestType?.ToString() ?? "");
            }
        });

        listener.Start();

        var services = new ServiceCollection();
        services.AddKommand(config =>
        {
            config.RegisterHandlersFromAssembly(typeof(TestCommand).Assembly);
        });

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act
        await mediator.SendAsync(new TestCommand("test"), CancellationToken.None);
        await mediator.QueryAsync(new TestQuery(42), CancellationToken.None);

        // Assert - Should have recorded both Command and Query (may capture others from concurrent tests)
        Assert.Contains("Command", requestTypes);
        Assert.Contains("Query", requestTypes);
        Assert.True(requestTypes.Count >= 2, $"Expected at least 2 request types, got {requestTypes.Count}");
    }

    /// <summary>
    /// Verifies that duration histogram includes the success flag tag.
    /// </summary>
    [Fact]
    public async Task DurationHistogram_ShouldIncludeSuccessFlag()
    {
        // Arrange
        var successFlags = new List<string>();

        using var listener = new MeterListener
        {
            InstrumentPublished = (instrument, meterListener) =>
            {
                if (instrument.Meter.Name == "Kommand" && instrument.Name == "kommand.request.duration")
                {
                    meterListener.EnableMeasurementEvents(instrument);
                }
            }
        };

        listener.SetMeasurementEventCallback<double>((instrument, measurement, tags, state) =>
        {
            var tagDict = tags.ToArray().ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            if (tagDict.TryGetValue("kommand.success", out var success))
            {
                successFlags.Add(success?.ToString() ?? "");
            }
        });

        listener.Start();

        var services = new ServiceCollection();
        services.AddKommand(config =>
        {
            config.RegisterHandlersFromAssembly(typeof(TestCommand).Assembly);
            config.RegisterHandlersFromAssembly(typeof(FailingCommand).Assembly);
        });

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act - One success, one failure
        await mediator.SendAsync(new TestCommand("test"), CancellationToken.None);

        try
        {
            await mediator.SendAsync(new FailingCommand(), CancellationToken.None);
        }
        catch (InvalidOperationException)
        {
            // Expected
        }

        // Assert - Should have both true and false success flags
        // Note: May capture metrics from other tests running concurrently, so check contains rather than exact count
        Assert.Contains("true", successFlags);
        Assert.Contains("false", successFlags);
        Assert.True(successFlags.Count >= 2, $"Expected at least 2 success flags, got {successFlags.Count}");
    }
}
