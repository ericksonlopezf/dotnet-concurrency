// Copyright © Erickson Lopez. MIT License.
using System;
using System.Globalization;
using EricksonLopez.Concurrency.Abstractions;

namespace EricksonLopez.Concurrency.PostgreSql;

/// <summary>
/// Represents an immutable concurrency token wrapping the native PostgreSQL system column <c>xmin</c> (32-bit transaction ID).
/// </summary>
public readonly record struct XminConcurrencyToken : IConcurrencyToken, IComparable<XminConcurrencyToken>, IComparable, ISpanFormattable
{
    /// <summary>
    /// Represents an uninitialized or empty xmin token (0).
    /// </summary>
    public static readonly XminConcurrencyToken None = new(0);

    /// <summary>
    /// Gets the underlying 32-bit PostgreSQL transaction ID.
    /// </summary>
    public uint Xmin { get; }

    /// <inheritdoc />
    public string Value => Xmin.ToString(CultureInfo.InvariantCulture);

    /// <inheritdoc />
    public string TokenKind => "PostgreSql.xmin";

    /// <inheritdoc />
    public bool IsEmpty => Xmin == 0;

    /// <summary>
    /// Initializes a new instance of the <see cref="XminConcurrencyToken"/> struct with the specified transaction ID.
    /// </summary>
    /// <param name="xmin">The 32-bit unsigned transaction ID from PostgreSQL.</param>
    public XminConcurrencyToken(uint xmin)
    {
        Xmin = xmin;
    }

    /// <summary>
    /// Creates a new <see cref="XminConcurrencyToken"/> from a 32-bit unsigned integer.
    /// </summary>
    /// <param name="xmin">The 32-bit unsigned transaction ID.</param>
    /// <returns>A new <see cref="XminConcurrencyToken"/> instance.</returns>
    public static XminConcurrencyToken From(uint xmin) => new(xmin);

    /// <summary>
    /// Parses an xmin value from its string representation.
    /// </summary>
    /// <param name="value">The string representation of the unsigned 32-bit integer.</param>
    /// <returns>A new <see cref="XminConcurrencyToken"/> instance.</returns>
    /// <exception cref="ArgumentException"><paramref name="value"/> is <see langword="null"/> or whitespace</exception>
    /// <exception cref="FormatException"><paramref name="value"/> is not in a valid numeric format</exception>
    public static XminConcurrencyToken Parse(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return new(uint.Parse(value, CultureInfo.InvariantCulture));
    }

    /// <inheritdoc />
    public bool Equals(IConcurrencyToken? other)
    {
        if (other is null)
        {
            return false;
        }

        if (other is XminConcurrencyToken xminOther)
        {
            return Xmin == xminOther.Xmin;
        }

        return string.Equals(Value, other.Value, StringComparison.Ordinal) &&
               string.Equals(TokenKind, other.TokenKind, StringComparison.Ordinal);
    }

    /// <inheritdoc />
    public int CompareTo(XminConcurrencyToken other) => Xmin.CompareTo(other.Xmin);

    /// <inheritdoc />
    /// <exception cref="ArgumentException"><paramref name="obj"/> is not an instance of <see cref="XminConcurrencyToken"/></exception>
    public int CompareTo(object? obj)
    {
        if (obj is null)
        {
            return 1;
        }

        if (obj is XminConcurrencyToken other)
        {
            return CompareTo(other);
        }

        throw new ArgumentException($"Object must be of type {nameof(XminConcurrencyToken)}.", nameof(obj));
    }

    /// <summary>
    /// Determines whether the left token is less than the right token.
    /// </summary>
    /// <param name="left">The left token to compare.</param>
    /// <param name="right">The right token to compare.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> is less than <paramref name="right"/>; otherwise, <see langword="false"/>.</returns>
    public static bool operator <(XminConcurrencyToken left, XminConcurrencyToken right) => left.Xmin < right.Xmin;

    /// <summary>
    /// Determines whether the left token is less than or equal to the right token.
    /// </summary>
    /// <param name="left">The left token to compare.</param>
    /// <param name="right">The right token to compare.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> is less than or equal to <paramref name="right"/>; otherwise, <see langword="false"/>.</returns>
    public static bool operator <=(XminConcurrencyToken left, XminConcurrencyToken right) => left.Xmin <= right.Xmin;

    /// <summary>
    /// Determines whether the left token is greater than the right token.
    /// </summary>
    /// <param name="left">The left token to compare.</param>
    /// <param name="right">The right token to compare.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> is greater than <paramref name="right"/>; otherwise, <see langword="false"/>.</returns>
    public static bool operator >(XminConcurrencyToken left, XminConcurrencyToken right) => left.Xmin > right.Xmin;

    /// <summary>
    /// Determines whether the left token is greater than or equal to the right token.
    /// </summary>
    /// <param name="left">The left token to compare.</param>
    /// <param name="right">The right token to compare.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> is greater than or equal to <paramref name="right"/>; otherwise, <see langword="false"/>.</returns>
    public static bool operator >=(XminConcurrencyToken left, XminConcurrencyToken right) => left.Xmin >= right.Xmin;

    /// <summary>
    /// Converts a 32-bit unsigned integer to an <see cref="XminConcurrencyToken"/>.
    /// </summary>
    /// <param name="xmin">The transaction ID value to convert.</param>
    /// <returns>A new <see cref="XminConcurrencyToken"/> instance wrapping the specified transaction ID.</returns>
    public static implicit operator XminConcurrencyToken(uint xmin) => new(xmin);

    /// <summary>
    /// Converts an <see cref="XminConcurrencyToken"/> to a 32-bit unsigned integer.
    /// </summary>
    /// <param name="token">The token instance to convert.</param>
    /// <returns>The underlying 32-bit unsigned transaction ID value.</returns>
    public static implicit operator uint(XminConcurrencyToken token) => token.Xmin;

    /// <inheritdoc />
    public override string ToString() => $"[xmin:{Xmin.ToString(CultureInfo.InvariantCulture)}]";

    /// <inheritdoc />
    public string ToString(string? format, IFormatProvider? formatProvider) =>
        Xmin.ToString(format, formatProvider);

    /// <inheritdoc />
    public bool TryFormat(
        Span<char> destination,
        out int charsWritten,
        ReadOnlySpan<char> format,
        IFormatProvider? provider) =>
        Xmin.TryFormat(destination, out charsWritten, format, provider);
}
