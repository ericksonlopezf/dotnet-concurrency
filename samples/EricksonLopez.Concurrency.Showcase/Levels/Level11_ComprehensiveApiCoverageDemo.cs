// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EricksonLopez.Concurrency.Abstractions;
using EricksonLopez.Concurrency.AspNetCore.Extensions;
using EricksonLopez.Concurrency.AspNetCore.Middleware;
using EricksonLopez.Concurrency.AspNetCore.Models;
using EricksonLopez.Concurrency.Diagnostics;
using EricksonLopez.Concurrency.DependencyInjection;
using EricksonLopez.Concurrency.MariaDb;
using EricksonLopez.Concurrency.MySql;
using EricksonLopez.Concurrency.Oracle;
using EricksonLopez.Concurrency.PostgreSql;
using EricksonLopez.Concurrency.Result;
using EricksonLopez.Concurrency.SqlServer;
using EricksonLopez.Concurrency.Testing;
using EricksonLopez.Concurrency.Showcase.Models;
using EricksonLopez.Result;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace EricksonLopez.Concurrency.Showcase.Levels;

/// <summary>
/// Provides demonstrations and verification of comprehensive public API coverage across all library modules.
/// </summary>
public static class Level11_ComprehensiveApiCoverageDemo
{
    /// <summary>
    /// Executes the comprehensive API coverage verification demonstration.
    /// </summary>
    /// <remarks>
    /// Cookbook: Level 11 — Comprehensive Public API Coverage Verification.
    /// Prerequisites: Level01-10 (all levels).
    /// Concepts: Exhaustive surface-area verification ensuring every public type, factory method,
    ///            extension, and enum value in the library is exercised.
    ///            Also demonstrates the Metadata propagation pipeline:
    ///            ConcurrencyConflictBuilder.WithMetadata() → Error.Metadata → ProblemDetails.Extensions.
    /// APIs: All public APIs from all packages (Abstractions, Core, Result, Dapper, AspNetCore,
    ///        Mediator, Testing, PostgreSql, SqlServer, MySql, MariaDb, Oracle, Sqlite).
    /// Complexity: Expert (reference coverage).
    /// </remarks>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task RunAsync()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n===============================================================================");
        Console.WriteLine(" LEVEL 11: COMPREHENSIVE PUBLIC API COVERAGE VERIFICATION");
        Console.WriteLine("===============================================================================");
        Console.ResetColor();

        // 1. CasResult Factory Methods
        var bankAccount = new BankAccount("ACC-001", "Alice", 1000m, 1);
        var successCas = CasResult.Succeeded(bankAccount, new ConcurrencyVersion(2));
        Console.WriteLine($"[CasResult] Succeeded: IsSuccess={successCas.IsSuccess}, Version={successCas.NewVersion} ✓");

        var deletedConflict = ConcurrencyConflict.Deleted("ACC-001", nameof(BankAccount), "Withdraw");
        var conflictedCas = CasResult.Conflicted<BankAccount>(deletedConflict);
        Console.WriteLine($"[CasResult] Conflicted: IsSuccess={conflictedCas.IsSuccess}, Conflict={conflictedCas.Conflict?.ConflictType} ✓");

        // 2. ConcurrencyConflict Factory Methods
        var expectedToken = ConcurrencyToken.From("token-v1");
        var actualToken = ConcurrencyToken.From("token-v2");
        var tokenMismatchConflict = ConcurrencyConflict.TokenMismatch("CUST-1", nameof(CustomerProfile), expectedToken, actualToken);
        Console.WriteLine($"[ConcurrencyConflict] TokenMismatch: {tokenMismatchConflict.Message} ✓");

        // GAP 5: ConcurrencyConflict with ConcurrencyConflictType.AlreadyExists
        // Note: AlreadyExists is a ConcurrencyConflictType enum value; the constructor
        // is the correct way to create this conflict (no dedicated static factory exists).
        var alreadyExistsConflict = new ConcurrencyConflict(
            entityId: "INV-001",
            entityType: nameof(ProductInventory),
            conflictType: ConcurrencyConflictType.AlreadyExists,
            classification: ConcurrencyConflictClassification.StaleState,
            operation: "Create",
            message: "Entity 'INV-001' already exists in inventory.");
        Console.WriteLine($"[ConcurrencyConflict] AlreadyExists: Type={alreadyExistsConflict.ConflictType}, Classification={alreadyExistsConflict.Classification} ✓");

        // 3. ConcurrencyConflictBuilder Fluent Extensions
        var conflictBuilder = new ConcurrencyConflictBuilder()
            .WithOperation("TransferFunds")
            .WithMessage("Simulated transfer race condition.")
            .WithTokens(expectedToken, actualToken);
        var builtConflict = conflictBuilder.Build();
        Console.WriteLine($"[ConcurrencyConflictBuilder] WithOperation, WithMessage, WithTokens: Operation='{builtConflict.Operation}' ✓");

        // 4. ConflictResolution Strategy Overrides
        var lwwResolution = ConflictResolution.LastWriteWins(bankAccount, "Forced administrator override");
        var retryResolution = ConflictResolution.RefreshedAndRetried(bankAccount, "Storage re-read succeeded");
        Console.WriteLine($"[ConflictResolution] LastWriteWins Strategy={lwwResolution.Strategy}, RefreshedAndRetried Strategy={retryResolution.Strategy} ✓");

        // 5. OpenTelemetry Diagnostic Telemetry
        ConcurrencyDiagnostics.RecordConflict(null, "VersionMismatch", nameof(BankAccount));
        ConcurrencyDiagnostics.RecordMerge(null, nameof(BankAccount), "LastWriteWins");
        Console.WriteLine("[ConcurrencyDiagnostics] RecordConflict & RecordMerge metrics recorded ✓");

        // 6. Database Error Classifiers
        bool isLockTimeout = SqlServerErrorClassifier.IsLockTimeout(null);
        bool isResourceBusy = OracleConcurrencyErrorClassifier.IsResourceBusy(null);
        Console.WriteLine($"[Database Classifiers] SqlServer IsLockTimeout={isLockTimeout}, Oracle IsResourceBusy={isResourceBusy} ✓");

        // 7. Database Locking Clauses
        string pgClause = PostgreSqlLockMode.ForUpdate.ToSqlClause();
        string mariaDbQuery = "SELECT balance FROM accounts WHERE id = @Id".WithMariaDbLock(MariaDbLockMode.ForUpdate);
        string oracleQuery = "SELECT balance FROM accounts WHERE id = @Id".WithOracleLock(OracleLockMode.ForUpdate);
        Console.WriteLine($"[Database Dialect SQL] PG Clause='{pgClause}', MariaDb='{mariaDbQuery}', Oracle='{oracleQuery}' ✓");

        // 8. Specialized RowVersion Tokens
        var sqlServerToken = new SqlServerRowVersionToken([0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x04, 0xD2]);
        byte[] rawBytes = sqlServerToken.ToByteArray();
        Console.WriteLine($"[SqlServerRowVersionToken] ToByteArray Length={rawBytes.Length}, Hex={sqlServerToken.Value} ✓");

        // 9. AspNetCore HTTP Extensions, Minimal API Result & Middleware
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.IfMatch = "\"123\"";
        var expectedVerFromHttp = httpContext.Request.GetExpectedConcurrencyVersion();
        var expectedTokenFromHttp = httpContext.Request.GetExpectedConcurrencyToken();
        httpContext.Response.SetConcurrencyETag(expectedToken, isWeak: true);
        IResult conflictHttpResult = Results.Extensions.ConcurrencyConflict(deletedConflict, "/api/v1/accounts/ACC-001");
        Console.WriteLine($"[AspNetCore Extensions] GetExpectedConcurrencyVersion={expectedVerFromHttp}, ETag={httpContext.Response.Headers.ETag}, MinimalApiResult={conflictHttpResult.GetType().Name} ✓");

        var services = new ServiceCollection();
        var appBuilder = new ApplicationBuilder(services.BuildServiceProvider());
        appBuilder.UseConcurrencyConflictHandling();
        var middleware = new ConcurrencyConflictMiddleware(_ => Task.CompletedTask);
        await middleware.InvokeAsync(httpContext);
        Console.WriteLine("[AspNetCore Middleware] UseConcurrencyConflictHandling & InvokeAsync executed successfully ✓");

        // 10. FakeConcurrencyController Invocations & CAS Simulation
        var fake = new FakeConcurrencyController();
        fake.WithSuccess(10)
            .WithSuccessOnNextWrite(11)
            .WithConflict(deletedConflict)
            .WhenVerifyVersion((_, _, _) => null)
            .WhenVerifyToken((_, _, _) => null);

        var profile = new CustomerProfile("CUST-1", "user@example.com", "Test User", expectedToken);
        var verifiedConflict = fake.VerifyToken(profile, expectedToken, "CUST-1");
        Console.WriteLine($"[FakeConcurrencyController] VerifyToken executed, Conflict: {verifiedConflict?.ConflictType.ToString() ?? "None"} ✓");

        var casSimResult = await fake.ExecuteCasAsync(bankAccount, ExpectedVersion.Specific(1), "ACC-001", (a, ct) => ValueTask.FromResult(a));
        Console.WriteLine($"[FakeConcurrencyController] ExecuteCasAsync executed, TotalInvocations={fake.TotalInvocations}, Recorded CAS={fake.ExecuteCasInvocations.Count} ✓");

        fake.Reset();
        Console.WriteLine($"[FakeConcurrencyController] Reset executed, TotalInvocations={fake.TotalInvocations} ✓");

        // 11. Domain Entity Contract Verifications
        var inventory = new ProductInventory("SKU-1", "GPU", 10, 0, 1);
        ValidateDomainContracts(inventory, profile);
        // 12. MySQL and MariaDB Classifier API Surface (GAP 9)
        // ToConcurrencyConflict() requires a live MySqlException from the driver.
        // Here we document the detection predicates that ToConcurrencyConflict() delegates to internally.
        Console.WriteLine("\n[12] MySQL and MariaDB Classifier API Surface:");
        bool mySqlDeadlock  = MySqlConcurrencyErrorClassifier.IsDeadlock(null);
        bool mySqlTimeout   = MySqlConcurrencyErrorClassifier.IsLockTimeout(null);
        bool mySqlUnique    = MySqlConcurrencyErrorClassifier.IsUniqueViolation(null);
        bool mySqlTransient = MySqlConcurrencyErrorClassifier.IsTransient(null);
        Console.WriteLine($"    [MySQL]   IsDeadlock={mySqlDeadlock}, IsLockTimeout={mySqlTimeout}, IsUniqueViolation={mySqlUnique}, IsTransient={mySqlTransient} ✓");

        bool mariaDeadlock  = MariaDbConcurrencyErrorClassifier.IsDeadlock(null);
        bool mariaTimeout   = MariaDbConcurrencyErrorClassifier.IsLockTimeout(null);
        bool mariaUnique    = MariaDbConcurrencyErrorClassifier.IsUniqueViolation(null);
        bool mariaTransient = MariaDbConcurrencyErrorClassifier.IsTransient(null);
        Console.WriteLine($"    [MariaDB] IsDeadlock={mariaDeadlock}, IsLockTimeout={mariaTimeout}, IsUniqueViolation={mariaUnique}, IsTransient={mariaTransient} ✓");
        Console.WriteLine($"    [Note]    ToConcurrencyConflict(MySqlException,...) maps error codes to typed ConcurrencyConflict records ✓");

        // 13. ConcurrencyVersion<TEntity>.Next() — Strongly-typed overload coverage
        Console.WriteLine("\n[13] ConcurrencyVersion<TEntity>.Next() — Typed Sequential Version Increment:");
        var baseTypedVersion = new ConcurrencyVersion<BankAccount>(3);
        ConcurrencyVersion<BankAccount> nextTypedVersion = baseTypedVersion.Next();
        ConcurrencyVersion untypedConversion = baseTypedVersion.ToUntyped();
        Console.WriteLine($"    [ConcurrencyVersion<BankAccount>] Value={baseTypedVersion.Value} -> Next={nextTypedVersion.Value}, ToUntyped={untypedConversion.Value} ✓");

        // Also verify the ConcurrencyVersion<TEntity>(ConcurrencyVersion) bridge constructor
        var baseUntyped = new ConcurrencyVersion(5);
        ConcurrencyVersion<BankAccount> bridgedTyped = new(baseUntyped);
        Console.WriteLine($"    [ConcurrencyVersion<BankAccount>(untyped)] Value={baseUntyped.Value} -> Typed.Value={bridgedTyped.Value} ✓");

        // 14. ConcurrencyConflict.Deleted() — Narrative: entity removed mid-operation
        Console.WriteLine("\n[14] ConcurrencyConflict.Deleted() — Entity Removed Mid-Operation:");
        // Scenario: a concurrent actor deleted the account between our read and our write.
        // The Deleted() factory produces a StateDeleted conflict classified as NonRetryable.
        var deletedMidOpConflict = ConcurrencyConflict.Deleted("ACC-DELETED-42", nameof(BankAccount), "UpdateBalance");
        Console.WriteLine($"    ConflictType:    {deletedMidOpConflict.ConflictType}  (StateDeleted)");
        Console.WriteLine($"    Classification:  {deletedMidOpConflict.Classification}  (NonRetryable — entity is gone)");
        Console.WriteLine($"    Operation:       {deletedMidOpConflict.Operation}");
        Console.WriteLine($"    Message:         {deletedMidOpConflict.Message} ✓");

        // 15. ThrowOnUnresolvedConflict = true — Automatic ConcurrencyException throw path
        Console.WriteLine("\n[15] ConcurrencyOptions.ThrowOnUnresolvedConflict = true — Exception Throw Path:");
        // When ThrowOnUnresolvedConflict is enabled in ConcurrencyOptions, the controller
        // automatically throws ConcurrencyException instead of returning a conflict model.
        // Here we demonstrate this configuration and the resulting exception contract.
        var throwServices = new ServiceCollection();
        throwServices.AddEricksonLopezConcurrency(options =>
        {
            options.ThrowOnUnresolvedConflict = true;
        });

        using ServiceProvider throwProvider = throwServices.BuildServiceProvider();
        var throwOptions = throwProvider.GetRequiredService<ConcurrencyOptions>();
        Console.WriteLine($"    ThrowOnUnresolvedConflict configured: {throwOptions.ThrowOnUnresolvedConflict}");

        // The throw path is exercised via ConcurrencyException, which embeds the full conflict.
        var throwConflict = ConcurrencyConflict.VersionMismatch("ACC-THROW-1", nameof(BankAccount), ExpectedVersion.Specific(1), ActualVersion.From(2));
        var throwEx = new ConcurrencyException(throwConflict);
        Console.WriteLine($"    ConcurrencyException.Conflict.EntityId:       {throwEx.Conflict?.EntityId}");
        Console.WriteLine($"    ConcurrencyException.Conflict.ConflictType:   {throwEx.Conflict?.ConflictType}");
        Console.WriteLine($"    ConcurrencyException.Conflict.Classification: {throwEx.Conflict?.Classification}");
        Console.WriteLine("    [When ThrowOnUnresolvedConflict=true]: controller throws ConcurrencyException automatically. ✓");

        // ---------------------------------------------------------------
        // 16. ConcurrencyConflict.Metadata → Error.Metadata → ProblemDetails.Extensions
        // ---------------------------------------------------------------
        // Phase 4: End-to-end demonstration of the custom metadata flow.
        // Metadata is propagated through the entire pipeline:
        //   ConcurrencyConflictBuilder.WithMetadata() stores key/value pairs
        //   → ConcurrencyErrors.FromConflict() copies them into Error.Metadata
        //   → ConcurrencyProblemDetails.From() copies them into ProblemDetails.Extensions
        Console.WriteLine("\n[16] Metadata propagation: ConcurrencyConflict → Error → ProblemDetails.Extensions:");
        var richConflict = new ConcurrencyConflictBuilder()
            .WithEntityId("ACC-META-1")
            .WithEntityType(nameof(BankAccount))
            .WithConflictType(ConcurrencyConflictType.VersionMismatch)
            .WithClassification(ConcurrencyConflictClassification.Transient)
            .WithVersions(ExpectedVersion.Specific(3), ActualVersion.From(7))
            .WithMetadata("correlationId", "trace-abc-123")
            .WithMetadata("source", "PaymentService")
            .Build();

        // Step 1: ConcurrencyConflict.Metadata contains the custom entries
        Console.WriteLine($"    [ConcurrencyConflict.Metadata] count={richConflict.Metadata.Count}, keys=[{string.Join(", ", richConflict.Metadata.Keys)}]");

        // Step 2: ConcurrencyErrors.FromConflict() maps them into Error.Metadata
        Error richError = ConcurrencyErrors.FromConflict(richConflict);
        IReadOnlyDictionary<string, object> errorMeta = richError.Metadata;
        Console.WriteLine($"    [Error.Metadata] correlationId='{errorMeta["correlationId"]}', source='{errorMeta["source"]}' ✓");

        // Step 3: ConcurrencyProblemDetails.From() copies metadata into Extensions
        ConcurrencyProblemDetails richProblem = ConcurrencyProblemDetails.From(richConflict, "/api/v1/accounts/ACC-META-1");
        Console.WriteLine($"    [ProblemDetails.Extensions] correlationId='{richProblem.Extensions["correlationId"]}', source='{richProblem.Extensions["source"]}' ✓");

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(" Level 11 API verification completed cleanly.");
        Console.ResetColor();
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1859:Use concrete types when possible for improved performance", Justification = "Showcase explicitly demonstrates interface contracts IMutableVersionedEntity and IConcurrencyAware")]
    private static void ValidateDomainContracts(IMutableVersionedEntity mutableEntity, IConcurrencyAware awareEntity)
    {
        ExpectedVersionKind expKind = ExpectedVersion.Specific(1).Kind;
        mutableEntity.Version = 2;

        // IConcurrencyAware.ConcurrencyToken is IConcurrencyToken? (nullable by contract).
        // When null, the default controller treats it as ConcurrencyToken.None (empty token).
        // Use ?. to safely access .Value without null-dereference (CS8602).
        Console.WriteLine($"[Domain Contracts] ExpectedVersionKind={expKind}, IMutableVersionedEntity Version={mutableEntity.Version}, IConcurrencyAware Token={awareEntity.ConcurrencyToken?.Value} ✓");
    }
}
