# Kommand Performance Benchmarks

This project uses [BenchmarkDotNet](https://benchmarkdotnet.org/) to measure the performance overhead of the Kommand mediator library.

## Two Benchmark Suites

### 1. Microbenchmarks (Default)

Measures **absolute mediator dispatch overhead** using trivial handler operations.

**What it measures:**
- Exact nanosecond cost of DI resolution, reflection, and pipeline construction
- Baseline handler performs simple multiplication (`value * 2`)
- Useful for detecting performance regressions in mediator infrastructure

**Typical results:**
- Mediator overhead (no interceptors): ~685 ns (0.685 microseconds)
- Mediator overhead (3 interceptors): ~915 ns (0.915 microseconds)
- Per-interceptor cost: ~74 ns

### 2. Realistic Workload Benchmarks

Measures **overhead percentage** for production-like handler operations.

**What it measures:**
- Overhead percentage for handlers with real I/O operations
- Simulates 1ms database query and 10ms external API call
- Demonstrates negligible overhead in realistic scenarios

**Typical results:**
- 1ms database query: <0.1% overhead
- 10ms API call: <0.01% overhead

## How to Run

### Microbenchmarks (Absolute Overhead)

```bash
cd tests/Kommand.Benchmarks
dotnet run -c Release
```

### Realistic Workloads (Percentage Overhead)

```bash
cd tests/Kommand.Benchmarks
dotnet run -c Release -- --realistic
```

**Important**: Always run benchmarks in **Release** mode, never Debug.

### Understanding the Output

BenchmarkDotNet will output:

- **Mean**: Average execution time
- **Error**: Half of 99.9% confidence interval
- **StdDev**: Standard deviation of all measurements
- **Ratio**: Compared to baseline (1.00 = same speed as baseline)
- **Allocated**: Memory allocated per operation

Example output:

```
| Method                          | Mean     | Ratio | Allocated |
|-------------------------------- |---------:|------:|----------:|
| DirectHandlerCall               | 10.50 ns |  1.00 |         - |
| KommandWithoutInterceptors      | 14.25 ns |  1.36 |      32 B |
| KommandWith3Interceptors        | 18.75 ns |  1.79 |      96 B |
```

### Interpreting Results

#### Microbenchmarks

**Absolute overhead** (typical values):
- Mediator dispatch: ~685 ns (0.685 microseconds)
- 3 interceptors pipeline: ~915 ns (0.915 microseconds)
- Per-interceptor cost: ~74 ns each

**Note:** High ratio values (80-110x) are **expected** because the baseline handler is trivial (8 ns). The absolute overhead of <1 microsecond is what matters for production use.

✅ **Good indicators:**
- Consistent sub-microsecond overhead
- Per-interceptor cost remains ~70-80 ns
- No unexpected memory allocations

❌ **Performance regressions to investigate:**
- Mediator overhead >2 microseconds
- Per-interceptor cost >200 ns
- Significant increase in memory allocations

#### Realistic Workload Benchmarks

**Overhead percentage** (expected values):
- 1ms database query: <0.1% overhead
- 10ms API call: <0.01% overhead

✅ **Production-ready:**
- Overhead <0.1% for operations ≥1ms
- Negligible impact on request latency
- No measurable effect on throughput

❌ **Needs investigation:**
- Overhead >1% for realistic workloads
- Ratio approaching 1.1x for 10ms operations

## Performance Targets

### Microbenchmarks (Absolute Overhead)

| Metric                           | Target        | Notes                           |
|----------------------------------|---------------|---------------------------------|
| Mediator dispatch overhead       | <2 μs         | DI resolution + reflection      |
| Per-interceptor cost             | <100 ns       | Pipeline delegate invocation    |
| Total (3 interceptors)           | <3 μs         | End-to-end overhead             |

### Realistic Workloads (Percentage Overhead)

| Scenario                         | Target        | Notes                           |
|----------------------------------|---------------|---------------------------------|
| 1ms database operation           | <0.1%         | Typical lightweight query       |
| 10ms external API call           | <0.01%        | Typical HTTP request            |
| 100ms+ long-running operation    | <0.001%       | Completely negligible           |

## Benchmark Artifacts

Results are saved in `BenchmarkDotNet.Artifacts/`:

- `results/` - Detailed HTML and CSV reports
- `logs/` - Execution logs
- `artifacts/` - Compiled benchmarks

These files are ignored by git (see .gitignore).

## Advanced Usage

### Run Specific Benchmark

```bash
dotnet run -c Release -- --filter *KommandWithoutInterceptors*
```

### Export Results

```bash
dotnet run -c Release -- --exporters html,csv,markdown
```

### Memory Profiler

```bash
dotnet run -c Release -- --memory
```

## What Makes These Benchmarks Reliable?

- **MemoryDiagnoser**: Tracks memory allocations per operation
- **Multiple Iterations**: BenchmarkDotNet runs warmup + actual iterations
- **Statistical Analysis**: Calculates mean, standard deviation, and confidence intervals
- **JIT Compilation**: Accounts for JIT warmup overhead
- **GC Pressure**: Measures and reports garbage collection impact

## Notes

- Benchmarks use minimal "passthrough" interceptors to isolate pipeline overhead
- Real-world interceptors (logging, validation, etc.) will have additional cost
- Results vary based on hardware - targets are ratios, not absolute times
- Always run benchmarks on clean, idle machine for consistent results

## Troubleshooting

### "Benchmark was not executed"
- Ensure you're running in **Release** mode
- Check that the project builds without errors

### Inconsistent Results
- Close other applications to reduce CPU load
- Run benchmarks multiple times and compare
- Disable CPU frequency scaling if available

### Build Errors
- Ensure Kommand project builds successfully
- Restore NuGet packages: `dotnet restore`

## References

- [BenchmarkDotNet Documentation](https://benchmarkdotnet.org/articles/overview.html)
- [Kommand Architecture Document](../../MEDIATOR_ARCHITECTURE_PLAN.md) - Section 11 (Performance)
- [.NET Performance Best Practices](https://learn.microsoft.com/en-us/dotnet/core/performance/)
