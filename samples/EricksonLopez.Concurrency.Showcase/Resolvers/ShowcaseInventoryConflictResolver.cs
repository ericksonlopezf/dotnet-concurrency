// Copyright © Erickson Lopez. MIT License.
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.Concurrency.Abstractions;
using EricksonLopez.Concurrency.Showcase.Models;

namespace EricksonLopez.Concurrency.Showcase.Resolvers;

/// <summary>
/// Provides domain-specific conflict resolution for <see cref="ProductInventory"/> entities.
/// </summary>
public sealed class ShowcaseInventoryConflictResolver : IConcurrencyConflictResolver<ProductInventory>
{
    /// <summary>
    /// Resolves a concurrency conflict for a product inventory item by reconciling available and reserved stock.
    /// </summary>
    /// <param name="proposedEntity">The proposed product inventory state.</param>
    /// <param name="currentDatabaseEntity">The current database entity state, or <see langword="null"/> if not found.</param>
    /// <param name="conflict">The detected concurrency conflict.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A value task representing the asynchronous operation. The task result contains the <see cref="ConflictResolution{TEntity}"/> outcome.</returns>
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
