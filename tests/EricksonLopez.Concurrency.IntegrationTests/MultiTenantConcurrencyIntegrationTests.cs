// Copyright © Erickson Lopez. MIT License.
using System.Threading.Tasks;
using AwesomeAssertions;
using Dapper;
using EricksonLopez.Concurrency.Abstractions;
using EricksonLopez.Concurrency.Dapper;
using Microsoft.Data.Sqlite;
using Xunit;

namespace EricksonLopez.Concurrency.IntegrationTests;

public sealed class MultiTenantConcurrencyIntegrationTests
{
    private static async Task<SqliteConnection> CreateTenantDatabaseAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        string initSql = @"
            CREATE TABLE documents (
                id TEXT NOT NULL,
                tenant_id TEXT NOT NULL,
                title TEXT NOT NULL,
                version INTEGER NOT NULL,
                PRIMARY KEY (id, tenant_id)
            );
            INSERT INTO documents (id, tenant_id, title, version) VALUES ('doc_1', 'tenant_a', 'Doc A', 1);
            INSERT INTO documents (id, tenant_id, title, version) VALUES ('doc_1', 'tenant_b', 'Doc B', 1);
        ";

        await connection.ExecuteAsync(initSql);
        return connection;
    }

    [Fact]
    public async Task MultiTenantUpdate_ShouldIsolateTenantsWithIdenticalEntityIds()
    {
        await using SqliteConnection connection = await CreateTenantDatabaseAsync();

        string updateSql = OptimisticUpdateBuilder.BuildVersionedUpdate(
            tableName: "documents",
            setClauses: "title = @Title",
            idColumn: "id",
            versionColumn: "version",
            tenantColumn: "tenant_id");

        // Update tenant_a only
        ConcurrencyConflict? conflictA = await connection.ExecuteOptimisticAsync(
            sql: updateSql,
            param: new { Id = "doc_1", TenantId = "tenant_a", Title = "Doc A Updated", ExpectedVersion = 1L },
            expectedVersion: ExpectedVersion.Specific(1),
            entityId: "doc_1",
            entityType: "Document");

        conflictA.Should().BeNull();

        // Verify tenant_a updated to version 2
        long versionA = await connection.ExecuteScalarAsync<long>(
            "SELECT version FROM documents WHERE id = 'doc_1' AND tenant_id = 'tenant_a'");
        versionA.Should().Be(2);

        // Verify tenant_b remains untouched at version 1
        long versionB = await connection.ExecuteScalarAsync<long>(
            "SELECT version FROM documents WHERE id = 'doc_1' AND tenant_id = 'tenant_b'");
        versionB.Should().Be(1);
    }
}
