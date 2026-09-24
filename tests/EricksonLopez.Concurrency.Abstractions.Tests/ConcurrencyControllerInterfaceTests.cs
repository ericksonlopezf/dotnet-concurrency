// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.Concurrency.Abstractions;
using Xunit;

namespace EricksonLopez.Concurrency.Abstractions.Tests;

public sealed class ConcurrencyControllerInterfaceTests
{
    private sealed class TestEntity : IVersionedEntity
    {
        public long Version { get; set; } = 1;
    }

    private sealed class StubController : IConcurrencyController
    {
        public ConcurrencyConflict? VerifyVersion<TEntity>(TEntity entity, ExpectedVersion expected, string entityId) where TEntity : class, IVersionedEntity => null;
        public ConcurrencyConflict? VerifyToken<TEntity>(TEntity entity, IConcurrencyToken expected, string entityId) where TEntity : class, IConcurrencyAware => null;
        public async ValueTask<CasResult<TEntity>> ExecuteCasAsync<TEntity>(
            TEntity entity,
            ExpectedVersion expected,
            string entityId,
            Func<TEntity, CancellationToken, ValueTask<TEntity>> mutate,
            CancellationToken cancellationToken = default) where TEntity : class, IVersionedEntity
        {
            var mutated = await mutate(entity, cancellationToken);
            return CasResult.Succeeded(mutated, new ConcurrencyVersion(entity.Version + 1));
        }
    }

    [Fact]
    public async Task IConcurrencyController_SyncMutateOverload_ShouldDelegateToAsyncOverload()
    {
        IConcurrencyController controller = new StubController();
        var entity = new TestEntity { Version = 10 };

        CasResult<TestEntity> result = await controller.ExecuteCasAsync(
            entity,
            ExpectedVersion.Specific(10),
            "id-1",
            (e, ct) => { e.Version = 11; return e; });

        result.IsSuccess.Should().BeTrue();
        result.Entity.Should().NotBeNull();
        result.Entity!.Version.Should().Be(11);
        result.NewVersion.Should().Be(new ConcurrencyVersion(12));
    }
}
