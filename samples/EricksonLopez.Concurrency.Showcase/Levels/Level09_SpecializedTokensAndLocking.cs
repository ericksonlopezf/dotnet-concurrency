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
/// Level 09: Specialized Database Tokens and Pessimistic Locking Hints — xmin, ROWVERSION, ORA_ROWSCN, and query hint extensions.
/// </summary>
public static class Level09_SpecializedTokensAndLocking
{
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

        // -------------------------------------------------------------
        // PART 2: Pessimistic Locking Clauses & Query Hints
        // -------------------------------------------------------------
        Console.WriteLine("\n--- Part 2: Pessimistic Locking Clauses and Hints ---");

        // PostgreSQL
        string pgQuery = "SELECT id, status FROM orders WHERE id = @Id"
            .WithLock(PostgreSqlLockMode.ForUpdateSkipLocked);
        Console.WriteLine($"[PostgreSQL] WithLock (SkipLocked):\n    {pgQuery}");

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
