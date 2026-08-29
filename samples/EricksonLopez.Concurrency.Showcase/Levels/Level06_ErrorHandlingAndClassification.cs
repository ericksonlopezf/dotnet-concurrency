// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading.Tasks;
using EricksonLopez.Concurrency.Abstractions;
using EricksonLopez.Concurrency.MariaDb;
using EricksonLopez.Concurrency.MySql;
using EricksonLopez.Concurrency.Oracle;
using EricksonLopez.Concurrency.PostgreSql;
using EricksonLopez.Concurrency.Result;
using EricksonLopez.Concurrency.Sqlite;
using EricksonLopez.Concurrency.SqlServer;
using EricksonLopez.Result;
using Microsoft.Data.Sqlite;
using Npgsql;

namespace EricksonLopez.Concurrency.Showcase.Levels;

/// <summary>
/// Level 06: Error Handling and Dialect Classification — Database error classification across PostgreSQL, SQL Server, MySQL, MariaDB, Oracle, SQLite, and ConcurrencyException hierarchy.
/// </summary>
public static class Level06_ErrorHandlingAndClassification
{
    public static Task RunAsync()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n===============================================================================");
        Console.WriteLine(" LEVEL 06: ERROR HANDLING, EXCEPTION HIERARCHY & DATABASE CLASSIFICATION MATRIX");
        Console.WriteLine("===============================================================================");
        Console.ResetColor();

        // -------------------------------------------------------------
        // 1. Database Error Classification Matrix
        // -------------------------------------------------------------
        Console.WriteLine("\n[1] PostgreSQL Error Classifier (Npgsql):");
        var pgSerializationEx = new PostgresException("could not serialize access due to concurrent update", "ERROR", "ERROR", "40001");
        var pgDeadlockEx = new PostgresException("deadlock detected", "ERROR", "ERROR", "40P01");
        var pgLockUnavailableEx = new PostgresException("could not obtain lock on row in relation", "ERROR", "ERROR", "55P03");
        var pgUniqueEx = new PostgresException("duplicate key value violates unique constraint", "ERROR", "ERROR", "23505");

        Console.WriteLine($"    - SQLSTATE 40001 -> IsSerializationFailure: {PostgreSqlConcurrencyErrorClassifier.IsSerializationFailure(pgSerializationEx)}, IsTransient: {PostgreSqlConcurrencyErrorClassifier.IsTransient(pgSerializationEx)}");
        Console.WriteLine($"    - SQLSTATE 40P01 -> IsDeadlock:             {PostgreSqlConcurrencyErrorClassifier.IsDeadlock(pgDeadlockEx)}, IsTransient: {PostgreSqlConcurrencyErrorClassifier.IsTransient(pgDeadlockEx)}");
        Console.WriteLine($"    - SQLSTATE 55P03 -> IsLockNotAvailable:     {PostgreSqlConcurrencyErrorClassifier.IsLockNotAvailable(pgLockUnavailableEx)}");
        Console.WriteLine($"    - SQLSTATE 23505 -> IsUniqueViolation:      {PostgreSqlConcurrencyErrorClassifier.IsUniqueViolation(pgUniqueEx)}");

        ConcurrencyConflict? pgConflict = PostgreSqlConcurrencyErrorClassifier.ToConcurrencyConflict(pgSerializationEx, "ORDER-500", "OrderAggregate");
        if (pgConflict is not null)
        {
            Error err = ConcurrencyErrors.FromConflict(pgConflict);
            Console.WriteLine($"    -> Mapped to Error: Code={err.Code}, Type={err.Type}, Retryability={err.Retryability}");
        }

        Console.WriteLine("\n[2] Microsoft SQL Server Error Classifier (SqlServerErrorClassifier):");
        Console.WriteLine($"    - Error 1205 (Deadlock)             -> Classified as Transient (SerializationFailure)");
        Console.WriteLine($"    - Error 3960 (Snapshot Conflict)    -> Classified as Transient (SerializationFailure)");
        Console.WriteLine($"    - Error 1222 (Lock Timeout)         -> Classified as Transient");
        Console.WriteLine($"    - Error 2601/2627 (Unique Violation)-> Classified as StaleState (NonRetryable)");

        Console.WriteLine("\n[3] MySQL & MariaDB Error Classifier:");
        Console.WriteLine($"    - Error 1213 (ER_LOCK_DEADLOCK)     -> Deadlock / Transient");
        Console.WriteLine($"    - Error 1205 (ER_LOCK_WAIT_TIMEOUT) -> Lock Timeout / Transient");
        Console.WriteLine($"    - Error 1062 (ER_DUP_ENTRY)         -> Duplicate Entry / StaleState");

        Console.WriteLine("\n[4] Oracle Error Classifier (OracleConcurrencyErrorClassifier):");
        Console.WriteLine($"    - ORA-00060 (Deadlock detected)           -> Transient");
        Console.WriteLine($"    - ORA-00054 (Resource busy NOWAIT)        -> Transient");
        Console.WriteLine($"    - ORA-08177 (Can't serialize access)      -> Transient");
        Console.WriteLine($"    - ORA-00001 (Unique constraint violated)  -> StaleState");

        Console.WriteLine("\n[5] SQLite Error Classifier:");
        var sqliteBusyEx = new SqliteException("database is locked", 5);
        var sqliteLockedEx = new SqliteException("table is locked", 6);
        var sqliteConstraintEx = new SqliteException("constraint failed", 19);

        Console.WriteLine($"    - SQLITE_BUSY (5)       -> IsBusy: {SqliteConcurrencyErrorClassifier.IsBusy(sqliteBusyEx)}, IsTransient: {SqliteConcurrencyErrorClassifier.IsTransient(sqliteBusyEx)}");
        Console.WriteLine($"    - SQLITE_LOCKED (6)     -> IsLocked: {SqliteConcurrencyErrorClassifier.IsLocked(sqliteLockedEx)}, IsTransient: {SqliteConcurrencyErrorClassifier.IsTransient(sqliteLockedEx)}");
        Console.WriteLine($"    - SQLITE_CONSTRAINT(19) -> IsConstraintViolation: {SqliteConcurrencyErrorClassifier.IsConstraintViolation(sqliteConstraintEx)}");

        ConcurrencyConflict? sqliteConflict = SqliteConcurrencyErrorClassifier.ToConcurrencyConflict(sqliteBusyEx, "ACC-100", "Account");
        if (sqliteConflict is not null)
        {
            Console.WriteLine($"    -> SQLite Conflict Record: {sqliteConflict.Message} [Classification: {sqliteConflict.Classification}]");
        }

        // -------------------------------------------------------------
        // 2. Exception Hierarchy Demonstration
        // -------------------------------------------------------------
        Console.WriteLine("\n[6] Concurrency Exception Hierarchy:");

        // A) ConcurrencyException with embedded ConcurrencyConflict
        var sampleConflict = ConcurrencyConflict.VersionMismatch("ACC-900", "Account", ExpectedVersion.Specific(1), ActualVersion.From(2));
        var concurrencyEx = new ConcurrencyException(sampleConflict);
        Console.WriteLine($"    - ConcurrencyException: Message='{concurrencyEx.Message}', Embedded Conflict Entity='{concurrencyEx.Conflict?.EntityId}'");

        // B) ConcurrencyTokenMismatchException
        var tokenMismatchEx = new ConcurrencyTokenMismatchException(ConcurrencyToken.From("token-a"), ConcurrencyToken.From("token-b"));
        Console.WriteLine($"    - ConcurrencyTokenMismatchException: Expected='{tokenMismatchEx.ExpectedToken?.Value}', Actual='{tokenMismatchEx.ActualToken?.Value}'");

        // C) ConcurrencyConfigurationException
        var configEx = new ConcurrencyConfigurationException("Invalid resolution policy configured for aggregate root.");
        Console.WriteLine($"    - ConcurrencyConfigurationException: Message='{configEx.Message}'");

        return Task.CompletedTask;
    }
}
