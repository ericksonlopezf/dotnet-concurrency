// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading.Tasks;
using EricksonLopez.Concurrency.Abstractions;
using EricksonLopez.Concurrency.MariaDb;
using EricksonLopez.Concurrency.MySql;
using EricksonLopez.Concurrency.Oracle;
using EricksonLopez.Concurrency.PostgreSql;
using EricksonLopez.Concurrency.SqlServer;

namespace EricksonLopez.Concurrency.Showcase.Levels;

/// <summary>
/// Provides demonstrations of specialized database tokens (xmin, ROWVERSION, ORA_ROWSCN) and dialect-specific locking hints.
/// </summary>
public static class Level09_SpecializedTokensAndLocking
{
    /// <summary>
    /// Executes the specialized tokens and locking demonstration.
    /// </summary>
    /// <remarks>
    /// Cookbook: Level 09 — Specialized Tokens and Dialect Locking.
    /// Prerequisites: Level01-08.
    /// Concepts: Engine-specific concurrency tokens, pessimistic locking SQL clauses for all 5 dialects.
    /// APIs: XminConcurrencyToken.From()/Parse(), SqlServerRowVersionToken.Parse()/ToByteArray(),
    ///        OracleRowScnToken.Parse()/From(), ConcurrencyVersion.Next(),
    ///        PostgreSqlExtensions.WithLock(), SqlServerExtensions.WithSqlServerTableHint(),
    ///        MySqlExtensions.WithMySqlLock(), MariaDbExtensions.WithMariaDbLock()/WithMariaDbLockWait(),
    ///        OracleExtensions.WithOracleLock()/WithOracleLockWait().
    /// Complexity: Advanced.
    /// Next: Level10_EnterpriseArchitecture for CQRS, mediator, and multi-tenancy.
    /// </remarks>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static Task RunAsync()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n===============================================================================");
        Console.WriteLine(" LEVEL 09: SPECIALIZED TOKENS & DIALECT LOCKING CLAUSES");
        Console.WriteLine("===============================================================================");
        Console.ResetColor();

        // -------------------------------------------------------------
        // PART 1: Native Engine-Specific Tokens
        // -------------------------------------------------------------
        Console.WriteLine("\n--- Part 1: Engine-Specific Tokens ---");

        // 1. PostgreSQL xmin token (uint 32-bit transaction id)
        var xminToken = XminConcurrencyToken.From(987654321);
        Console.WriteLine($"[PostgreSQL] XminConcurrencyToken: Value='{xminToken.Value}', Kind='{xminToken.TokenKind}', ToString='{xminToken}'");

        // 2. SQL Server ROWVERSION token (8-byte binary)
        var rowVersionToken = SqlServerRowVersionToken.Parse("0x00000000000007D1");
        Console.WriteLine($"[SQL Server] SqlServerRowVersionToken: Value='{rowVersionToken.Value}', Kind='{rowVersionToken.TokenKind}', Hex='{rowVersionToken}'");

        // 3. Oracle ORA_ROWSCN token (64-bit System Change Number)
        var oracleScnToken = OracleRowScnToken.Parse("184467440737");
        Console.WriteLine($"[Oracle]     OracleRowScnToken: Value='{oracleScnToken.Value}', Kind='{oracleScnToken.TokenKind}', ToString='{oracleScnToken}'");

        // 4. ConcurrencyVersion.Next() — untyped overload (GAP 8)
        var baseVersion = new ConcurrencyVersion(7);
        ConcurrencyVersion nextVersion = baseVersion.Next();
        Console.WriteLine($"[ConcurrencyVersion] Untyped .Next(): {baseVersion.Value} -> {nextVersion.Value}");

        // -------------------------------------------------------------
        // PART 2: Pessimistic Locking Clauses & Query Hints
        // -------------------------------------------------------------
        Console.WriteLine("\n--- Part 2: Pessimistic Locking Clauses and Hints ---");

        // PostgreSQL — all 5 lock modes documented
        string pgQuery = "SELECT id, status FROM orders WHERE id = @Id"
            .WithLock(PostgreSqlLockMode.ForUpdateSkipLocked);
        Console.WriteLine($"[PostgreSQL] ForUpdateSkipLocked:\n    {pgQuery}");

        string pgShareQuery = "SELECT id, balance FROM accounts WHERE id = @Id"
            .WithLock(PostgreSqlLockMode.ForShare);
        Console.WriteLine($"[PostgreSQL] ForShare:\n    {pgShareQuery}");

        string pgNoKeyQuery = "SELECT id, status FROM orders WHERE id = @Id"
            .WithLock(PostgreSqlLockMode.ForNoKeyUpdate);
        Console.WriteLine($"[PostgreSQL] ForNoKeyUpdate:\n    {pgNoKeyQuery}");

        // SQL Server
        string sqlServerQuery = "orders"
            .WithSqlServerTableHint(SqlServerLockMode.UpdLockRowLockNowait);
        Console.WriteLine($"[SQL Server] WithSqlServerTableHint (UPDLOCK, NOWAIT):\n    SELECT * FROM {sqlServerQuery} WHERE id = @Id;");

        // MySQL
        string mySqlQuery = "SELECT id, total FROM orders WHERE id = @Id"
            .WithMySqlLock(MySqlLockMode.ForUpdateNowait);
        Console.WriteLine($"[MySQL]      WithMySqlLock (NOWAIT):\n    {mySqlQuery}");

        // MariaDB
        string mariaDbQuery = "SELECT id, total FROM orders WHERE id = @Id"
            .WithMariaDbLockWait(5);
        Console.WriteLine($"[MariaDB]    WithMariaDbLockWait (5s):\n    {mariaDbQuery}");

        // Oracle
        string oracleQuery = "SELECT id, total FROM orders WHERE id = @Id"
            .WithOracleLockWait(10);
        Console.WriteLine($"[Oracle]     WithOracleLockWait (10s):\n    {oracleQuery}");

        return Task.CompletedTask;
    }
}
