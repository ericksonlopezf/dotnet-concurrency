// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using AwesomeAssertions;
using EricksonLopez.Concurrency.Abstractions;
using EricksonLopez.Concurrency.Result;
using EricksonLopez.Result;
using Xunit;

namespace EricksonLopez.Concurrency.Result.Tests;

public sealed class ConcurrencyErrorsTests
{
    [Fact]
    public void Constants_ShouldHaveExpectedValues()
    {
        ConcurrencyErrors.ConcurrencyConflictCode.Should().Be("Concurrency.Conflict");
        ConcurrencyErrors.VersionMismatchCode.Should().Be("Concurrency.VersionMismatch");
        ConcurrencyErrors.TokenMismatchCode.Should().Be("Concurrency.TokenMismatch");
        ConcurrencyErrors.EntityDeletedCode.Should().Be("Concurrency.EntityDeleted");
        ConcurrencyErrors.SerializationFailureCode.Should().Be("Concurrency.SerializationFailure");
        ConcurrencyErrors.DeadlockCode.Should().Be("Concurrency.Deadlock");
    }

    [Fact]
    public void FromConflict_NullConflict_ShouldThrowArgumentNullException()
    {
        Action act = () => ConcurrencyErrors.FromConflict(null!);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("conflict");
    }

    [Theory]
    [InlineData(ConcurrencyConflictType.VersionMismatch, "Concurrency.VersionMismatch")]
    [InlineData(ConcurrencyConflictType.TokenMismatch, "Concurrency.TokenMismatch")]
    [InlineData(ConcurrencyConflictType.StateDeleted, "Concurrency.EntityDeleted")]
    [InlineData(ConcurrencyConflictType.SerializationFailure, "Concurrency.SerializationFailure")]
    [InlineData(ConcurrencyConflictType.Deadlock, "Concurrency.Deadlock")]
    [InlineData(ConcurrencyConflictType.Custom, "Concurrency.Conflict")]
    public void FromConflict_ConflictTypes_ShouldMapToCorrectCodes(ConcurrencyConflictType type, string expectedCode)
    {
        var conflict = new ConcurrencyConflict(
            "e1",
            "Entity",
            type,
            ConcurrencyConflictClassification.Transient,
            "Op",
            "Conflict occurred.");

        Error error = ConcurrencyErrors.FromConflict(conflict);

        error.Code.Should().Be(expectedCode);
        error.Description.Should().Be("Conflict occurred.");
        error.Type.Should().Be(ErrorType.Conflict);
        error.Severity.Should().Be(ErrorSeverity.Warning);
    }

    [Theory]
    [InlineData(ConcurrencyConflictClassification.Transient, ErrorRetryability.Transient)]
    [InlineData(ConcurrencyConflictClassification.Retryable, ErrorRetryability.Transient)]
    [InlineData(ConcurrencyConflictClassification.NonRetryable, ErrorRetryability.Permanent)]
    [InlineData(ConcurrencyConflictClassification.Fatal, ErrorRetryability.Permanent)]
    [InlineData((ConcurrencyConflictClassification)250, ErrorRetryability.NotApplicable)]
    public void FromConflict_Classifications_ShouldMapToCorrectRetryability(
        ConcurrencyConflictClassification classification,
        ErrorRetryability expectedRetryability)
    {
        var conflict = new ConcurrencyConflict(
            "e1",
            "Entity",
            ConcurrencyConflictType.VersionMismatch,
            classification,
            "Op",
            "Classification test.");

        Error error = ConcurrencyErrors.FromConflict(conflict);

        error.Retryability.Should().Be(expectedRetryability);
    }

    [Fact]
    public void FromConflict_ShouldMapFullMetadata_WhenAllFieldsPresent()
    {
        var timestamp = new DateTimeOffset(2026, 8, 27, 20, 0, 0, TimeSpan.Zero);
        var customMeta = new Dictionary<string, string>
        {
            ["tenantId"] = "tenant-xyz",
            ["region"] = "us-east-1"
        };

        var expectedVersion = ExpectedVersion.Specific(10);
        var actualVersion = ActualVersion.From(12);
        var expectedToken = new ConcurrencyToken("token-exp", "ETag");
        var actualToken = new ConcurrencyToken("token-act", "ETag");

        var conflict = new ConcurrencyConflict(
            entityId: "order-999",
            entityType: "OrderAggregate",
            conflictType: ConcurrencyConflictType.VersionMismatch,
            classification: ConcurrencyConflictClassification.Transient,
            operation: "UpdateOrder",
            message: "Version mismatch detected.",
            expectedVersion: expectedVersion,
            actualVersion: actualVersion,
            expectedToken: expectedToken,
            actualToken: actualToken,
            timestamp: timestamp,
            metadata: customMeta);

        Error error = ConcurrencyErrors.FromConflict(conflict);

        error.Code.Should().Be(ConcurrencyErrors.VersionMismatchCode);
        error.Description.Should().Be("Version mismatch detected.");
        error.Type.Should().Be(ErrorType.Conflict);
        error.Severity.Should().Be(ErrorSeverity.Warning);
        error.Retryability.Should().Be(ErrorRetryability.Transient);

        error.Metadata.Should().NotBeNull();
        error.Metadata!["entityId"].Should().Be("order-999");
        error.Metadata["entityType"].Should().Be("OrderAggregate");
        error.Metadata["operation"].Should().Be("UpdateOrder");
        error.Metadata["conflictType"].Should().Be("VersionMismatch");
        error.Metadata["classification"].Should().Be("Transient");
        error.Metadata["timestamp"].Should().Be(timestamp.ToString("O"));
        error.Metadata["expectedVersion"].Should().Be(expectedVersion.ToString());
        error.Metadata["actualVersion"].Should().Be(actualVersion.ToString());
        error.Metadata["expectedToken"].Should().Be("token-exp");
        error.Metadata["actualToken"].Should().Be("token-act");
        error.Metadata["tenantId"].Should().Be("tenant-xyz");
        error.Metadata["region"].Should().Be("us-east-1");
    }

    [Fact]
    public void FromConflict_ShouldOmitOptionalMetadata_WhenFieldsAreNull()
    {
        var conflict = new ConcurrencyConflict(
            entityId: "item-1",
            entityType: "Item",
            conflictType: ConcurrencyConflictType.StateDeleted,
            classification: ConcurrencyConflictClassification.NonRetryable,
            operation: "DeleteItem",
            message: "Item deleted.",
            expectedVersion: null,
            actualVersion: null,
            expectedToken: null,
            actualToken: null,
            timestamp: DateTimeOffset.UtcNow,
            metadata: null);

        Error error = ConcurrencyErrors.FromConflict(conflict);

        error.Metadata.Should().NotBeNull();
        error.Metadata!.ContainsKey("expectedVersion").Should().BeFalse();
        error.Metadata.ContainsKey("actualVersion").Should().BeFalse();
        error.Metadata.ContainsKey("expectedToken").Should().BeFalse();
        error.Metadata.ContainsKey("actualToken").Should().BeFalse();
    }

    [Fact]
    public void VersionMismatch_FactoryMethod_ShouldReturnConfiguredError()
    {
        Error errorWithoutActual = ConcurrencyErrors.VersionMismatch("inv-1", "Invoice", ExpectedVersion.Specific(5));
        errorWithoutActual.Code.Should().Be(ConcurrencyErrors.VersionMismatchCode);
        errorWithoutActual.Metadata!["entityId"].Should().Be("inv-1");
        errorWithoutActual.Metadata["entityType"].Should().Be("Invoice");
        errorWithoutActual.Metadata.ContainsKey("actualVersion").Should().BeFalse();

        Error errorWithActual = ConcurrencyErrors.VersionMismatch("inv-1", "Invoice", ExpectedVersion.Specific(5), ActualVersion.From(6));
        errorWithActual.Code.Should().Be(ConcurrencyErrors.VersionMismatchCode);
        errorWithActual.Metadata!["actualVersion"].Should().Be(ActualVersion.From(6).ToString());
    }

    [Fact]
    public void TokenMismatch_FactoryMethod_ShouldReturnConfiguredError()
    {
        var expToken = new ConcurrencyToken("tokenA", "Kind");
        var actToken = new ConcurrencyToken("tokenB", "Kind");

        Error errorWithoutActual = ConcurrencyErrors.TokenMismatch("doc-1", "Document", expToken);
        errorWithoutActual.Code.Should().Be(ConcurrencyErrors.TokenMismatchCode);
        errorWithoutActual.Metadata!["entityId"].Should().Be("doc-1");
        errorWithoutActual.Metadata["expectedToken"].Should().Be("tokenA");
        errorWithoutActual.Metadata.ContainsKey("actualToken").Should().BeFalse();

        Error errorWithActual = ConcurrencyErrors.TokenMismatch("doc-1", "Document", expToken, actToken);
        errorWithActual.Code.Should().Be(ConcurrencyErrors.TokenMismatchCode);
        errorWithActual.Metadata!["actualToken"].Should().Be("tokenB");
    }
}
