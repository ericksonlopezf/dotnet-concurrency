// Copyright © Erickson Lopez. MIT License.
using System;
using System.Data;
using System.Diagnostics.Metrics;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Dapper;
using EricksonLopez.Concurrency.Abstractions;
using EricksonLopez.Concurrency.Dapper;
using EricksonLopez.Concurrency.Diagnostics;
using Microsoft.Data.Sqlite;
using Xunit;

namespace EricksonLopez.Concurrency.Dapper.Tests;

public sealed class ConcurrencyDapperExtensionsTests : IDisposable
{
    private readonly SqliteConnection _connection;

    public ConcurrencyDapperExtensionsTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        _connection.Execute(@"
            CREATE TABLE products (
                id TEXT PRIMARY KEY,
                name TEXT NOT NULL,
                version INTEGER NOT NULL,
                token TEXT NOT NULL
            );
            INSERT INTO products (id, name, version, token) VALUES ('prod-1', 'Laptop', 1, 'token-v1');
        ");
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

    [Fact]
    public async Task ExecuteOptimisticAsync_WhenRowUpdated_ShouldReturnNullAndRecordSuccess()
    {
        long successCount = 0;
        string? recordedEntity = null;
        using var meterListener = new MeterListener();
        meterListener.InstrumentPublished = (inst, l) => { if (inst.Meter.Name == ConcurrencyDiagnostics.SourceName) l.EnableMeasurementEvents(inst); };
        meterListener.SetMeasurementEventCallback<long>((inst, measurement, tags, state) =>
        {
            if (inst.Name == "concurrency.successes")
            {
                string? entity = null;
                foreach (var tag in tags)
                {
                    if (tag.Key == "concurrency.entity_type") entity = tag.Value?.ToString();
                }
                if (entity == "Product")
                {
                    successCount += measurement;
                    recordedEntity = entity;
                }
            }
        });
        meterListener.Start();

        string sql = "UPDATE products SET name = @Name, version = version + 1 WHERE id = @Id AND version = @ExpectedVersion;";

        ConcurrencyConflict? conflict = await _connection.ExecuteOptimisticAsync(
            sql: sql,
            param: new { Name = "Laptop Pro", Id = "prod-1", ExpectedVersion = 1 },
            expectedVersion: ExpectedVersion.Specific(1),
            entityId: "prod-1",
            entityType: "Product");

        conflict.Should().BeNull();
        successCount.Should().BeGreaterThan(0);
        recordedEntity.Should().Be("Product");
    }

    [Fact]
    public async Task ExecuteOptimisticAsync_WhenNoRowUpdated_ShouldReturnConflictAndRecordConflict()
    {
        long conflictCount = 0;
        string? recordedType = null;
        using var meterListener = new MeterListener();
        meterListener.InstrumentPublished = (inst, l) => { if (inst.Meter.Name == ConcurrencyDiagnostics.SourceName) l.EnableMeasurementEvents(inst); };
        meterListener.SetMeasurementEventCallback<long>((inst, measurement, tags, state) =>
        {
            if (inst.Name == "concurrency.conflicts")
            {
                string? entity = null;
                string? conflictType = null;
                foreach (var tag in tags)
                {
                    if (tag.Key == "concurrency.entity_type") entity = tag.Value?.ToString();
                    if (tag.Key == "concurrency.conflict_type") conflictType = tag.Value?.ToString();
                }
                if (entity == "Product")
                {
                    conflictCount += measurement;
                    recordedType = conflictType;
                }
            }
        });
        meterListener.Start();

        string sql = "UPDATE products SET name = @Name, version = version + 1 WHERE id = @Id AND version = @ExpectedVersion;";

        // Stale expected version: 99
        ConcurrencyConflict? conflict = await _connection.ExecuteOptimisticAsync(
            sql: sql,
            param: new { Name = "Laptop Max", Id = "prod-1", ExpectedVersion = 99 },
            expectedVersion: ExpectedVersion.Specific(99),
            entityId: "prod-1",
            entityType: "Product");

        conflict.Should().NotBeNull();
        conflict!.ConflictType.Should().Be(ConcurrencyConflictType.VersionMismatch);
        conflict.EntityId.Should().Be("prod-1");
        conflict.EntityType.Should().Be("Product");
        conflict.ExpectedVersion.Should().Be(ExpectedVersion.Specific(99));
        conflict.Operation.Should().Be("ExecuteOptimisticUpdate");
        conflictCount.Should().BeGreaterThan(0);
        recordedType.Should().Be("VersionMismatch");
    }

    [Fact]
    public async Task ExecuteOptimisticAsync_WithTransactionAndOptions_ShouldWork()
    {
        using var tx = _connection.BeginTransaction();

        string sql = "UPDATE products SET name = @Name, version = version + 1 WHERE id = @Id AND version = @ExpectedVersion;";

        ConcurrencyConflict? conflict = await _connection.ExecuteOptimisticAsync(
            sql: sql,
            param: new { Name = "Laptop Tx", Id = "prod-1", ExpectedVersion = 1 },
            expectedVersion: ExpectedVersion.Specific(1),
            entityId: "prod-1",
            entityType: "Product",
            transaction: tx,
            commandTimeout: 30,
            commandType: CommandType.Text,
            cancellationToken: CancellationToken.None);

        conflict.Should().BeNull();
        tx.Commit();
    }

    [Fact]
    public async Task ExecuteOptimisticAsync_InvalidArguments_ShouldThrow()
    {
        var actNullConn = async () => await ConcurrencyDapperExtensions.ExecuteOptimisticAsync(
            null!, "UPDATE products SET version = 1", null, ExpectedVersion.Specific(1), "p1", "Product");
        await actNullConn.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("connection");

        var actNullSql = async () => await _connection.ExecuteOptimisticAsync(
            null!, null, ExpectedVersion.Specific(1), "p1", "Product");
        await actNullSql.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("sql");

        var actWhitespaceSql = async () => await _connection.ExecuteOptimisticAsync(
            "   ", null, ExpectedVersion.Specific(1), "p1", "Product");
        await actWhitespaceSql.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("sql");
    }

    [Fact]
    public async Task ExecuteOptimisticTokenAsync_WhenRowUpdated_ShouldReturnNullAndRecordSuccess()
    {
        long successCount = 0;
        string? recordedEntity = null;
        using var meterListener = new MeterListener();
        meterListener.InstrumentPublished = (inst, l) => { if (inst.Meter.Name == ConcurrencyDiagnostics.SourceName) l.EnableMeasurementEvents(inst); };
        meterListener.SetMeasurementEventCallback<long>((inst, measurement, tags, state) =>
        {
            if (inst.Name == "concurrency.successes")
            {
                successCount += measurement;
                foreach (var tag in tags)
                {
                    if (tag.Key == "concurrency.entity_type") recordedEntity = tag.Value?.ToString();
                }
            }
        });
        meterListener.Start();

        var expectedToken = new ConcurrencyToken("token-v1", "Custom");
        string sql = "UPDATE products SET token = @NewToken WHERE id = @Id AND token = @ExpectedToken;";

        ConcurrencyConflict? conflict = await _connection.ExecuteOptimisticTokenAsync(
            sql: sql,
            param: new { NewToken = "token-v2", Id = "prod-1", ExpectedToken = "token-v1" },
            expectedToken: expectedToken,
            entityId: "prod-1",
            entityType: "Product");

        conflict.Should().BeNull();
        successCount.Should().BeGreaterThan(0);
        recordedEntity.Should().Be("Product");
    }

    [Fact]
    public async Task ExecuteOptimisticTokenAsync_WhenNoRowUpdated_ShouldReturnConflictAndRecordConflict()
    {
        long conflictCount = 0;
        string? recordedType = null;
        using var meterListener = new MeterListener();
        meterListener.InstrumentPublished = (inst, l) => { if (inst.Meter.Name == ConcurrencyDiagnostics.SourceName) l.EnableMeasurementEvents(inst); };
        meterListener.SetMeasurementEventCallback<long>((inst, measurement, tags, state) =>
        {
            if (inst.Name == "concurrency.conflicts")
            {
                conflictCount += measurement;
                foreach (var tag in tags)
                {
                    if (tag.Key == "concurrency.conflict_type") recordedType = tag.Value?.ToString();
                }
            }
        });
        meterListener.Start();

        var expectedToken = new ConcurrencyToken("token-stale", "Custom");
        string sql = "UPDATE products SET token = @NewToken WHERE id = @Id AND token = @ExpectedToken;";

        ConcurrencyConflict? conflict = await _connection.ExecuteOptimisticTokenAsync(
            sql: sql,
            param: new { NewToken = "token-v2", Id = "prod-1", ExpectedToken = "token-stale" },
            expectedToken: expectedToken,
            entityId: "prod-1",
            entityType: "Product");

        conflict.Should().NotBeNull();
        conflict!.ConflictType.Should().Be(ConcurrencyConflictType.TokenMismatch);
        conflict.EntityId.Should().Be("prod-1");
        conflict.EntityType.Should().Be("Product");
        conflict.ExpectedToken.Should().Be(expectedToken);
        conflict.Operation.Should().Be("ExecuteOptimisticTokenUpdate");
        conflictCount.Should().BeGreaterThan(0);
        recordedType.Should().Be("TokenMismatch");
    }

    [Fact]
    public async Task ExecuteOptimisticTokenAsync_WithTransactionAndOptions_ShouldWork()
    {
        using var tx = _connection.BeginTransaction();

        var expectedToken = new ConcurrencyToken("token-v1", "Custom");
        string sql = "UPDATE products SET token = @NewToken WHERE id = @Id AND token = @ExpectedToken;";

        ConcurrencyConflict? conflict = await _connection.ExecuteOptimisticTokenAsync(
            sql: sql,
            param: new { NewToken = "token-v2", Id = "prod-1", ExpectedToken = "token-v1" },
            expectedToken: expectedToken,
            entityId: "prod-1",
            entityType: "Product",
            transaction: tx,
            commandTimeout: 45,
            commandType: CommandType.Text,
            cancellationToken: CancellationToken.None);

        conflict.Should().BeNull();
        tx.Commit();
    }

    [Fact]
    public async Task ExecuteOptimisticTokenAsync_InvalidArguments_ShouldThrow()
    {
        var token = new ConcurrencyToken("token-1", "Custom");

        var actNullConn = async () => await ConcurrencyDapperExtensions.ExecuteOptimisticTokenAsync(
            null!, "UPDATE products SET token = 't2'", null, token, "p1", "Product");
        await actNullConn.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("connection");

        var actNullSql = async () => await _connection.ExecuteOptimisticTokenAsync(
            null!, null, token, "p1", "Product");
        await actNullSql.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("sql");

        var actWhitespaceSql = async () => await _connection.ExecuteOptimisticTokenAsync(
            "   ", null, token, "p1", "Product");
        await actWhitespaceSql.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("sql");

        var actNullToken = async () => await _connection.ExecuteOptimisticTokenAsync(
            "UPDATE products SET token = 't2'", null, null!, "p1", "Product");
        await actNullToken.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("expectedToken");
    }
}
