using BenchmarkDotNet.Running;
using Kommand.Benchmarks;

Console.WriteLine("╔═══════════════════════════════════════════════════════════╗");
Console.WriteLine("║   Kommand Performance Benchmarks                          ║");
Console.WriteLine("║   Comparing mediator overhead vs direct handler calls    ║");
Console.WriteLine("╚═══════════════════════════════════════════════════════════╝");
Console.WriteLine();

// Check for command-line arguments to select benchmark suite
var cmdArgs = Environment.GetCommandLineArgs();
if (cmdArgs.Length > 1 && cmdArgs[1] == "--realistic")
{
    Console.WriteLine("Running REALISTIC WORKLOAD benchmarks (1ms DB, 10ms API)...");
    Console.WriteLine("These measure overhead percentage for production-like scenarios.");
    Console.WriteLine();

    var realisticSummary = BenchmarkRunner.Run<RealisticWorkloadBenchmarks>();

    Console.WriteLine();
    Console.WriteLine("════════════════════════════════════════════════════════════");
    Console.WriteLine("Performance Targets (Realistic Workloads):");
    Console.WriteLine("════════════════════════════════════════════════════════════");
    Console.WriteLine("• 1ms database query:  <0.1% overhead");
    Console.WriteLine("• 10ms API call:       <0.01% overhead");
    Console.WriteLine("════════════════════════════════════════════════════════════");
}
else
{
    Console.WriteLine("Running MICROBENCHMARKS (trivial handler operation)...");
    Console.WriteLine("These measure absolute mediator dispatch overhead.");
    Console.WriteLine("Use --realistic flag for production-like workload tests.");
    Console.WriteLine();

    var summary = BenchmarkRunner.Run<KommandBenchmarks>();

    Console.WriteLine();
    Console.WriteLine("════════════════════════════════════════════════════════════");
    Console.WriteLine("Absolute Performance:");
    Console.WriteLine("════════════════════════════════════════════════════════════");
    Console.WriteLine("• Mediator overhead (no interceptors):  ~685 ns");
    Console.WriteLine("• Mediator overhead (3 interceptors):   ~915 ns");
    Console.WriteLine("• Per-interceptor cost:                 ~74 ns");
    Console.WriteLine("════════════════════════════════════════════════════════════");
}

Console.WriteLine();
Console.WriteLine("Results saved to: BenchmarkDotNet.Artifacts/results");
Console.WriteLine();
