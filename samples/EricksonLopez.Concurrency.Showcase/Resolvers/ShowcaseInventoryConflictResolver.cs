// Copyright © Erickson Lopez. MIT License.
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.Concurrency.Abstractions;
using EricksonLopez.Concurrency.Showcase.Models;

namespace EricksonLopez.Concurrency.Showcase.Resolvers;

/// <summary>
/// Custom conflict resolver demonstrating domain-specific conflict resolution registered via DI.
/// </summary>
public sealed class ShowcaseInventoryConflictResolver : IConcurrencyConflictResolver<ProductInventory>
{
    public ValueTask<ConflictResolution<ProductInventory>> ResolveAsync(
        ProductInventory proposedEntity,
        ProductInventory? currentDatabaseEntity,
        ConcurrencyConflict conflict,
        CancellationToken cancellationToken = default)
    {
        if (currentDatabaseEntity is null)
        {
            return ValueTask.FromResult(ConflictResolution.Rejected<ProductInventory>("The product no longer exists in the catalog."));
        }

        // Domain merge rule: accumulate proposed reserved stock if database has sufficient available stock
        if (currentDatabaseEntity.AvailableStock >= proposedEntity.ReservedStock)
        {
            var merged = new ProductInventory(
                proposedEntity.Id,
                proposedEntity.Sku,
                currentDatabaseEntity.AvailableStock - proposedEntity.ReservedStock,
                currentDatabaseEntity.ReservedStock + proposedEntity.ReservedStock,
                currentDatabaseEntity.Version + 1);

            return ValueTask.FromResult(ConflictResolution.Merged(merged, "Reserved stock automatically reconciled with database state."));
        }

        return ValueTask.FromResult(ConflictResolution.Rejected<ProductInventory>("Insufficient stock during concurrent reconciliation."));
    }
}
