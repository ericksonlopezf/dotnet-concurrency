// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.Concurrency.Abstractions;
using EricksonLopez.Concurrency.Testing;
using Xunit;

namespace EricksonLopez.Concurrency.Testing.Tests;

public sealed class FakeConcurrencyControllerTests
{
    private sealed class OrderAggregate : IVersionedEntity, IConcurrencyAware
    {
        public string Id { get; init; } = string.Empty;
        public decimal Total { get; set; }
        public long Version { get; set; }
        public IConcurrencyToken ConcurrencyToken => new StubToken(Version.ToString());
    }

    private sealed record StubToken(string Value) : IConcurrencyToken
    {
        public string TokenKind => "Stub";
        public bool IsEmpty => string.IsNullOrEmpty(Value);
        public bool Equals(IConcurrencyToken? other) => other is not null && Value == other.Value;
    }

    [Fact]
    public void VerifyVersion_DefaultBehavior_ShouldReturnNull_AndRecordCall()
    {
        var fake = new FakeConcurrencyController();
        var order = new OrderAggregate { Id = "ord-1", Version = 5 };

        ConcurrencyConflict? conflict = fake.VerifyVersion(order, ExpectedVersion.Specific(5), "ord-1");

        conflict.Should().BeNull();
        fake.VerifyVersionInvocations.Should().HaveCount(1);
        VerifyVersionInvocation invocation = fake.VerifyVersionInvocations[0];
        invocation.Entity.Should().BeSameAs(order);
        invocation.EntityId.Should().Be("ord-1");
        invocation.Expected.Should().Be(ExpectedVersion.Specific(5));
        invocation.Timestamp.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        fake.TotalInvocations.Should().Be(1);
    }

    [Fact]
    public void VerifyVersion_NullEntity_ShouldThrowArgumentNullException()
    {
        var fake = new FakeConcurrencyController();
        Action act = () => fake.VerifyVersion<OrderAggregate>(null!, ExpectedVersion.Specific(1), "ord-1");

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("entity");
    }

    [Fact]
    public void VerifyToken_DefaultBehavior_ShouldReturnNull_AndRecordCall()
    {
        var fake = new FakeConcurrencyController();
        var order = new OrderAggregate { Id = "ord-1", Version = 5 };
        var token = new StubToken("5");

        ConcurrencyConflict? conflict = fake.VerifyToken(order, token, "ord-1");

        conflict.Should().BeNull();
        fake.VerifyTokenInvocations.Should().HaveCount(1);
        VerifyTokenInvocation invocation = fake.VerifyTokenInvocations[0];
        invocation.Entity.Should().BeSameAs(order);
        invocation.Expected.Should().Be(token);
        invocation.EntityId.Should().Be("ord-1");
        invocation.Timestamp.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        fake.TotalInvocations.Should().Be(1);
    }

    [Fact]
    public void VerifyToken_NullArguments_ShouldThrowArgumentNullException()
    {
        var fake = new FakeConcurrencyController();
        var order = new OrderAggregate { Id = "ord-1", Version = 5 };
        var token = new StubToken("5");

        Action actNullEntity = () => fake.VerifyToken<OrderAggregate>(null!, token, "ord-1");
        actNullEntity.Should().Throw<ArgumentNullException>().WithParameterName("entity");

        Action actNullToken = () => fake.VerifyToken(order, null!, "ord-1");
        actNullToken.Should().Throw<ArgumentNullException>().WithParameterName("expected");
    }

    [Fact]
    public async Task ExecuteCasAsync_DefaultBehavior_ShouldMutateAndIncrementVersion()
    {
        var fake = new FakeConcurrencyController();
        var order = new OrderAggregate { Id = "ord-1", Total = 100, Version = 5 };

        CasResult<OrderAggregate> result = await fake.ExecuteCasAsync(
            order,
            ExpectedVersion.Specific(5),
            "ord-1",
            (entity, ct) =>
            {
                entity.Total = 150;
                return ValueTask.FromResult(entity);
            },
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.IsConflict.Should().BeFalse();
        result.Entity.Should().NotBeNull();
        result.Entity!.Total.Should().Be(150);
        result.NewVersion.Should().Be(ConcurrencyVersion.From(6));
        fake.ExecuteCasInvocations.Should().HaveCount(1);
        ExecuteCasInvocation invocation = fake.ExecuteCasInvocations[0];
        invocation.Entity.Should().BeSameAs(order);
        invocation.Expected.Should().Be(ExpectedVersion.Specific(5));
        invocation.EntityId.Should().Be("ord-1");
        invocation.Timestamp.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task ExecuteCasAsync_NullArguments_ShouldThrowArgumentNullException()
    {
        var fake = new FakeConcurrencyController();
        var order = new OrderAggregate { Id = "ord-1", Version = 1 };

        Func<Task> actNullEntity = async () => await fake.ExecuteCasAsync<OrderAggregate>(
            null!,
            ExpectedVersion.Specific(1),
            "ord-1",
            (e, ct) => ValueTask.FromResult(e));
        await actNullEntity.Should().ThrowAsync<ArgumentNullException>().WithParameterName("entity");

        Func<Task> actNullMutate = async () => await fake.ExecuteCasAsync(
            order,
            ExpectedVersion.Specific(1),
            "ord-1",
            null!);
        await actNullMutate.Should().ThrowAsync<ArgumentNullException>().WithParameterName("mutate");
    }

    [Fact]
    public async Task ExecuteCasAsync_CancellationTokenCanceled_ShouldThrowOperationCanceledException()
    {
        var fake = new FakeConcurrencyController();
        var order = new OrderAggregate { Id = "ord-1", Version = 1 };
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Func<Task> act = async () => await fake.ExecuteCasAsync(
            order,
            ExpectedVersion.Specific(1),
            "ord-1",
            (e, ct) => ValueTask.FromResult(e),
            cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task ExecuteCasAsync_WithFixedSuccessVersion_ShouldReturnConfiguredVersion()
    {
        var fake = new FakeConcurrencyController();
        fake.WithSuccess(42);

        var order = new OrderAggregate { Id = "ord-1", Version = 5 };

        CasResult<OrderAggregate> result = await fake.ExecuteCasAsync(
            order,
            ExpectedVersion.Specific(5),
            "ord-1",
            (e, ct) => ValueTask.FromResult(e));

        result.IsSuccess.Should().BeTrue();
        result.NewVersion.Should().Be(ConcurrencyVersion.From(42));
    }

    [Fact]
    public async Task ExecuteCasAsync_WithSuccessOnNextWrite_ShouldUseQueuedVersionThenFallback()
    {
        var fake = new FakeConcurrencyController();
        fake.WithSuccessOnNextWrite(100);

        var order = new OrderAggregate { Id = "ord-1", Version = 5 };

        CasResult<OrderAggregate> result1 = await fake.ExecuteCasAsync(
            order,
            ExpectedVersion.Specific(5),
            "ord-1",
            (e, ct) => ValueTask.FromResult(e));

        result1.NewVersion.Should().Be(ConcurrencyVersion.From(100));

        CasResult<OrderAggregate> result2 = await fake.ExecuteCasAsync(
            order,
            ExpectedVersion.Specific(5),
            "ord-1",
            (e, ct) => ValueTask.FromResult(e));

        result2.NewVersion.Should().Be(ConcurrencyVersion.From(6));
    }

    [Fact]
    public async Task ExecuteCasAsync_WithSuccessOnNextWrite_NullVersion_ShouldFallbackToIncrement()
    {
        var fake = new FakeConcurrencyController();
        fake.WithSuccessOnNextWrite(null);

        var order = new OrderAggregate { Id = "ord-1", Version = 10 };

        CasResult<OrderAggregate> result = await fake.ExecuteCasAsync(
            order,
            ExpectedVersion.Specific(10),
            "ord-1",
            (e, ct) => ValueTask.FromResult(e));

        result.IsSuccess.Should().BeTrue();
        result.NewVersion.Should().Be(ConcurrencyVersion.From(11));
    }

    [Fact]
    public async Task ExecuteCasAsync_WithFixedConflict_ShouldReturnConflictedResult()
    {
        var fake = new FakeConcurrencyController();
        var conflict = new ConcurrencyConflict("ord-1", "OrderAggregate", ConcurrencyConflictType.Deadlock, ConcurrencyConflictClassification.Transient, "TestOp", "Deadlock detected");
        fake.WithConflict(conflict);

        var order = new OrderAggregate { Id = "ord-1", Version = 5 };

        CasResult<OrderAggregate> result = await fake.ExecuteCasAsync(
            order,
            ExpectedVersion.Specific(5),
            "ord-1",
            (e, ct) => ValueTask.FromResult(e));

        result.IsSuccess.Should().BeFalse();
        result.IsConflict.Should().BeTrue();
        result.Conflict.Should().Be(conflict);
    }

    [Fact]
    public async Task ExecuteCasAsync_WithQueuedConflict_ShouldReturnConflictOnceThenSucceed()
    {
        var fake = new FakeConcurrencyController();
        var conflict = new ConcurrencyConflict("ord-1", "OrderAggregate", ConcurrencyConflictType.Deadlock, ConcurrencyConflictClassification.Transient, "TestOp", "Deadlock detected");
        fake.WithConflictOnNextWrite(conflict);

        var order = new OrderAggregate { Id = "ord-1", Version = 5 };

        CasResult<OrderAggregate> result1 = await fake.ExecuteCasAsync(
            order,
            ExpectedVersion.Specific(5),
            "ord-1",
            (e, ct) => ValueTask.FromResult(e));

        result1.IsConflict.Should().BeTrue();
        result1.Conflict.Should().Be(conflict);

        CasResult<OrderAggregate> result2 = await fake.ExecuteCasAsync(
            order,
            ExpectedVersion.Specific(5),
            "ord-1",
            (e, ct) => ValueTask.FromResult(e));

        result2.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void WithConflict_Null_ShouldThrowArgumentNullException()
    {
        var fake = new FakeConcurrencyController();
        Action act = () => fake.WithConflict(null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("conflict");
    }

    [Fact]
    public void WithConflictOnNextWrite_Null_ShouldThrowArgumentNullException()
    {
        var fake = new FakeConcurrencyController();
        Action act = () => fake.WithConflictOnNextWrite(null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("conflict");
    }

    [Fact]
    public void WithConflict_Synthesized_ShouldReturnConfiguredSynthesizedConflict()
    {
        var fake = new FakeConcurrencyController();
        fake.WithConflict(
            ConcurrencyConflictType.Deadlock,
            "ent-1",
            "EntityCustom",
            ConcurrencyConflictClassification.Transient);

        var order = new OrderAggregate { Id = "ord-1", Version = 1 };
        ConcurrencyConflict? conflict = fake.VerifyVersion(order, ExpectedVersion.Specific(1), "ord-1");

        conflict.Should().NotBeNull();
        conflict!.EntityId.Should().Be("ent-1");
        conflict.EntityType.Should().Be("EntityCustom");
        conflict.ConflictType.Should().Be(ConcurrencyConflictType.Deadlock);
        conflict.Classification.Should().Be(ConcurrencyConflictClassification.Transient);
        conflict.Operation.Should().Be("FakeOperation");
        conflict.Message.Should().Be("Simulated Deadlock conflict.");

        // With default parameters
        fake.WithConflict(ConcurrencyConflictType.SerializationFailure);
        ConcurrencyConflict? conflictDefaults = fake.VerifyVersion(order, ExpectedVersion.Specific(1), "ord-1");
        conflictDefaults!.EntityId.Should().Be("test-entity-id");
        conflictDefaults.EntityType.Should().Be("TestEntity");
        conflictDefaults.ConflictType.Should().Be(ConcurrencyConflictType.SerializationFailure);
        conflictDefaults.Operation.Should().Be("FakeOperation");
        conflictDefaults.Message.Should().Be("Simulated SerializationFailure conflict.");
    }

    [Fact]
    public void WithConflictOnNextWrite_Synthesized_ShouldReturnQueuedSynthesizedConflict()
    {
        var fake = new FakeConcurrencyController();
        fake.WithConflictOnNextWrite(
            ConcurrencyConflictType.TokenMismatch,
            "ent-2",
            "EntityTwo",
            ConcurrencyConflictClassification.StaleState);

        var order = new OrderAggregate { Id = "ord-1", Version = 1 };
        ConcurrencyConflict? conflict1 = fake.VerifyVersion(order, ExpectedVersion.Specific(1), "ord-1");

        conflict1.Should().NotBeNull();
        conflict1!.EntityId.Should().Be("ent-2");
        conflict1.EntityType.Should().Be("EntityTwo");
        conflict1.ConflictType.Should().Be(ConcurrencyConflictType.TokenMismatch);
        conflict1.Classification.Should().Be(ConcurrencyConflictClassification.StaleState);
        conflict1.Operation.Should().Be("FakeOperation");
        conflict1.Message.Should().Be("Simulated TokenMismatch conflict.");

        ConcurrencyConflict? conflict2 = fake.VerifyVersion(order, ExpectedVersion.Specific(1), "ord-1");
        conflict2.Should().BeNull();

        // Default synthesized on next write
        fake.WithConflictOnNextWrite(ConcurrencyConflictType.VersionMismatch);
        ConcurrencyConflict? conflict3 = fake.VerifyVersion(order, ExpectedVersion.Specific(1), "ord-1");
        conflict3!.EntityId.Should().Be("test-entity-id");
        conflict3.EntityType.Should().Be("TestEntity");
        conflict3.Operation.Should().Be("FakeOperation");
        conflict3.Message.Should().Be("Simulated VersionMismatch conflict.");
    }

    [Fact]
    public void WhenVerifyVersion_CustomHandler_ShouldEvaluateCustomLogic()
    {
        var fake = new FakeConcurrencyController();
        var order = new OrderAggregate { Id = "ord-1", Version = 3 };

        fake.WhenVerifyVersion((entity, expected, id) =>
        {
            if (id == "special-fail")
            {
                return ConcurrencyConflict.VersionMismatch(id, "OrderAggregate", expected, ActualVersion.From(99));
            }
            return null;
        });

        fake.VerifyVersion(order, ExpectedVersion.Specific(3), "normal").Should().BeNull();
        fake.VerifyVersion(order, ExpectedVersion.Specific(3), "special-fail").Should().NotBeNull();
    }

    [Fact]
    public void WhenVerifyToken_CustomHandler_ShouldEvaluateCustomLogic()
    {
        var fake = new FakeConcurrencyController();
        var order = new OrderAggregate { Id = "ord-1", Version = 3 };
        var token = new StubToken("v3");

        fake.WhenVerifyToken((entity, expected, id) =>
        {
            if (id == "token-fail")
            {
                return ConcurrencyConflict.TokenMismatch(id, "OrderAggregate", expected, new StubToken("actual-v99"));
            }
            return null;
        });

        fake.VerifyToken(order, token, "normal").Should().BeNull();
        fake.VerifyToken(order, token, "token-fail").Should().NotBeNull();
    }

    [Fact]
    public void VerifyToken_WithQueuedAndFixedConflict_ShouldReturnConflict()
    {
        var fake = new FakeConcurrencyController();
        var order = new OrderAggregate { Id = "ord-1", Version = 1 };
        var token = new StubToken("1");
        var conflict = new ConcurrencyConflict("ord-1", "OrderAggregate", ConcurrencyConflictType.Deadlock, ConcurrencyConflictClassification.Transient, "TestOp", "Deadlock detected");

        fake.WithConflictOnNextWrite(conflict);
        fake.VerifyToken(order, token, "ord-1").Should().Be(conflict);
        fake.VerifyToken(order, token, "ord-1").Should().BeNull();

        fake.WithConflict(conflict);
        fake.VerifyToken(order, token, "ord-1").Should().Be(conflict);
    }

    [Fact]
    public async Task Reset_ShouldClearAllInvocationsAndConfiguredOutcomes()
    {
        var fake = new FakeConcurrencyController();
        var order = new OrderAggregate { Id = "ord-1", Version = 1 };
        var token = new StubToken("1");

        // Record invocations on all three operations
        fake.VerifyVersion(order, ExpectedVersion.Specific(1), "ord-1");
        fake.VerifyToken(order, token, "ord-1");
        await fake.ExecuteCasAsync(order, ExpectedVersion.Specific(1), "ord-1", (e, ct) => ValueTask.FromResult(e));

        fake.VerifyVersionInvocations.Should().HaveCount(1);
        fake.VerifyTokenInvocations.Should().HaveCount(1);
        fake.ExecuteCasInvocations.Should().HaveCount(1);
        fake.TotalInvocations.Should().Be(3);

        // Enqueue multiple items in both queues and configure fixed conflict/handlers
        fake.WithConflict(ConcurrencyConflictType.TokenMismatch);
        fake.WithConflictOnNextWrite(ConcurrencyConflictType.Deadlock);
        fake.WithConflictOnNextWrite(ConcurrencyConflictType.Custom);
        fake.WithSuccessOnNextWrite(99);
        fake.WithSuccessOnNextWrite(100);
        fake.WhenVerifyVersion((e, v, id) => null);
        fake.WhenVerifyToken((e, t, id) => null);

        fake.Reset();

        fake.TotalInvocations.Should().Be(0);
        fake.VerifyVersionInvocations.Should().BeEmpty();
        fake.VerifyTokenInvocations.Should().BeEmpty();
        fake.ExecuteCasInvocations.Should().BeEmpty();

        // Verify that queues and fixed conflicts were completely flushed
        fake.VerifyVersion(order, ExpectedVersion.Specific(1), "ord-1").Should().BeNull();
        fake.VerifyVersion(order, ExpectedVersion.Specific(1), "ord-1").Should().BeNull();

        CasResult<OrderAggregate> casResult = await fake.ExecuteCasAsync(
            order,
            ExpectedVersion.Specific(1),
            "ord-1",
            (e, ct) => ValueTask.FromResult(e));
        casResult.IsSuccess.Should().BeTrue();
        casResult.NewVersion.Should().Be(ConcurrencyVersion.From(2));
    }
}
