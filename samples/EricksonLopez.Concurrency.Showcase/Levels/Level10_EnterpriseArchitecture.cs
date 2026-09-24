// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.Concurrency.Abstractions;
using EricksonLopez.Concurrency.AspNetCore.Extensions;
using EricksonLopez.Concurrency.AspNetCore.Models;
using EricksonLopez.Concurrency.Controllers;
using EricksonLopez.Concurrency.Dapper;
using EricksonLopez.Concurrency.DependencyInjection;
using EricksonLopez.Concurrency.Mediator;
using EricksonLopez.Concurrency.Showcase.Models;
using EricksonLopez.Concurrency.Testing;
using EricksonLopez.Mediator;
using EricksonLopez.Result;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace EricksonLopez.Concurrency.Showcase.Levels;

/// <summary>
/// Provides demonstrations of enterprise architectural patterns including CQRS, mediator pipelines, multi-tenancy, and test doubles.
/// </summary>
public static class Level10_EnterpriseArchitecture
{
    /// <summary>
    /// Executes the enterprise architecture demonstration.
    /// </summary>
    /// <remarks>
    /// Cookbook: Level 10 — Enterprise Architecture.
    /// Prerequisites: Level01-09.
    /// Concepts: CQRS command dispatch, mediator pipeline behavior (observability only), multi-tenant SQL,
    ///            test doubles, RFC 7807 ProblemDetails, ADR-001 architectural demarcation.
    /// APIs: ConcurrencyBehavior&lt;TRequest,TResponse&gt;, IConcurrencyAwareRequest,
    ///        OptimisticUpdateBuilder (multi-tenant overload), FakeConcurrencyController,
    ///        ConcurrencyConflictBuilder, ConcurrencyProblemDetails.From(),
    ///        HttpRequest.GetExpectedConcurrencyToken()/GetExpectedConcurrencyVersion(),
    ///        HttpResponse.SetConcurrencyETag().
    /// Complexity: Expert.
    /// Next: Level11_ComprehensiveApiCoverageDemo for exhaustive API surface verification.
    /// </remarks>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task RunAsync()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n===============================================================================");
        Console.WriteLine(" LEVEL 10: ENTERPRISE ARCHITECTURE (CQRS, MEDIATOR, MULTI-TENANCY, TESTING & HTTP)");
        Console.WriteLine("===============================================================================");
        Console.ResetColor();

        // -------------------------------------------------------------
        // 1. Enterprise Dependency Injection Configuration
        // -------------------------------------------------------------
        var services = new ServiceCollection();
        services.AddEricksonLopezConcurrency();

        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        var controller = serviceProvider.GetRequiredService<IConcurrencyController>();

        var behavior = new ConcurrencyBehavior<TransferFundsCommand, Result<TransferResult>>();
        Console.WriteLine("[1] Pipeline Behavior ConcurrencyBehavior<TRequest, TResponse> instantiated with ActivitySource.");

        // -------------------------------------------------------------
        // 2. Command Dispatch with Matching Optimistic Version (Success)
        // -------------------------------------------------------------
        Console.WriteLine("\n[2] Processing TransferFundsCommand via ConcurrencyBehavior with ExpectedVersion=1:");

        var successCommand = new TransferFundsCommand(
            SourceAccountId: "ACC-5001",
            TargetAccountId: "ACC-5002",
            Amount: 250.00m,
            ExpectedVersion: ExpectedVersion.Specific(1));

        var successNext = new TransferFundsNext(successCommand, controller);
        Result<TransferResult> successResult = await behavior.Handle(successCommand, successNext, CancellationToken.None);

        if (successResult.IsSuccess)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"    -> Transfer Successful! TX: {successResult.Value.TransactionId}, Amount: {successResult.Value.Amount:C}, New Version: {successResult.Value.NewVersion}");
            Console.ResetColor();
        }

        // -------------------------------------------------------------
        // 3. Command Dispatch with Stale Optimistic Version (Conflict)
        // -------------------------------------------------------------
        Console.WriteLine("\n[3] Processing TransferFundsCommand with Stale ExpectedVersion=99 (Conflict):");

        var staleCommand = new TransferFundsCommand(
            SourceAccountId: "ACC-5001",
            TargetAccountId: "ACC-5002",
            Amount: 100.00m,
            ExpectedVersion: ExpectedVersion.Specific(99));

        var staleNext = new TransferFundsNext(staleCommand, controller);
        Result<TransferResult> staleResult = await behavior.Handle(staleCommand, staleNext, CancellationToken.None);

        if (staleResult.IsFailure)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"    -> Command rejected by Concurrency Pipeline Behavior!");
            Console.WriteLine($"    -> Error: [{staleResult.Error?.Code}] {staleResult.Error?.Description}");
            Console.ResetColor();
        }

        // -------------------------------------------------------------
        // 4. Multi-Tenancy Isolation with OptimisticUpdateBuilder
        // -------------------------------------------------------------
        Console.WriteLine("\n[4] Multi-Tenant Isolation via Dapper Optimistic Predicates:");

        string multiTenantSql = OptimisticUpdateBuilder.BuildVersionedUpdate(
            tableName: "tenant_accounts",
            setClauses: "balance = @Balance",
            idColumn: "id",
            versionColumn: "version",
            idParam: "Id",
            versionParam: "ExpectedVersion",
            tenantColumn: "tenant_id",
            tenantParam: "TenantId");

        Console.WriteLine($"    -> Generated SQL query:\n       {multiTenantSql}");

        // -------------------------------------------------------------
        // 5. Test Double Framework: FakeConcurrencyController & ConcurrencyConflictBuilder
        // -------------------------------------------------------------
        Console.WriteLine("\n[5] Unit Testing Harness (EricksonLopez.Concurrency.Testing):");

        var fakeController = new FakeConcurrencyController();

        // Build a simulated conflict with ConcurrencyConflictBuilder
        ConcurrencyConflict testConflict = new ConcurrencyConflictBuilder()
            .WithEntityId("ACC-TEST-99")
            .WithEntityType("BankAccount")
            .WithConflictType(ConcurrencyConflictType.VersionMismatch)
            .WithClassification(ConcurrencyConflictClassification.Transient)
            .WithVersions(ExpectedVersion.Specific(5), ActualVersion.From(6))
            .WithMetadata("testRunner", "ShowcaseUnitTester")
            .Build();

        fakeController.WithConflictOnNextWrite(testConflict);

        var testAccount = new BankAccount("ACC-TEST-99", "Test Owner", 500m, 5);
        ConcurrencyConflict? simulatedConflict = fakeController.VerifyVersion(testAccount, ExpectedVersion.Specific(5), testAccount.AccountId);

        Console.WriteLine($"    -> FakeConcurrencyController recorded invocation: Total={fakeController.TotalInvocations}");
        Console.WriteLine($"    -> VerifyVersionInvocations count: {fakeController.VerifyVersionInvocations.Count}");
        Console.WriteLine($"    -> Simulated conflict returned: Entity='{simulatedConflict?.EntityId}', Msg='{simulatedConflict?.Message}'");

        // GAP 4: WithSuccessOnNextWrite \u2014 queue-based explicit version on next CAS
        fakeController.Reset();
        fakeController.WithSuccessOnNextWrite(nextVersion: 42L);
        CasResult<BankAccount> queuedSuccessResult = await fakeController.ExecuteCasAsync(
            testAccount, ExpectedVersion.Specific(5), testAccount.AccountId,
            (a, ct) => ValueTask.FromResult(a));
        Console.WriteLine($"    -> WithSuccessOnNextWrite(42): IsSuccess={queuedSuccessResult.IsSuccess}, NewVersion={queuedSuccessResult.NewVersion}");

        // GAP 4: WithConflictOnNextWrite(type, entityId, entityType, classification) \u2014 synthesized overload
        fakeController.WithConflictOnNextWrite(
            ConcurrencyConflictType.VersionMismatch,
            entityId: "ACC-TEST-99",
            entityType: "BankAccount",
            classification: ConcurrencyConflictClassification.Transient);
        CasResult<BankAccount> queuedConflictResult = await fakeController.ExecuteCasAsync(
            testAccount, ExpectedVersion.Specific(5), testAccount.AccountId,
            (a, ct) => ValueTask.FromResult(a));
        Console.WriteLine($"    -> WithConflictOnNextWrite(type,...): IsSuccess={queuedConflictResult.IsSuccess}, Conflict={queuedConflictResult.Conflict?.ConflictType}");

        // -------------------------------------------------------------
        // 6. ASP.NET Core RFC 7807 ProblemDetails & HTTP Result Mapping
        // -------------------------------------------------------------
        Console.WriteLine("\n[6] ASP.NET Core Integration (RFC 7807 ProblemDetails & HTTP Status 409):");

        if (simulatedConflict is not null)
        {
            ConcurrencyProblemDetails problemDetails = ConcurrencyProblemDetails.From(simulatedConflict, "/api/v1/accounts/ACC-TEST-99");
            Console.WriteLine($"    -> ProblemDetails Status: {problemDetails.Status} Conflict (409)");
            Console.WriteLine($"    -> Title:                 {problemDetails.Title}");
            Console.WriteLine($"    -> Detail:                {problemDetails.Detail}");
            Console.WriteLine($"    -> ConflictType:          {problemDetails.ConflictType}");
            Console.WriteLine($"    -> Classification:        {problemDetails.Classification}");
            Console.WriteLine($"    -> Instance Path:         {problemDetails.Instance}");
        }

        // -------------------------------------------------------------
        // 7. Architectural Boundary Demarcation (ADR-001)
        // -------------------------------------------------------------
        Console.WriteLine("\n[7] Architectural Demarcation (ADR-001):");
        Console.WriteLine(@"    - Concurrency is responsible for detecting and classifying conflicts (e.g., Transient vs NonRetryable).
    - Resilience (or application orchestration policies) is responsible for managing backoff, jitter, and retries.
    - Zero circular coupling: Clean Separation of Concerns (SRP).");
    }
}
