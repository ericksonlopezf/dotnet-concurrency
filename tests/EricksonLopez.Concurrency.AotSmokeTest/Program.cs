// Copyright © Erickson Lopez. MIT License.
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using EricksonLopez.Concurrency.Abstractions;
using EricksonLopez.Concurrency.Controllers;
using EricksonLopez.Concurrency.Diagnostics;
using EricksonLopez.Concurrency.NativeAotTests;
using EricksonLopez.Concurrency.Oracle;
using EricksonLopez.Concurrency.PostgreSql;
using EricksonLopez.Concurrency.Resolvers;
using EricksonLopez.Concurrency.Result;
using EricksonLopez.Concurrency.SqlServer;
using EricksonLopez.Result;

Console.WriteLine("=================================================");
Console.WriteLine(" EricksonLopez.Concurrency NativeAOT Test Suite  ");
Console.WriteLine("=================================================");

int passedTests = 0;

void Assert([DoesNotReturnIf(false)] bool condition, string testName)
{
    if (!condition)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"[FAIL] {testName}");
        Console.ResetColor();
        throw new InvalidOperationException($"Assertion failed for: {testName}");
    }

    passedTests++;
    Console.WriteLine($"[PASS] {testName}");
}

// ── 1. Value Types & Tokens ──────────────────────────────────────────────
Console.WriteLine("\n--- 1. Value Types & Tokens ---");

var v1 = new ConcurrencyVersion(10);
var v2 = new ConcurrencyVersion(10);
var v3 = new ConcurrencyVersion(20);

Assert(v1 == v2, "ConcurrencyVersion equality works");
Assert(v1 < v3, "ConcurrencyVersion comparison works");
Assert(v1.Next().Value == 11, "ConcurrencyVersion.Next increments value");

var exp1 = ExpectedVersion.Specific(5);
var expAny = ExpectedVersion.Any;
var expNew = ExpectedVersion.New;

Assert(exp1.Version.Value == 5, "ExpectedVersion.Specific sets number");
Assert(expAny.Kind == ExpectedVersionKind.Any, "ExpectedVersion.Any kind");
Assert(expNew.Kind == ExpectedVersionKind.New, "ExpectedVersion.New kind");

// Dialect Tokens
var xmin = new XminConcurrencyToken(12345);
Assert(xmin.TokenKind == "PostgreSql.xmin", "XminConcurrencyToken kind is PostgreSql.xmin");
Assert(xmin.Value == "12345", "XminConcurrencyToken value format is correct");

byte[] rowVerBytes = [0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01];
var sqlServerToken = new SqlServerRowVersionToken(rowVerBytes);
Assert(sqlServerToken.TokenKind == "SqlServer.RowVersion", "SqlServerRowVersionToken kind is SqlServer.RowVersion");
Assert(sqlServerToken.Value == "0000000000000001", "SqlServerRowVersionToken value is hex-encoded");

var oracleToken = new OracleRowScnToken(998877);
Assert(oracleToken.TokenKind == "Oracle.ORA_ROWSCN", "OracleRowScnToken kind is Oracle.ORA_ROWSCN");
Assert(oracleToken.RowScn == 998877, "OracleRowScnToken SCN value is preserved");

// ── 2. Optimistic Concurrency Controller & CAS ──────────────────────────────
Console.WriteLine("\n--- 2. Optimistic Concurrency Controller ---");

var controller = new ConcurrencyController();

var state = new SampleEntity { Id = "ACC-100", Balance = 1000m, Version = 1 };

var casResult = await controller.ExecuteCasAsync(
    entity: state,
    expected: ExpectedVersion.Specific(1),
    entityId: state.Id,
    mutate: (acc, ct) => ValueTask.FromResult(acc with { Balance = 1200m }));

Assert(casResult.IsSuccess, "CAS with matching expected version succeeds");
Assert(casResult.Entity?.Balance == 1200m, "Mutated balance is 1200");
Assert(casResult.NewVersion.HasValue && casResult.NewVersion.Value.Value == 2, "Mutated version auto-incremented to 2");

var updatedState = casResult.Entity! with { Version = casResult.NewVersion!.Value.Value };

var failedCas = await controller.ExecuteCasAsync(
    entity: updatedState,
    expected: ExpectedVersion.Specific(1),
    entityId: state.Id,
    mutate: (acc, ct) => ValueTask.FromResult(acc with { Balance = 1500m }));

Assert(!failedCas.IsSuccess, "CAS with outdated expected version fails");
Assert(failedCas.Conflict is not null, "Conflict details are populated on failed CAS");
Assert(failedCas.Conflict!.ConflictType == ConcurrencyConflictType.VersionMismatch, "Conflict type is VersionMismatch");

// ── 3. Conflict Resolvers ───────────────────────────────────────────────────
Console.WriteLine("\n--- 3. Conflict Resolution Arbitration ---");

var lwwResolver = LastWriteWinsConflictResolver<SampleEntity>.Instance;
var resolution = await lwwResolver.ResolveAsync(
    proposedEntity: new SampleEntity { Id = "ACC-100", Balance = 1500m, Version = 1 },
    currentDatabaseEntity: new SampleEntity { Id = "ACC-100", Balance = 1200m, Version = 2 },
    conflict: failedCas.Conflict!);

Assert(resolution.IsResolved, "LastWriteWins resolves successfully");
Assert(resolution.ResolvedEntity?.Balance == 1500m, "Resolved state has proposed balance");

// ── 4. Result Pattern Integration ───────────────────────────────────────────
Console.WriteLine("\n--- 4. Result Pattern Integration ---");

Error error = ConcurrencyErrors.FromConflict(failedCas.Conflict!);
Assert(error.Code == ConcurrencyErrors.VersionMismatchCode, "Error code is VersionMismatchCode");
Assert(error.Type == ErrorType.Conflict, "Error type is Conflict");

// ── 5. OpenTelemetry Diagnostics ────────────────────────────────────────────
Console.WriteLine("\n--- 5. Diagnostics Verification ---");
ConcurrencyDiagnostics.RecordConflict(null, nameof(ConcurrencyConflictType.VersionMismatch), "Account");
ConcurrencyDiagnostics.RecordSuccess(null, "Account");
Assert(ConcurrencyDiagnostics.Meter.Name == "EricksonLopez.Concurrency", "Meter name is EricksonLopez.Concurrency");
Assert(ConcurrencyDiagnostics.ActivitySource.Name == "EricksonLopez.Concurrency", "ActivitySource name is EricksonLopez.Concurrency");

Console.WriteLine("\n=================================================");
Console.WriteLine($" ALL {passedTests} NATIVE AOT SUITE TESTS PASSED SUCCESSFULLY! ");
Console.WriteLine("=== AOT Validator: OK ===");
Console.WriteLine("=================================================");
