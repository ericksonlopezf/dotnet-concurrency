// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading.Tasks;
using EricksonLopez.Concurrency.Abstractions;
using EricksonLopez.Concurrency.DependencyInjection;
using EricksonLopez.Concurrency.Showcase.Models;
using Microsoft.Extensions.DependencyInjection;

namespace EricksonLopez.Concurrency.Showcase.Levels;

/// <summary>
/// Level 01: Quick Start — Minimal DI setup, entity versioning, and initial version validation.
/// </summary>
public static class Level01_QuickStart
{
    public static Task RunAsync()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n===============================================================================");
        Console.WriteLine(" LEVEL 01: QUICK START (DEPENDENCY INJECTION & FIRST VERIFICATION)");
        Console.WriteLine("===============================================================================");
        Console.ResetColor();

        // 1. Configure Dependency Injection container
        var services = new ServiceCollection();
        services.AddEricksonLopezConcurrency();

        using ServiceProvider serviceProvider = services.BuildServiceProvider();

        // 2. Resolve the concurrency controller
        var controller = serviceProvider.GetRequiredService<IConcurrencyController>();

        // 3. Create a versioned entity
        var account = new BankAccount("ACC-1001", "Alice Smith", 5000.00m, version: 1);
        Console.WriteLine($"[1] Versioned Entity created: Account '{account.AccountId}', Owner '{account.Owner}', Current Version: {account.Version}");

        // 4. Success Case: Expecting version 1 (matches entity)
        ExpectedVersion expectedSuccess = ExpectedVersion.Specific(1);
        ConcurrencyConflict? conflictSuccess = controller.VerifyVersion(account, expectedSuccess, account.AccountId);

        if (conflictSuccess is null)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[2] Success: Verification against {expectedSuccess} succeeded with zero conflicts.");
            Console.ResetColor();
        }

        // 5. Conflict Case: Expecting version 2 (stale check against version 1)
        ExpectedVersion expectedConflict = ExpectedVersion.Specific(2);
        ConcurrencyConflict? conflictDetected = controller.VerifyVersion(account, expectedConflict, account.AccountId);

        if (conflictDetected is not null)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[3] Conflict Detected:");
            Console.WriteLine($"    - Conflict Type:  {conflictDetected.ConflictType}");
            Console.WriteLine($"    - Classification: {conflictDetected.Classification}");
            Console.WriteLine($"    - Message:        {conflictDetected.Message}");
            Console.WriteLine($"    - Timestamp (UTC):{conflictDetected.Timestamp:O}");
            Console.ResetColor();
        }

        return Task.CompletedTask;
    }
}
