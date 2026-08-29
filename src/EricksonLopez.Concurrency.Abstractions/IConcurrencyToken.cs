// Copyright © Erickson Lopez. MIT License.
using System;

namespace EricksonLopez.Concurrency.Abstractions;

/// <summary>
/// Defines a contract for an immutable concurrency token utilized in optimistic concurrency validation.
/// </summary>
public interface IConcurrencyToken : IEquatable<IConcurrencyToken>
{
    /// <summary>
    /// Gets the raw string representation of the concurrency token.
    /// </summary>
    string Value { get; }

    /// <summary>
    /// Gets an opaque discriminator or type tag representing the token format (e.g., "RowVersion", "ETag", "Xmin", "Hash").
    /// </summary>
    string TokenKind { get; }

    /// <summary>
    /// Gets a value indicating whether the token represents an empty or uninitialized state.
    /// </summary>
    bool IsEmpty { get; }
}
