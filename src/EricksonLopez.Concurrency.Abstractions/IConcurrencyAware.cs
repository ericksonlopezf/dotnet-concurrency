// Copyright © Erickson Lopez. MIT License.

namespace EricksonLopez.Concurrency.Abstractions;

/// <summary>
/// Defines a contract for domain entities, aggregates, or DTOs that encapsulate a concurrency token.
/// </summary>
public interface IConcurrencyAware
{
    /// <summary>
    /// Gets the concurrency token associated with this instance, or <see langword="null"/> if no token has been assigned.
    /// </summary>
    /// <remarks>
    /// When <see langword="null"/>, the default concurrency controller implementation treats this as an empty token (<see cref="ConcurrencyToken.None"/>).
    /// </remarks>
    IConcurrencyToken? ConcurrencyToken { get; }
}
