// Copyright © Erickson Lopez. MIT License.
using System;
using AwesomeAssertions;
using EricksonLopez.Concurrency.Abstractions;
using EricksonLopez.Concurrency.Testing;
using Xunit;

namespace EricksonLopez.Concurrency.Testing.Tests;

public sealed class ConcurrencyConflictBuilderTests
{
    private sealed record StubToken(string Value) : IConcurrencyToken
    {
        public string TokenKind => "Stub";
        public bool IsEmpty => string.IsNullOrEmpty(Value);
        public bool Equals(IConcurrencyToken? other) => other is not null && Value == other.Value;
    }

    [Fact]
    public void Build_DefaultConfiguration_ShouldPopulateDefaults()
    {
        var builder = new ConcurrencyConflictBuilder();
        ConcurrencyConflict conflict = builder.Build();

        conflict.EntityId.Should().Be("test-entity-1");
        conflict.EntityType.Should().Be("TestAggregate");
        conflict.ConflictType.Should().Be(ConcurrencyConflictType.VersionMismatch);
        conflict.Classification.Should().Be(ConcurrencyConflictClassification.Transient);
        conflict.Operation.Should().Be("Update");
        conflict.Message.Should().Be("Optimistic concurrency conflict detected during testing.");
        conflict.ExpectedVersion.Should().Be(ExpectedVersion.Specific(1));
        conflict.ActualVersion.Should().Be(ActualVersion.From(2));
        conflict.ExpectedToken.Should().BeNull();
        conflict.ActualToken.Should().BeNull();
        conflict.Metadata.Should().BeEmpty();
    }

    [Fact]
    public void Builder_FluentMethods_ShouldConfigureAllProperties()
    {
        var expectedToken = new StubToken("token-exp");
        var actualToken = new StubToken("token-act");

        ConcurrencyConflict conflict = new ConcurrencyConflictBuilder()
            .WithEntityId("custom-id")
            .WithEntityType("CustomEntity")
            .WithConflictType(ConcurrencyConflictType.Deadlock)
            .WithClassification(ConcurrencyConflictClassification.StaleState)
            .WithOperation("Delete")
            .WithMessage("Custom message")
            .WithVersions(ExpectedVersion.Specific(10), ActualVersion.From(15))
            .WithTokens(expectedToken, actualToken)
            .WithMetadata("key1", "val1")
            .WithMetadata("key2", "val2")
            .Build();

        conflict.EntityId.Should().Be("custom-id");
        conflict.EntityType.Should().Be("CustomEntity");
        conflict.ConflictType.Should().Be(ConcurrencyConflictType.Deadlock);
        conflict.Classification.Should().Be(ConcurrencyConflictClassification.StaleState);
        conflict.Operation.Should().Be("Delete");
        conflict.Message.Should().Be("Custom message");
        conflict.ExpectedVersion.Should().Be(ExpectedVersion.Specific(10));
        conflict.ActualVersion.Should().Be(ActualVersion.From(15));
        conflict.ExpectedToken.Should().Be(expectedToken);
        conflict.ActualToken.Should().Be(actualToken);
        conflict.Metadata.Should().NotBeNull();
        conflict.Metadata!["key1"].Should().Be("val1");
        conflict.Metadata!["key2"].Should().Be("val2");
    }

    [Fact]
    public void ImplicitOperator_ValidBuilder_ShouldBuildConflict()
    {
        var builder = new ConcurrencyConflictBuilder().WithEntityId("imp-1");
        ConcurrencyConflict conflict = builder;

        conflict.Should().NotBeNull();
        conflict.EntityId.Should().Be("imp-1");
    }

    [Fact]
    public void ImplicitOperator_NullBuilder_ShouldThrowArgumentNullException()
    {
        ConcurrencyConflictBuilder nullBuilder = null!;
        Action act = () =>
        {
            ConcurrencyConflict conflict = nullBuilder;
            _ = conflict;
        };

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("builder");
    }

    [Fact]
    public void NullGuards_AllSetterMethods_ShouldThrowArgumentNullException()
    {
        var builder = new ConcurrencyConflictBuilder();

        Action actEntityId = () => builder.WithEntityId(null!);
        actEntityId.Should().Throw<ArgumentNullException>().WithParameterName("entityId");

        Action actEntityType = () => builder.WithEntityType(null!);
        actEntityType.Should().Throw<ArgumentNullException>().WithParameterName("entityType");

        Action actOp = () => builder.WithOperation(null!);
        actOp.Should().Throw<ArgumentNullException>().WithParameterName("operation");

        Action actMsg = () => builder.WithMessage(null!);
        actMsg.Should().Throw<ArgumentNullException>().WithParameterName("message");

        Action actMetaKey = () => builder.WithMetadata(null!, "val");
        actMetaKey.Should().Throw<ArgumentNullException>().WithParameterName("key");

        Action actMetaVal = () => builder.WithMetadata("key", null!);
        actMetaVal.Should().Throw<ArgumentNullException>().WithParameterName("value");
    }
}
