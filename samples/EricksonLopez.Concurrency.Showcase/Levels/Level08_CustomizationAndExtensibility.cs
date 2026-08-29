// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.Concurrency.Abstractions;
using EricksonLopez.Concurrency.Resolvers;
using EricksonLopez.Concurrency.Result;
using EricksonLopez.Concurrency.Showcase.Models;
using EricksonLopez.Result;

namespace EricksonLopez.Concurrency.Showcase.Levels;

/// <summary>
/// Level 08: Customization and Extensibility — Custom conflict resolvers, DelegateConflictResolver, RefreshAndRetryConflictResolver, and built-in resolution strategies.
/// </summary>
public static class Level08_CustomizationAndExtensibility
{
    public static async Task RunAsync()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n===============================================================================");
        Console.WriteLine(" LEVEL 08: CUSTOMIZATION & EXTENSIBILITY (ALL CONFLICT RESOLUTION STRATEGIES)");
        Console.WriteLine("===============================================================================");
        Console.ResetColor();

        var localProposed = new BankAccount("ACC-3001", "Emma Watson", 1200.00m, version: 2);
        var dbCurrent = new BankAccount("ACC-3001", "Emma Watson", 1500.00m, version: 3);

        var conflict = ConcurrencyConflict.VersionMismatch("ACC-3001", nameof(BankAccount), ExpectedVersion.Specific(2), ActualVersion.From(3));

        // -------------------------------------------------------------
        // 1. RejectConflictResolver (Default Strategy: Strict Rejection)
        // -------------------------------------------------------------
        Console.WriteLine("\n[1] RejectConflictResolver (Strict Rejection):");
        var rejectResolver = RejectConflictResolver<BankAccount>.Instance;
        ConflictResolution<BankAccount> rejectOutcome = await rejectResolver.ResolveAsync(localProposed, dbCurrent, conflict);

        Console.WriteLine($"    - IsResolved: {rejectOutcome.IsResolved}");
        Console.WriteLine($"    - Strategy:   {rejectOutcome.Strategy}");
        Console.WriteLine($"    - Reason:     {rejectOutcome.Reason}");

        Result<BankAccount> rejectResult = rejectOutcome.ToResult();
        Console.WriteLine($"    - Result.IsFailure mapping: {rejectResult.IsFailure}, Error: {rejectResult.Error?.Description}");

        // -------------------------------------------------------------
        // 2. DelegateConflictResolver (Domain Reconciliation via Merge)
        // -------------------------------------------------------------
        Console.WriteLine("\n[2] DelegateConflictResolver (Domain-Specific Merge):");

        var mergeResolver = new DelegateConflictResolver<BankAccount>((proposed, current, conf, ct) =>
        {
            if (current is null)
            {
                return ValueTask.FromResult(ConflictResolution.Rejected<BankAccount>("Entity no longer exists in storage"));
            }

            // Calculate deposit delta applied locally and add to current database balance
            decimal depositDelta = proposed.Balance - 1000.00m;
            var reconciled = new BankAccount(proposed.AccountId, proposed.Owner, current.Balance + depositDelta, current.Version + 1);

            return ValueTask.FromResult(ConflictResolution.Merged(reconciled, "Incremental deposit reconciled with updated database balance."));
        });

        ConflictResolution<BankAccount> mergeOutcome = await mergeResolver.ResolveAsync(localProposed, dbCurrent, conflict);
        Console.WriteLine($"    - IsResolved:       {mergeOutcome.IsResolved}");
        Console.WriteLine($"    - Strategy:         {mergeOutcome.Strategy}");
        Console.WriteLine($"    - Resolved Balance: {mergeOutcome.ResolvedEntity?.Balance:C}");
        Console.WriteLine($"    - Reason:           {mergeOutcome.Reason}");

        // -------------------------------------------------------------
        // 3. LastWriteWinsConflictResolver (Explicit Opt-In LWW Overwrite)
        // -------------------------------------------------------------
        Console.WriteLine("\n[3] LastWriteWinsConflictResolver (Opt-in Explicit LWW Overwrite):");
        var lwwResolver = LastWriteWinsConflictResolver<BankAccount>.Instance;
        ConflictResolution<BankAccount> lwwOutcome = await lwwResolver.ResolveAsync(localProposed, dbCurrent, conflict);

        Console.WriteLine($"    - IsResolved:       {lwwOutcome.IsResolved}");
        Console.WriteLine($"    - Strategy:         {lwwOutcome.Strategy}");
        Console.WriteLine($"    - Applied State:    Balance={lwwOutcome.ResolvedEntity?.Balance:C}");

        // -------------------------------------------------------------
        // 4. RefreshAndRetryConflictResolver (Reload from persistence & reapply)
        // -------------------------------------------------------------
        Console.WriteLine("\n[4] RefreshAndRetryConflictResolver (Reload latest state & re-apply):");

        var refreshRetryResolver = new RefreshAndRetryConflictResolver<BankAccount>(
            refreshDelegate: (entityId, ct) =>
            {
                // Simulating database reload returning fresh state (Version = 4, Balance = $1,750)
                var freshFromDb = new BankAccount(entityId, "Emma Watson", 1750.00m, version: 4);
                return ValueTask.FromResult<BankAccount?>(freshFromDb);
            },
            reapplyDelegate: (proposed, fresh) =>
            {
                // Re-apply local deposit delta ($200) onto the freshly loaded balance ($1,750 -> $1,950)
                decimal depositDelta = proposed.Balance - 1000.00m;
                return new BankAccount(fresh.AccountId, fresh.Owner, fresh.Balance + depositDelta, fresh.Version + 1);
            },
            maxRetries: 3);

        ConflictResolution<BankAccount> refreshOutcome = await refreshRetryResolver.ResolveAsync(localProposed, null, conflict);
        Console.WriteLine($"    - IsResolved:       {refreshOutcome.IsResolved}");
        Console.WriteLine($"    - Strategy:         {refreshOutcome.Strategy}");
        Console.WriteLine($"    - Reconciled State: Balance={refreshOutcome.ResolvedEntity?.Balance:C}, Version={refreshOutcome.ResolvedEntity?.Version}");
        Console.WriteLine($"    - Reason:           {refreshOutcome.Reason}");
    }
}
