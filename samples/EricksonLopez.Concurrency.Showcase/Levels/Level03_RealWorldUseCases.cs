// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading.Tasks;
using EricksonLopez.Concurrency.Abstractions;
using EricksonLopez.Concurrency.Controllers;
using EricksonLopez.Concurrency.Showcase.Models;

namespace EricksonLopez.Concurrency.Showcase.Levels;

/// <summary>
/// Level 03: Real-World Use Cases — Domain models, strongly-typed versions, ExpectedVersion kinds, ActualVersion, and ConcurrencyToken/ETags.
/// </summary>
public static class Level03_RealWorldUseCases
{
    public static Task RunAsync()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n===============================================================================");
        Console.WriteLine(" LEVEL 03: REAL-WORLD USE CASES (TYPED VERSIONS, ETags & EXPECTATION SEMANTICS)");
        Console.WriteLine("===============================================================================");
        Console.ResetColor();

        var checker = OptimisticConcurrencyChecker.Instance;

        // -------------------------------------------------------------
        // USE CASE 1: ExpectedVersion Semantics (New, Exists, Any, Specific)
        // -------------------------------------------------------------
        Console.WriteLine("\n--- Case 1: ExpectedVersion Semantics (Creation vs Mutation) ---");

        var newInventory = new ProductInventory("SKU-9901", "Gaming Laptop X1", 50, 0, version: 0);
        var existingInventory = new ProductInventory("SKU-9902", "Mechanical Keyboard Pro", 120, 10, version: 4);

        // A) Attempt to create an entity that must be new (ExpectedVersion.New -> expects version 0)
        bool isNewValid = checker.CheckVersion(
            ExpectedVersion.New,
            new ConcurrencyVersion(newInventory.Version),
            newInventory.Id,
            nameof(ProductInventory),
            out ConcurrencyConflict? newConflict);

        Console.WriteLine($"[A] Expecting NEW entity (Version=0) for 'SKU-9901' (Version={newInventory.Version}):");
        Console.WriteLine($"    - Outcome: {(isNewValid ? "VALID (Creation permitted)" : "CONFLICT")}");

        // B) Attempt to create as NEW an entity that already exists (Version=4) -> Conflict
        bool isExistingAsNewValid = checker.CheckVersion(
            ExpectedVersion.New,
            new ConcurrencyVersion(existingInventory.Version),
            existingInventory.Id,
            nameof(ProductInventory),
            out ConcurrencyConflict? existingAsNewConflict);

        Console.WriteLine($"[B] Expecting NEW entity for 'SKU-9902' (Version={existingInventory.Version}):");
        Console.WriteLine($"    - Outcome: {(isExistingAsNewValid ? "VALID" : "CONFLICT DETECTED")}");
        if (existingAsNewConflict is not null)
        {
            Console.WriteLine($"    - Detail:  {existingAsNewConflict.Message}");
        }

        // C) Mutation expectation: Exists (any version > 0)
        bool isExistsValid = checker.CheckVersion(
            ExpectedVersion.Exists,
            new ConcurrencyVersion(existingInventory.Version),
            existingInventory.Id,
            nameof(ProductInventory),
            out _);

        Console.WriteLine($"[C] Expecting entity to EXIST for 'SKU-9902': {(isExistsValid ? "VALID" : "CONFLICT")}");

        // D) Bypass expectation: Any (matches any version)
        bool isAnyValid = ExpectedVersion.Any.Matches(new ConcurrencyVersion(existingInventory.Version));
        Console.WriteLine($"[D] Expecting ANY version (bypassing optimistic checks): Matches={isAnyValid}");

        // -------------------------------------------------------------
        // USE CASE 2: Strongly-Typed Versions & Parsing
        // -------------------------------------------------------------
        Console.WriteLine("\n--- Case 2: Strongly-Typed Versions (ConcurrencyVersion<TEntity>) & Parsing ---");

        var bankAccount = new BankAccount("ACC-4001", "Grace Hopper", 10000.00m, version: 10);
        ConcurrencyVersion<BankAccount> typedVer = ((IVersionedEntity<BankAccount>)bankAccount).TypedVersion;
        ConcurrencyVersion<BankAccount> nextTypedVer = typedVer.Next();

        Console.WriteLine($"    - Entity: BankAccount '{bankAccount.AccountId}', Current TypedVersion: {typedVer.Value}");
        Console.WriteLine($"    - Next Sequential TypedVersion: {nextTypedVer.Value}");
        Console.WriteLine($"    - Untyped ConcurrencyVersion:   {typedVer.ToUntyped()}");

        if (ConcurrencyVersion.TryParse("42", out ConcurrencyVersion parsedVer))
        {
            Console.WriteLine($"    - Parsed ConcurrencyVersion from string '42': {parsedVer}");
        }

        ActualVersion actualFound = ActualVersion.From(10);
        ActualVersion actualMissing = ActualVersion.NotFound;
        Console.WriteLine($"    - ActualVersion (Exists): {actualFound}, ActualVersion (NotFound): {actualMissing}");

        // -------------------------------------------------------------
        // USE CASE 3: Opaque Concurrency Tokens / Binary & Guid ETags
        // -------------------------------------------------------------
        Console.WriteLine("\n--- Case 3: Opaque Concurrency Token / HTTP ETag Validation ---");

        var initialToken = ConcurrencyToken.NewGuid();
        var binaryToken = ConcurrencyToken.From(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x0A, 0xFF });
        var customer = new CustomerProfile("CUST-550", "bob@example.com", "Bob Johnson", initialToken);

        Console.WriteLine($"    - Customer created with Guid ETag: {customer.ConcurrencyToken}");
        Console.WriteLine($"    - Binary ConcurrencyToken (RowVersion): {binaryToken}");

        // Simulation: Client sends valid If-Match header matching the current token
        var clientHeaderToken = ConcurrencyToken.From(initialToken.Value, "Guid");
        bool tokenMatch = checker.CheckToken(
            clientHeaderToken,
            customer.ConcurrencyToken,
            customer.CustomerId,
            nameof(CustomerProfile),
            out ConcurrencyConflict? tokenConflict);

        Console.WriteLine($"[A] Request with valid If-Match ('{clientHeaderToken.Value}'):");
        Console.WriteLine($"    - Outcome: {(tokenMatch ? "AUTHORIZED (Token match)" : "412 PRECONDITION FAILED")}");

        // Simulation: Client sends a stale/invalid ETag
        var staleHeaderToken = ConcurrencyToken.From(Guid.NewGuid());
        bool staleTokenMatch = checker.CheckToken(
            staleHeaderToken,
            customer.ConcurrencyToken,
            customer.CustomerId,
            nameof(CustomerProfile),
            out ConcurrencyConflict? staleConflict);

        Console.WriteLine($"[B] Request with stale If-Match ('{staleHeaderToken.Value}'):");
        Console.WriteLine($"    - Outcome: {(staleTokenMatch ? "AUTHORIZED" : "412 PRECONDITION FAILED (Conflict detected)")}");
        if (staleConflict is not null)
        {
            Console.WriteLine($"    - Detail:  {staleConflict.Message}");
        }

        return Task.CompletedTask;
    }
}
