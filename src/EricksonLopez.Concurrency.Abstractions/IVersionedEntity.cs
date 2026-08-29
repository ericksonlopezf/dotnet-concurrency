// Copyright © Erickson Lopez. MIT License.

namespace EricksonLopez.Concurrency.Abstractions;

/// <summary>
/// Defines a contract for domain entities or aggregates that maintain an explicit numeric concurrency version.
/// </summary>
public interface IVersionedEntity
{
    /// <summary>
    /// Gets the current numeric concurrency version of the entity or aggregate.
    /// </summary>
    long Version { get; }
}
