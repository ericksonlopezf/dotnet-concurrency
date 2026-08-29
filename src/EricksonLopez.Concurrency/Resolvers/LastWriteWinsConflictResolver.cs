// Copyright © Erickson Lopez. MIT License.
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.Concurrency.Abstractions;
using EricksonLopez.Concurrency.Diagnostics;

namespace EricksonLopez.Concurrency.Resolvers;

/// <summary>
/// Resolves optimistic concurrency conflicts by explicitly overwriting storage with the local proposed state.
/// </summary>
/// <remarks>
/// Use this resolver with caution as it unconditionally overwrites concurrent modifications without domain inspection.
/// </remarks>
/// <typeparam name="TEntity">The type of the domain entity or aggregate root.</typeparam>
public sealed class LastWriteWinsConflictResolver<TEntity> : IConcurrencyConflictResolver<TEntity>
    where TEntity : class
{
    /// <summary>
    /// Gets the shared singleton instance of <see cref="LastWriteWinsConflictResolver{TEntity}"/>.
    /// </summary>
    public static readonly LastWriteWinsConflictResolver<TEntity> Instance = new();

    /// <inheritdoc />
    public ValueTask<ConflictResolution<TEntity>> ResolveAsync(
        TEntity proposedEntity,
        TEntity? currentDatabaseEntity,
        ConcurrencyConflict conflict,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(proposedEntity);

        ConcurrencyDiagnostics.RecordMerge(
            Activity.Current,
            typeof(TEntity).Name,
            nameof(ConflictResolutionStrategy.LastWriteWinsExplicit));

        return ValueTask.FromResult(ConflictResolution.LastWriteWins(proposedEntity, "Explicit Last-Write-Wins overwrite strategy applied."));
    }
}
