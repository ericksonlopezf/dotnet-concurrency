// Copyright © Erickson Lopez. MIT License.
using System;

namespace EricksonLopez.Concurrency.Abstractions;

/// <summary>
/// Represents an immutable, opaque concurrency token used to validate state consistency.
/// </summary>
public readonly record struct ConcurrencyToken : IConcurrencyToken, IComparable<ConcurrencyToken>, IComparable
{
    /// <summary>
    /// Represents an empty, uninitialized concurrency token.
    /// </summary>
    public static readonly ConcurrencyToken None = new(string.Empty, "None");

    /// <summary>
    /// Gets the raw string value of the token.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Gets the discriminator or kind of token (e.g. "String", "Guid", "RowVersion", "Hash").
    /// </summary>
    public string TokenKind { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrencyToken"/> struct with a specific value and kind.
    /// </summary>
    /// <param name="value">The string value of the token.</param>
    /// <param name="tokenKind">The descriptive kind of the token.</param>
    public ConcurrencyToken(string value, string tokenKind = "Opaque")
    {
        Value = value ?? string.Empty;
        TokenKind = string.IsNullOrWhiteSpace(tokenKind) ? "Opaque" : tokenKind;
    }

    /// <summary>
    /// Creates a new concurrency token from a <see cref="Guid"/>.
    /// </summary>
    /// <param name="tokenValue">The unique identifier value.</param>
    /// <returns>A new <see cref="ConcurrencyToken"/> formatted as a 32-character hexadecimal string without hyphens.</returns>
    public static ConcurrencyToken From(Guid tokenValue) => new(tokenValue.ToString("N"), "Guid");

    /// <summary>
    /// Creates a new concurrency token from a byte array.
    /// </summary>
    /// <param name="bytes">The byte array containing binary concurrency data (e.g., SQL rowversion).</param>
    /// <returns>A new <see cref="ConcurrencyToken"/> with hex-encoded string representation and kind "RowVersion".</returns>
    /// <exception cref="ArgumentNullException"><paramref name="bytes"/> is <see langword="null"/></exception>
    public static ConcurrencyToken From(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        return new(Convert.ToHexString(bytes), "RowVersion");
    }

    /// <summary>
    /// Creates a new concurrency token from a string.
    /// </summary>
    /// <param name="value">The string token value.</param>
    /// <param name="kind">The token kind discriminator.</param>
    /// <returns>A new <see cref="ConcurrencyToken"/> containing the specified string value and kind.</returns>
    public static ConcurrencyToken From(string value, string kind = "String") => new(value, kind);

    /// <summary>
    /// Generates a new cryptographically unique GUID-based concurrency token.
    /// </summary>
    /// <returns>A new unique <see cref="ConcurrencyToken"/>.</returns>
    public static ConcurrencyToken NewGuid() => From(Guid.NewGuid());

    /// <inheritdoc />
    public bool IsEmpty => string.IsNullOrEmpty(Value);

    /// <inheritdoc />
    public bool Equals(IConcurrencyToken? other)
    {
        if (other is null)
        {
            return false;
        }

        return string.Equals(Value, other.Value, StringComparison.Ordinal) &&
               string.Equals(TokenKind, other.TokenKind, StringComparison.Ordinal);
    }

    /// <inheritdoc />
    public int CompareTo(ConcurrencyToken other)
    {
        int valueComparison = string.Compare(Value, other.Value, StringComparison.Ordinal);
        if (valueComparison != 0)
        {
            return valueComparison;
        }

        return string.Compare(TokenKind, other.TokenKind, StringComparison.Ordinal);
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentException"><paramref name="obj"/> is not an instance of <see cref="ConcurrencyToken"/></exception>
    public int CompareTo(object? obj)
    {
        if (obj is null)
        {
            return 1;
        }

        if (obj is ConcurrencyToken other)
        {
            return CompareTo(other);
        }

        throw new ArgumentException($"Object must be of type {nameof(ConcurrencyToken)}.", nameof(obj));
    }

    /// <summary>
    /// Determines whether the left token is less than the right token.
    /// </summary>
    /// <param name="left">The left token to compare.</param>
    /// <param name="right">The right token to compare.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> is less than <paramref name="right"/>; otherwise, <see langword="false"/>.</returns>
    public static bool operator <(ConcurrencyToken left, ConcurrencyToken right) => left.CompareTo(right) < 0;

    /// <summary>
    /// Determines whether the left token is less than or equal to the right token.
    /// </summary>
    /// <param name="left">The left token to compare.</param>
    /// <param name="right">The right token to compare.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> is less than or equal to <paramref name="right"/>; otherwise, <see langword="false"/>.</returns>
    public static bool operator <=(ConcurrencyToken left, ConcurrencyToken right) => left.CompareTo(right) <= 0;

    /// <summary>
    /// Determines whether the left token is greater than the right token.
    /// </summary>
    /// <param name="left">The left token to compare.</param>
    /// <param name="right">The right token to compare.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> is greater than <paramref name="right"/>; otherwise, <see langword="false"/>.</returns>
    public static bool operator >(ConcurrencyToken left, ConcurrencyToken right) => left.CompareTo(right) > 0;

    /// <summary>
    /// Determines whether the left token is greater than or equal to the right token.
    /// </summary>
    /// <param name="left">The left token to compare.</param>
    /// <param name="right">The right token to compare.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> is greater than or equal to <paramref name="right"/>; otherwise, <see langword="false"/>.</returns>
    public static bool operator >=(ConcurrencyToken left, ConcurrencyToken right) => left.CompareTo(right) >= 0;

    /// <inheritdoc />
    public override string ToString() => IsEmpty ? "[EmptyToken]" : $"{TokenKind}:{Value}";
}
