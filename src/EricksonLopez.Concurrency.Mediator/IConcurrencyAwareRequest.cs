// Copyright © Erickson Lopez. MIT License.
using EricksonLopez.Concurrency.Abstractions;

namespace EricksonLopez.Concurrency.Mediator;

/// <summary>
/// Defines a contract for mediator commands or requests that declare explicit optimistic concurrency constraints.
/// </summary>
public interface IConcurrencyAwareRequest
{
    /// <summary>
    /// Gets the expected numeric version for optimistic verification, if specified.
    /// </summary>
    ExpectedVersion? ExpectedVersion => null;

    /// <summary>
    /// Gets the expected concurrency token for optimistic verification, if specified.
    /// </summary>
    IConcurrencyToken? ConcurrencyToken => null;
}
