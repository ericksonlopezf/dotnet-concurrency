// Copyright © Erickson Lopez. MIT License.
using System.Collections.Concurrent;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Dapper;
using EricksonLopez.Concurrency.Abstractions;
using EricksonLopez.Concurrency.Dapper;
using Microsoft.Data.Sqlite;
using Xunit;

namespace EricksonLopez.Concurrency.IntegrationTests;

public sealed class SqliteOptimisticConcurrencyIntegrationTests
{
    private static async Task<SqliteConnection> CreateDatabaseAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        string initSql = @"
            CREATE TABLE products (
                id TEXT PRIMARY KEY,
                name TEXT NOT NULL,
                price REAL NOT NULL,
                version INTEGER NOT NULL
            );
            INSERT INTO products (id, name, price, version) VALUES ('prod_1', 'Laptop', 1200.0, 10);
        ";

        await connection.ExecuteAsync(initSql);
        return connection;
    }

    [Fact]
    public async Task ExecuteOptimisticAsync_SingleWriter_ShouldSucceedAndIncrementVersion()
    {
        await using SqliteConnection connection = await CreateDatabaseAsync();

        string updateSql = OptimisticUpdateBuilder.BuildVersionedUpdate(
            tableName: "products",
            setClauses: "price = @Price",
            idColumn: "id",
            versionColumn: "version");

        ConcurrencyConflict? conflict = await connection.ExecuteOptimisticAsync(
            sql: updateSql,
            param: new { Id = "prod_1", Price = 1300.0, ExpectedVersion = 10L },
            expectedVersion: ExpectedVersion.Specific(10),
            entityId: "prod_1",
            entityType: "Product");

        conflict.Should().BeNull();

        long newVersion = await connection.ExecuteScalarAsync<long>("SELECT version FROM products WHERE id = 'prod_1'");
        newVersion.Should().Be(11);
    }

    [Fact]
    public async Task ExecuteOptimisticAsync_StaleVersion_ShouldReturnConflictWithoutModifyingData()
    {
        await using SqliteConnection connection = await CreateDatabaseAsync();

        string updateSql = OptimisticUpdateBuilder.BuildVersionedUpdate(
            tableName: "products",
            setClauses: "price = @Price",
            idColumn: "id",
            versionColumn: "version");

        // Request update with stale version 9 (actual version in DB is 10)
        ConcurrencyConflict? conflict = await connection.ExecuteOptimisticAsync(
            sql: updateSql,
            param: new { Id = "prod_1", Price = 1500.0, ExpectedVersion = 9L },
            expectedVersion: ExpectedVersion.Specific(9),
            entityId: "prod_1",
            entityType: "Product");

        conflict.Should().NotBeNull();
        conflict!.ConflictType.Should().Be(ConcurrencyConflictType.VersionMismatch);
        conflict.EntityId.Should().Be("prod_1");

        long currentVersion = await connection.ExecuteScalarAsync<long>("SELECT version FROM products WHERE id = 'prod_1'");
        currentVersion.Should().Be(10); // Unchanged
    }
}
