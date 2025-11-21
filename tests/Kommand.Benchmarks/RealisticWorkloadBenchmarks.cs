using BenchmarkDotNet.Attributes;
using Kommand;
using Kommand.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Kommand.Benchmarks;

/// <summary>
/// Benchmarks comparing Kommand mediator overhead with realistic handler workloads.
/// </summary>
/// <remarks>
/// These benchmarks measure overhead as a percentage of total execution time,
/// using handlers that simulate real-world database and API operations.
///
/// Performance target: &lt;1% overhead for handlers taking ≥1ms
/// </remarks>
[MemoryDiagnoser]
[SimpleJob]
public class RealisticWorkloadBenchmarks
{
    private IServiceProvider _providerNoInterceptors = null!;
    private IServiceProvider _providerWith3Interceptors = null!;
    private IMediator _mediatorNoInterceptors = null!;
    private IMediator _mediatorWith3Interceptors = null!;
    private DatabaseCommandHandler _directDbHandler = null!;
    private ApiCommandHandler _directApiHandler = null!;
    private DatabaseCommand _dbCommand = null!;
    private ApiCommand _apiCommand = null!;

    [GlobalSetup]
    public void Setup()
    {
        _dbCommand = new DatabaseCommand(42);
        _apiCommand = new ApiCommand("test-data");

        // Setup 1: Direct handlers (baseline)
        _directDbHandler = new DatabaseCommandHandler();
        _directApiHandler = new ApiCommandHandler();

        // Setup 2: Kommand WITHOUT interceptors
        var servicesNoInterceptors = new ServiceCollection();
        servicesNoInterceptors.AddKommand(config =>
        {
            config.RegisterHandlersFromAssembly(typeof(DatabaseCommand).Assembly);
        });
        _providerNoInterceptors = servicesNoInterceptors.BuildServiceProvider();
        _mediatorNoInterceptors = _providerNoInterceptors.GetRequiredService<IMediator>();

        // Setup 3: Kommand WITH 3 interceptors
        var servicesWith3Interceptors = new ServiceCollection();
        servicesWith3Interceptors.AddKommand(config =>
        {
            config.RegisterHandlersFromAssembly(typeof(DatabaseCommand).Assembly);
            config.AddInterceptor(typeof(BenchmarkInterceptor1<,>));
            config.AddInterceptor(typeof(BenchmarkInterceptor2<,>));
            config.AddInterceptor(typeof(BenchmarkInterceptor3<,>));
        });
        _providerWith3Interceptors = servicesWith3Interceptors.BuildServiceProvider();
        _mediatorWith3Interceptors = _providerWith3Interceptors.GetRequiredService<IMediator>();
    }

    // ============================================================================
    // 1ms Database Operation Benchmarks
    // ============================================================================

    /// <summary>
    /// Baseline: Direct handler call with simulated 1ms database query.
    /// Represents a typical lightweight database operation.
    /// </summary>
    [Benchmark(Baseline = true, Description = "Direct 1ms DB query")]
    public async Task<int> DirectDatabaseQuery()
    {
        return await _directDbHandler.HandleAsync(_dbCommand, CancellationToken.None);
    }

    /// <summary>
    /// Kommand mediator with 1ms database operation (no interceptors).
    /// Expected overhead: &lt;0.1% (0.7μs / 1000μs)
    /// </summary>
    [Benchmark(Description = "Kommand 1ms DB query (no interceptors)")]
    public async Task<int> KommandDatabaseQueryNoInterceptors()
    {
        return await _mediatorNoInterceptors.SendAsync(_dbCommand, CancellationToken.None);
    }

    /// <summary>
    /// Kommand mediator with 1ms database operation (3 interceptors).
    /// Expected overhead: &lt;0.1% (0.9μs / 1000μs)
    /// </summary>
    [Benchmark(Description = "Kommand 1ms DB query (3 interceptors)")]
    public async Task<int> KommandDatabaseQueryWith3Interceptors()
    {
        return await _mediatorWith3Interceptors.SendAsync(_dbCommand, CancellationToken.None);
    }

    // ============================================================================
    // 10ms External API Benchmarks
    // ============================================================================

    /// <summary>
    /// Baseline: Direct handler call with simulated 10ms API call.
    /// Represents a typical external HTTP API request.
    /// </summary>
    [Benchmark(Description = "Direct 10ms API call")]
    public async Task<string> DirectApiCall()
    {
        return await _directApiHandler.HandleAsync(_apiCommand, CancellationToken.None);
    }

    /// <summary>
    /// Kommand mediator with 10ms API operation (no interceptors).
    /// Expected overhead: &lt;0.01% (0.7μs / 10000μs)
    /// </summary>
    [Benchmark(Description = "Kommand 10ms API call (no interceptors)")]
    public async Task<string> KommandApiCallNoInterceptors()
    {
        return await _mediatorNoInterceptors.SendAsync(_apiCommand, CancellationToken.None);
    }

    /// <summary>
    /// Kommand mediator with 10ms API operation (3 interceptors).
    /// Expected overhead: &lt;0.01% (0.9μs / 10000μs)
    /// </summary>
    [Benchmark(Description = "Kommand 10ms API call (3 interceptors)")]
    public async Task<string> KommandApiCallWith3Interceptors()
    {
        return await _mediatorWith3Interceptors.SendAsync(_apiCommand, CancellationToken.None);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        (_providerNoInterceptors as IDisposable)?.Dispose();
        (_providerWith3Interceptors as IDisposable)?.Dispose();
    }
}

// ============================================================================
// Realistic Test Commands and Handlers
// ============================================================================

/// <summary>
/// Command that simulates a database query operation.
/// </summary>
public record DatabaseCommand(int Id) : ICommand<int>;

/// <summary>
/// Handler that simulates a 1ms database query.
/// </summary>
public class DatabaseCommandHandler : ICommandHandler<DatabaseCommand, int>
{
    public async Task<int> HandleAsync(DatabaseCommand command, CancellationToken cancellationToken)
    {
        // Simulate database query latency (1ms)
        await Task.Delay(1, cancellationToken);

        // Perform some work (simulating data processing)
        var result = command.Id * 2 + 100;

        return result;
    }
}

/// <summary>
/// Command that simulates an external API call.
/// </summary>
public record ApiCommand(string Data) : ICommand<string>;

/// <summary>
/// Handler that simulates a 10ms external API call.
/// </summary>
public class ApiCommandHandler : ICommandHandler<ApiCommand, string>
{
    public async Task<string> HandleAsync(ApiCommand command, CancellationToken cancellationToken)
    {
        // Simulate external API call latency (10ms)
        await Task.Delay(10, cancellationToken);

        // Perform some work (simulating API response processing)
        var result = $"Processed: {command.Data.ToUpperInvariant()}";

        return result;
    }
}
