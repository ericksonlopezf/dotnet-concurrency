// Copyright © Erickson Lopez. MIT License.

namespace EricksonLopez.Concurrency.Abstractions;

/// <summary>
/// Defines a contract for domain entities or aggregates whose numeric concurrency version can be mutated by the concurrency controller.
/// </summary>
public interface IMutableVersionedEntity : IVersionedEntity
{
    /// <summary>
    /// Gets or sets the current numeric concurrency version of the entity or aggregate.
    /// </summary>
    new long Version { get; set; }
}
