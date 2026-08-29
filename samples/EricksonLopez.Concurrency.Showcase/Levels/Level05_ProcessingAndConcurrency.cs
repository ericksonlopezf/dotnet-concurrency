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
/// Level 05: Processing and High Concurrency — In-Memory Compare-And-Swap (CAS) and race-condition stress simulations.
/// </summary>
public static class Level05_ProcessingAndConcurrency
{
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
