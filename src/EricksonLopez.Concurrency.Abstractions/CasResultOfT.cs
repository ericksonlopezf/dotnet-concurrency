// Copyright © Erickson Lopez. MIT License.

namespace EricksonLopez.Concurrency.Abstractions;

/// <summary>
/// Represents the result of an atomic or optimistic Compare-And-Swap (CAS) state transition.
/// </summary>
/// <remarks>
/// This struct indicates either success with a mutated entity and version, or conflict with a populated conflict descriptor.
/// </remarks>
/// <typeparam name="TEntity">The type of the target entity or aggregate root.</typeparam>
public readonly record struct CasResult<TEntity>
    where TEntity : class
{
    /// <summary>
    /// Gets a value indicating whether the Compare-And-Swap operation succeeded.
    /// </summary>
    public bool IsSuccess => Conflict is null && Entity is not null;

    /// <summary>
    /// Gets a value indicating whether a concurrency conflict prevented the state transition.
    /// </summary>
    public bool IsConflict => Conflict is not null;

    /// <summary>
    /// Gets the mutated entity instance if successful; otherwise, <see langword="null"/>.
    /// </summary>
    public TEntity? Entity { get; }

    /// <summary>
    /// Gets the resulting new concurrency version if successful; otherwise, <see langword="null"/>.
    /// </summary>
    public ConcurrencyVersion? NewVersion { get; }

    /// <summary>
    /// Gets the conflict descriptor if the operation failed due to a state mismatch; otherwise, <see langword="null"/>.
    /// </summary>
    public ConcurrencyConflict? Conflict { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CasResult{TEntity}"/> struct.
    /// </summary>
    /// <param name="entity">The mutated entity instance when successful; otherwise, <see langword="null"/>.</param>
    /// <param name="newVersion">The new concurrency version when successful; otherwise, <see langword="null"/>.</param>
    /// <param name="conflict">The conflict descriptor when conflicted; otherwise, <see langword="null"/>.</param>
    public CasResult(TEntity? entity, ConcurrencyVersion? newVersion, ConcurrencyConflict? conflict)
    {
        Entity = entity;
        NewVersion = newVersion;
        Conflict = conflict;
    }
}
