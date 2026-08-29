// Copyright © Erickson Lopez. MIT License.

namespace EricksonLopez.Concurrency.Abstractions;

/// <summary>
/// Specifies the expected version semantics for an optimistic concurrency operation.
/// </summary>
public enum ExpectedVersionKind : byte
{
    /// <summary>Matches an exact, specific numeric version.</summary>
    Specific = 0,
    /// <summary>Matches any version (disables optimistic version mismatch check).</summary>
    Any = 1,
    /// <summary>Expects that the target entity does not yet exist (version 0 / None).</summary>
    New = 2,
    /// <summary>Expects that the target entity exists with any non-zero version.</summary>
    Exists = 3
}
