// Copyright © Erickson Lopez. MIT License.

namespace EricksonLopez.Concurrency.Abstractions;

/// <summary>
/// Defines a strongly-typed contract for domain entities or aggregates that maintain a typed concurrency version.
/// </summary>
/// <typeparam name="TEntity">The target entity or aggregate type.</typeparam>
public interface IVersionedEntity<TEntity> : IVersionedEntity
    where TEntity : class
{
    /// <summary>
    /// Gets the strongly-typed concurrency version of this entity.
    /// </summary>
    ConcurrencyVersion<TEntity> TypedVersion => new(Version);
}
