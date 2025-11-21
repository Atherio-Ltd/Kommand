using BenchmarkDotNet.Attributes;
using Kommand;
using Kommand.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Kommand.Benchmarks;

/// <summary>
/// Benchmarks comparing direct handler calls vs Kommand mediator overhead.
/// </summary>
/// <remarks>
/// Performance targets (from Architecture Doc Section 11):
/// - Direct method call: 1.0x (baseline)
/// - Kommand without interceptors: &lt;1.5x
/// - Kommand with 3 interceptors: &lt;2.0x
/// </remarks>
[MemoryDiagnoser]
[SimpleJob]
public class KommandBenchmarks
{
    private IServiceProvider _providerNoInterceptors = null!;
    private IServiceProvider _providerWith3Interceptors = null!;
    private IMediator _mediatorNoInterceptors = null!;
    private IMediator _mediatorWith3Interceptors = null!;
    private TestCommandHandler _directHandler = null!;
    private TestCommand _command = null!;

    [GlobalSetup]
    public void Setup()
    {
        _command = new TestCommand(42);

        // Setup 1: Direct handler (baseline)
        _directHandler = new TestCommandHandler();

        // Setup 2: Kommand WITHOUT interceptors
        var servicesNoInterceptors = new ServiceCollection();
        servicesNoInterceptors.AddKommand(config =>
        {
            config.RegisterHandlersFromAssembly(typeof(TestCommand).Assembly);
            // No interceptors added
        });
        _providerNoInterceptors = servicesNoInterceptors.BuildServiceProvider();
        _mediatorNoInterceptors = _providerNoInterceptors.GetRequiredService<IMediator>();

        // Setup 3: Kommand WITH 3 interceptors
        var servicesWith3Interceptors = new ServiceCollection();
        servicesWith3Interceptors.AddKommand(config =>
        {
            config.RegisterHandlersFromAssembly(typeof(TestCommand).Assembly);
            config.AddInterceptor(typeof(BenchmarkInterceptor1<,>));
            config.AddInterceptor(typeof(BenchmarkInterceptor2<,>));
            config.AddInterceptor(typeof(BenchmarkInterceptor3<,>));
        });
        _providerWith3Interceptors = servicesWith3Interceptors.BuildServiceProvider();
        _mediatorWith3Interceptors = _providerWith3Interceptors.GetRequiredService<IMediator>();
    }

    /// <summary>
    /// Baseline: Direct handler call without any mediator overhead.
    /// This represents the theoretical minimum time for the operation.
    /// </summary>
    [Benchmark(Baseline = true, Description = "Direct handler call (no mediator)")]
    public async Task<int> DirectHandlerCall()
    {
        return await _directHandler.HandleAsync(_command, CancellationToken.None);
    }

    /// <summary>
    /// Kommand mediator dispatch WITHOUT any interceptors.
    /// Measures only the mediator resolution and dispatch overhead.
    /// Target: &lt;1.5x baseline
    /// </summary>
    [Benchmark(Description = "Kommand without interceptors")]
    public async Task<int> KommandWithoutInterceptors()
    {
        return await _mediatorNoInterceptors.SendAsync(_command, CancellationToken.None);
    }

    /// <summary>
    /// Kommand mediator dispatch WITH 3 interceptors in the pipeline.
    /// Measures mediator + interceptor pipeline overhead.
    /// Target: &lt;2.0x baseline
    /// </summary>
    [Benchmark(Description = "Kommand with 3 interceptors")]
    public async Task<int> KommandWith3Interceptors()
    {
        return await _mediatorWith3Interceptors.SendAsync(_command, CancellationToken.None);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        (_providerNoInterceptors as IDisposable)?.Dispose();
        (_providerWith3Interceptors as IDisposable)?.Dispose();
    }
}

// ============================================================================
// Test Command and Handler
// ============================================================================

/// <summary>
/// Simple test command for benchmarking.
/// </summary>
public record TestCommand(int Value) : ICommand<int>;

/// <summary>
/// Test handler that performs a simple synchronous operation.
/// </summary>
public class TestCommandHandler : ICommandHandler<TestCommand, int>
{
    public Task<int> HandleAsync(TestCommand command, CancellationToken cancellationToken)
    {
        // Simple operation: multiply by 2
        return Task.FromResult(command.Value * 2);
    }
}

// ============================================================================
// Benchmark Interceptors (minimal overhead, for measuring pipeline cost)
// ============================================================================

/// <summary>
/// First benchmark interceptor - minimal passthrough logic.
/// </summary>
public class BenchmarkInterceptor1<TRequest, TResponse> : IInterceptor<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Minimal overhead - just pass through
        return await next();
    }
}

/// <summary>
/// Second benchmark interceptor - minimal passthrough logic.
/// </summary>
public class BenchmarkInterceptor2<TRequest, TResponse> : IInterceptor<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Minimal overhead - just pass through
        return await next();
    }
}

/// <summary>
/// Third benchmark interceptor - minimal passthrough logic.
/// </summary>
public class BenchmarkInterceptor3<TRequest, TResponse> : IInterceptor<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Minimal overhead - just pass through
        return await next();
    }
}
