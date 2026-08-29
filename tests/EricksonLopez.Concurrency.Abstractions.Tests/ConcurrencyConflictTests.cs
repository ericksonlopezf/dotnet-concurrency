// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using AwesomeAssertions;
using EricksonLopez.Concurrency.Abstractions;
using Xunit;

namespace EricksonLopez.Concurrency.Abstractions.Tests;

public sealed class ConcurrencyConflictTests
{
    [Fact]
    public void VersionMismatch_WithActualVersion_ShouldPopulateConflictRecordCorrectly()
    {
        var expected = ExpectedVersion.Specific(10);
        var actual = ActualVersion.From(12);
        var metadata = new Dictionary<string, string> { ["UserId"] = "user_42" };

        ConcurrencyConflict conflict = ConcurrencyConflict.VersionMismatch(
            entityId: "cust_123",
            entityType: "Customer",
            expected: expected,
            actual: actual,
            operation: "UpdateName",
            metadata: metadata);

        conflict.EntityId.Should().Be("cust_123");
        conflict.EntityType.Should().Be("Customer");
        conflict.ConflictType.Should().Be(ConcurrencyConflictType.VersionMismatch);
        conflict.Classification.Should().Be(ConcurrencyConflictClassification.StaleState);
        conflict.ExpectedVersion.Should().Be(expected);
        conflict.ActualVersion.Should().Be(actual);
        conflict.Operation.Should().Be("UpdateName");
        conflict.Message.Should().Be("Optimistic concurrency conflict on 'Customer' with ID 'cust_123'. Expected [Expected:10], but found [Actual:12].");
        conflict.Metadata.Should().ContainKey("UserId").WhoseValue.Should().Be("user_42");
        conflict.Timestamp.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void VersionMismatch_WithoutActualVersion_ShouldFormatZeroRowCountMessage()
    {
        var expected = ExpectedVersion.Specific(5);

        ConcurrencyConflict conflict = ConcurrencyConflict.VersionMismatch(
            entityId: "order_1",
            entityType: "Order",
            expected: expected,
            actual: null);

        conflict.EntityId.Should().Be("order_1");
        conflict.EntityType.Should().Be("Order");
        conflict.ConflictType.Should().Be(ConcurrencyConflictType.VersionMismatch);
        conflict.Classification.Should().Be(ConcurrencyConflictClassification.StaleState);
        conflict.ExpectedVersion.Should().Be(expected);
        conflict.ActualVersion.Should().BeNull();
        conflict.Operation.Should().Be("Update");
        conflict.Message.Should().Be("Optimistic concurrency conflict on 'Order' with ID 'order_1'. Expected [Expected:5], but row count affected was 0.");
        conflict.Metadata.Should().BeEmpty();
    }

    [Fact]
    public void TokenMismatch_WithActualToken_ShouldPopulateConflictRecordCorrectly()
    {
        var expectedToken = new ConcurrencyToken("etag-v1", "ETag");
        var actualToken = new ConcurrencyToken("etag-v2", "ETag");
        var metadata = new Dictionary<string, string> { ["TraceId"] = "trace_abc" };

        ConcurrencyConflict conflict = ConcurrencyConflict.TokenMismatch(
            entityId: "order_99",
            entityType: "Order",
            expected: expectedToken,
            actual: actualToken,
            operation: "Checkout",
            metadata: metadata);

        conflict.EntityId.Should().Be("order_99");
        conflict.EntityType.Should().Be("Order");
        conflict.ConflictType.Should().Be(ConcurrencyConflictType.TokenMismatch);
        conflict.Classification.Should().Be(ConcurrencyConflictClassification.StaleState);
        conflict.ExpectedToken.Should().Be(expectedToken);
        conflict.ActualToken.Should().Be(actualToken);
        conflict.Operation.Should().Be("Checkout");
        conflict.Message.Should().Be("Concurrency token mismatch on 'Order' with ID 'order_99'. Expected 'etag-v1', but found 'etag-v2'.");
        conflict.Metadata.Should().ContainKey("TraceId").WhoseValue.Should().Be("trace_abc");
    }

    [Fact]
    public void TokenMismatch_WithoutActualToken_ShouldFormatZeroRowCountMessage()
    {
        var expectedToken = new ConcurrencyToken("etag-v1", "ETag");

        ConcurrencyConflict conflict = ConcurrencyConflict.TokenMismatch(
            entityId: "order_99",
            entityType: "Order",
            expected: expectedToken,
            actual: null);

        conflict.Message.Should().Be("Concurrency token mismatch on 'Order' with ID 'order_99'. Expected 'etag-v1', but row count affected was 0.");
        conflict.ActualToken.Should().BeNull();
        conflict.Metadata.Should().BeEmpty();
    }

    [Fact]
    public void Deleted_ShouldPopulateStateDeletedConflict()
    {
        ConcurrencyConflict conflict = ConcurrencyConflict.Deleted("prod_55", "Product", "RemoveItem");

        conflict.EntityId.Should().Be("prod_55");
        conflict.EntityType.Should().Be("Product");
        conflict.ConflictType.Should().Be(ConcurrencyConflictType.StateDeleted);
        conflict.Classification.Should().Be(ConcurrencyConflictClassification.NonRetryable);
        conflict.Operation.Should().Be("RemoveItem");
        conflict.Message.Should().Be("Entity 'Product' with ID 'prod_55' does not exist or has been deleted.");
        conflict.ActualVersion.Should().Be(ActualVersion.NotFound);
    }

    [Fact]
    public void Deleted_DefaultOperation_ShouldBeUpdate()
    {
        ConcurrencyConflict conflict = ConcurrencyConflict.Deleted("prod_55", "Product");
        conflict.Operation.Should().Be("Update");
    }

    [Fact]
    public void Constructor_WithNullDefaults_ShouldApplyFallbackValues()
    {
        var fixedTime = new DateTimeOffset(2026, 8, 27, 12, 0, 0, TimeSpan.Zero);

        var conflict = new ConcurrencyConflict(
            entityId: null!,
            entityType: null!,
            conflictType: ConcurrencyConflictType.Custom,
            classification: ConcurrencyConflictClassification.Fatal,
            operation: null!,
            message: null!,
            expectedVersion: null,
            actualVersion: null,
            expectedToken: null,
            actualToken: null,
            timestamp: fixedTime,
            metadata: null);

        conflict.EntityId.Should().Be(string.Empty);
        conflict.EntityType.Should().Be("Unknown");
        conflict.Operation.Should().Be("Update");
        conflict.Message.Should().Be("A concurrency conflict occurred.");
        conflict.Timestamp.Should().Be(fixedTime);
        conflict.Metadata.Should().BeEmpty();
    }

    [Fact]
    public void Enums_ShouldHaveExpectedValues()
    {
        ((byte)ConcurrencyConflictClassification.Transient).Should().Be(0);
        ((byte)ConcurrencyConflictClassification.Retryable).Should().Be(1);
        ((byte)ConcurrencyConflictClassification.NonRetryable).Should().Be(2);
        ((byte)ConcurrencyConflictClassification.StaleState).Should().Be(3);
        ((byte)ConcurrencyConflictClassification.Fatal).Should().Be(4);

        ((byte)ConcurrencyConflictType.VersionMismatch).Should().Be(0);
        ((byte)ConcurrencyConflictType.TokenMismatch).Should().Be(1);
        ((byte)ConcurrencyConflictType.StateDeleted).Should().Be(2);
        ((byte)ConcurrencyConflictType.AlreadyExists).Should().Be(3);
        ((byte)ConcurrencyConflictType.SerializationFailure).Should().Be(4);
        ((byte)ConcurrencyConflictType.Deadlock).Should().Be(5);
        ((byte)ConcurrencyConflictType.LockUnavailable).Should().Be(6);
        ((byte)ConcurrencyConflictType.Custom).Should().Be(7);
    }
}
