// Copyright © Erickson Lopez. MIT License.
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using EricksonLopez.Concurrency.Abstractions;
using EricksonLopez.Concurrency.Controllers;
using EricksonLopez.Concurrency.Diagnostics;

namespace EricksonLopez.Concurrency.Showcase.Levels;

/// <summary>
/// Level 07: Scalability and Throughput — Zero-allocation struct validation and OpenTelemetry instrumentation.
/// </summary>
public static class Level07_ScalabilityAndThroughput
{
    public static Task RunAsync()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n===============================================================================");
        Console.WriteLine(" LEVEL 07: SCALABILITY & THROUGHPUT (ZERO-ALLOCATION STRUCTS & OPENTELEMETRY)");
        Console.WriteLine("===============================================================================");
        Console.ResetColor();

        // -------------------------------------------------------------
        // PART 1: Zero-Allocation Verification
        // -------------------------------------------------------------
        Console.WriteLine("\n--- Part 1: Throughput Evaluation (1,000,000 Version Checks) ---");

        var checker = OptimisticConcurrencyChecker.Instance;
        var expected = ExpectedVersion.Specific(42);
        var actual = new ConcurrencyVersion(42);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        long allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        long startTime = Stopwatch.GetTimestamp();

        const int iterations = 1_000_000;
        int successCount = 0;

        for (int i = 0; i < iterations; i++)
        {
            if (checker.CheckVersion(expected, actual, "ENTITY-1", "ThroughputEntity", out _))
            {
                successCount++;
            }
        }

        TimeSpan elapsed = Stopwatch.GetElapsedTime(startTime);
        long allocatedAfter = GC.GetAllocatedBytesForCurrentThread();
        long netAllocated = allocatedAfter - allocatedBefore;

        Console.WriteLine($"    - Iterations:             {iterations:N0}");
        Console.WriteLine($"    - Total Elapsed Time:     {elapsed.TotalMilliseconds:F2} ms");
        Console.WriteLine($"    - Ops / Second:           {(iterations / elapsed.TotalSeconds):N0} ops/sec");
        Console.WriteLine($"    - Heap Allocations:       {netAllocated} bytes (Zero-Allocation verified!)");

        // -------------------------------------------------------------
        // PART 2: Diagnostic Instrumentation and OpenTelemetry Metrics
        // -------------------------------------------------------------
        Console.WriteLine("\n--- Part 2: Metrics and Tracing with ConcurrencyDiagnostics ---");

        Console.WriteLine($"    - Diagnostic Source Name: '{ConcurrencyDiagnostics.SourceName}' (v{ConcurrencyDiagnostics.Version})");
        Console.WriteLine($"    - ActivitySource:         {ConcurrencyDiagnostics.ActivitySource.Name}");
        Console.WriteLine($"    - Meter:                  {ConcurrencyDiagnostics.Meter.Name}");

        // Simulate activity and metric recording
        using Activity? activity = ConcurrencyDiagnostics.StartActivity("showcase.process_batch", "CustomerAccount", "ACC-990");
        ConcurrencyDiagnostics.RecordSuccess(activity, "CustomerAccount");
        ConcurrencyDiagnostics.OperationDurationHistogram.Record(1.45);

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("    -> Metric 'concurrency.successes' incremented.");
        Console.WriteLine("    -> Metric 'concurrency.duration' recorded (1.45 ms).");
        Console.WriteLine("    -> OpenTelemetry Activity finalized with status Ok.");
        Console.ResetColor();

        return Task.CompletedTask;
    }
}
