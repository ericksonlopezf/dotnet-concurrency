// Copyright © Erickson Lopez. MIT License.

namespace EricksonLopez.Concurrency.Abstractions;

/// <summary>
/// Defines a contract for domain entities, aggregates, or DTOs that encapsulate a concurrency token.
/// </summary>
public interface IConcurrencyAware
{
    /// <summary>
    /// Gets the concurrency token associated with this instance.
    /// </summary>
    IConcurrencyToken ConcurrencyToken { get; }
}
