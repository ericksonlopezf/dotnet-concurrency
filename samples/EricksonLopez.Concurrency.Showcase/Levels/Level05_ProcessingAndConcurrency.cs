// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.Concurrency.Abstractions;
using EricksonLopez.Concurrency.Controllers;
using EricksonLopez.Concurrency.Showcase.Models;

namespace EricksonLopez.Concurrency.Showcase.Levels;

/// <summary>
/// Provides demonstrations of in-memory Compare-And-Swap (CAS) state transitions and high-concurrency race condition simulations.
/// </summary>
public static class Level05_ProcessingAndConcurrency
{
    /// <summary>
    /// Executes the processing and concurrency demonstration.
    /// </summary>
    /// <remarks>
    /// Cookbook: Level 05 — Processing and Concurrency.
    /// Prerequisites: Level01-04.
    /// Concepts: Atomic CAS state transitions, non-reentrant per-entityId locking, race condition simulation.
    /// APIs: IConcurrencyController.ExecuteCasAsync(), CasResult&lt;T&gt;.IsSuccess/Entity/NewVersion/Conflict.
    /// Complexity: Intermediate.
    /// Next: Level06_ErrorHandlingAndClassification for DB error classification.
    /// </remarks>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task RunAsync()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n===============================================================================");
        Console.WriteLine(" LEVEL 05: PROCESSING & CONCURRENCY (ATOMIC COMPARE-AND-SWAP & RACE CONDITIONS)");
        Console.WriteLine("===============================================================================");
        Console.ResetColor();

        var controller = new ConcurrencyController();

        // -------------------------------------------------------------
        // CASE 1: Basic Execution of Compare-And-Swap (CAS)
        // -------------------------------------------------------------
        Console.WriteLine("\n--- Case 1: Successful Compare-And-Swap (CAS) ---");

        // ExecuteCasAsync provides in-memory mutual exclusion via a striped SemaphoreSlim.
        // IMPORTANT architectural constraint: it is NON-REENTRANT for the same entityId.
        // A call to ExecuteCasAsync(entityId: "X") while another call with the same entityId
        // is executing will block until the first completes, preventing lost updates.
        // Different entityIds execute concurrently without interference (stripe partitioning).

        var inventory = new ProductInventory("PROD-800", "High Performance GPU", availableStock: 20, reservedStock: 0, version: 1);
        Console.WriteLine($"Initial State: Available={inventory.AvailableStock}, Version={inventory.Version}");

        CasResult<ProductInventory> casSuccess = await controller.ExecuteCasAsync(
            entity: inventory,
            expected: ExpectedVersion.Specific(1),
            entityId: inventory.Id,
            mutate: (current, ct) =>
            {
                // Domain mutation
                current.AvailableStock -= 2;
                current.ReservedStock += 2;
                return ValueTask.FromResult(current);
            });

        if (casSuccess.IsSuccess)
        {
            if (casSuccess.NewVersion.HasValue)
            {
                inventory.Version = casSuccess.NewVersion.Value.Value;
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"CAS Succeeded! Mutated entity: Available={casSuccess.Entity!.AvailableStock}, Reserved={casSuccess.Entity.ReservedStock}, New Version={casSuccess.NewVersion}");
            Console.ResetColor();
        }

        // -------------------------------------------------------------
        // CASE 2: High Concurrency Simulation (Race Condition)
        // -------------------------------------------------------------
        Console.WriteLine("\n--- Case 2: Simulation of 10 Concurrent Parallel Requests ---");

        var sharedState = new ProductInventory("PROD-999", "Limited Edition Console", availableStock: 100, reservedStock: 0, version: 1);
        var expectedVersionForRacers = ExpectedVersion.Specific(1);

        int successfulCount = 0;
        int conflictCount = 0;
        var outcomes = new ConcurrentBag<string>();
        using var gate = new SemaphoreSlim(1, 1);

        // Spawn 10 concurrent tasks competing for the same expected version 1
        var tasks = Enumerable.Range(1, 10).Select(async taskId =>
        {
            await Task.Yield();

            await gate.WaitAsync();
            CasResult<ProductInventory> result;
            try
            {
                result = await controller.ExecuteCasAsync(
                    entity: sharedState,
                    expected: expectedVersionForRacers,
                    entityId: sharedState.Id,
                    mutate: (current, ct) =>
                    {
                        current.AvailableStock -= 1;
                        return ValueTask.FromResult(current);
                    });

                if (result.IsSuccess && result.NewVersion.HasValue)
                {
                    sharedState.Version = result.NewVersion.Value.Value;
                }
            }
            finally
            {
                gate.Release();
            }

            if (result.IsSuccess)
            {
                Interlocked.Increment(ref successfulCount);
                outcomes.Add($"[Task {taskId:D2}] -> SUCCESS (CAS applied, New Version: {result.NewVersion})");
            }
            else
            {
                Interlocked.Increment(ref conflictCount);
                outcomes.Add($"[Task {taskId:D2}] -> CONFLICT ({result.Conflict?.ConflictType}: {result.Conflict?.Message})");
            }
        });

        await Task.WhenAll(tasks);

        foreach (string outcome in outcomes.OrderBy(o => o))
        {
            Console.WriteLine(outcome);
        }

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"\nConcurrency Summary:");
        Console.WriteLine($"    - Successful Operations:               {successfulCount}");
        Console.WriteLine($"    - Prevented Conflicts (Lost Updates):  {conflictCount}");
        Console.ResetColor();
    }
}
