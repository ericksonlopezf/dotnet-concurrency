// Copyright © Erickson Lopez. MIT License.
using System;
using System.Globalization;

namespace EricksonLopez.Concurrency.Abstractions;

/// <summary>
/// Represents an immutable, zero-allocation strongly-typed numeric concurrency version.
/// </summary>
public readonly record struct ConcurrencyVersion : IComparable<ConcurrencyVersion>, IComparable, ISpanFormattable, ISpanParsable<ConcurrencyVersion>, IParsable<ConcurrencyVersion>
{
    /// <summary>
    /// Represents the uninitialized or non-existent concurrency version (0).
    /// </summary>
    public static readonly ConcurrencyVersion None = new(0);

    /// <summary>
    /// Represents the initial concurrency version for newly created entities (1).
    /// </summary>
    public static readonly ConcurrencyVersion Initial = new(1);

    /// <summary>
    /// Gets the underlying 64-bit integer version value.
    /// </summary>
    public long Value { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrencyVersion"/> struct with the specified numeric value.
    /// </summary>
    /// <param name="value">The non-negative numeric version value.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is negative</exception>
    public ConcurrencyVersion(long value)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "Concurrency version value must be non-negative.");
        }

        Value = value;
    }

    /// <summary>
    /// Creates a new <see cref="ConcurrencyVersion"/> instance from the specified numeric value.
    /// </summary>
    /// <param name="value">The 64-bit integer representing the version value.</param>
    /// <returns>A new <see cref="ConcurrencyVersion"/> configured with the specified numeric value.</returns>
    public static ConcurrencyVersion From(long value) => new(value);

    /// <summary>
    /// Returns the next sequential concurrency version.
    /// </summary>
    /// <returns>A new <see cref="ConcurrencyVersion"/> incremented by 1.</returns>
    /// <exception cref="OverflowException">The incremented version exceeds <see cref="long.MaxValue"/></exception>
    public ConcurrencyVersion Next() => checked(new ConcurrencyVersion(Value + 1));

    /// <summary>
    /// Gets a value indicating whether this version represents an uninitialized or empty version.
    /// </summary>
    public bool IsNone => Value == 0;

    /// <inheritdoc />
    public int CompareTo(ConcurrencyVersion other) => Value.CompareTo(other.Value);

    /// <inheritdoc />
    /// <exception cref="ArgumentException"><paramref name="obj"/> is not an instance of <see cref="ConcurrencyVersion"/></exception>
    public int CompareTo(object? obj)
    {
        if (obj is null)
        {
            return 1;
        }

        if (obj is ConcurrencyVersion other)
        {
            return CompareTo(other);
        }

        throw new ArgumentException($"Object must be of type {nameof(ConcurrencyVersion)}.", nameof(obj));
    }

    /// <summary>
    /// Converts a <see cref="ConcurrencyVersion"/> to a 64-bit signed integer.
    /// </summary>
    /// <param name="version">The concurrency version instance to convert.</param>
    /// <returns>The underlying 64-bit signed integer version value.</returns>
    public static implicit operator long(ConcurrencyVersion version) => version.Value;

    /// <summary>
    /// Converts a 64-bit signed integer to a <see cref="ConcurrencyVersion"/>.
    /// </summary>
    /// <param name="value">The 64-bit integer version value to convert.</param>
    /// <returns>A new <see cref="ConcurrencyVersion"/> instance with the specified numeric value.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is negative</exception>
    public static explicit operator ConcurrencyVersion(long value) => new(value);

    /// <summary>
    /// Determines whether the left version is less than the right version.
    /// </summary>
    /// <param name="left">The left version to compare.</param>
    /// <param name="right">The right version to compare.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> is less than <paramref name="right"/>; otherwise, <see langword="false"/>.</returns>
    public static bool operator <(ConcurrencyVersion left, ConcurrencyVersion right) => left.Value < right.Value;

    /// <summary>
    /// Determines whether the left version is less than or equal to the right version.
    /// </summary>
    /// <param name="left">The left version to compare.</param>
    /// <param name="right">The right version to compare.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> is less than or equal to <paramref name="right"/>; otherwise, <see langword="false"/>.</returns>
    public static bool operator <=(ConcurrencyVersion left, ConcurrencyVersion right) => left.Value <= right.Value;

    /// <summary>
    /// Determines whether the left version is greater than the right version.
    /// </summary>
    /// <param name="left">The left version to compare.</param>
    /// <param name="right">The right version to compare.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> is greater than <paramref name="right"/>; otherwise, <see langword="false"/>.</returns>
    public static bool operator >(ConcurrencyVersion left, ConcurrencyVersion right) => left.Value > right.Value;

    /// <summary>
    /// Determines whether the left version is greater than or equal to the right version.
    /// </summary>
    /// <param name="left">The left version to compare.</param>
    /// <param name="right">The right version to compare.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> is greater than or equal to <paramref name="right"/>; otherwise, <see langword="false"/>.</returns>
    public static bool operator >=(ConcurrencyVersion left, ConcurrencyVersion right) => left.Value >= right.Value;

    /// <inheritdoc />
    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);

    /// <inheritdoc />
    public string ToString(string? format, IFormatProvider? formatProvider) =>
        Value.ToString(format, formatProvider);

    /// <inheritdoc />
    public bool TryFormat(
        Span<char> destination,
        out int charsWritten,
        ReadOnlySpan<char> format,
        IFormatProvider? provider) =>
        Value.TryFormat(destination, out charsWritten, format, provider);

    /// <summary>
    /// Attempts to parse a span of characters into a <see cref="ConcurrencyVersion"/>.
    /// </summary>
    /// <param name="s">The span of characters to parse.</param>
    /// <param name="provider">An object that provides culture-specific formatting information.</param>
    /// <param name="result">When this method returns, contains the parsed <see cref="ConcurrencyVersion"/> if successful; otherwise, <see cref="None"/>.</param>
    /// <returns><see langword="true"/> if parsing succeeded; otherwise, <see langword="false"/>.</returns>
    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out ConcurrencyVersion result)
    {
        if (long.TryParse(s, NumberStyles.Integer, provider ?? CultureInfo.InvariantCulture, out long parsedValue) && parsedValue >= 0)
        {
            result = new ConcurrencyVersion(parsedValue);
            return true;
        }

        result = None;
        return false;
    }

    /// <summary>
    /// Attempts to parse a span of characters into a <see cref="ConcurrencyVersion"/> using invariant culture.
    /// </summary>
    /// <param name="s">The span of characters to parse.</param>
    /// <param name="result">When this method returns, contains the parsed <see cref="ConcurrencyVersion"/> if successful; otherwise, <see cref="None"/>.</param>
    /// <returns><see langword="true"/> if parsing succeeded; otherwise, <see langword="false"/>.</returns>
    public static bool TryParse(ReadOnlySpan<char> s, out ConcurrencyVersion result) =>
        TryParse(s, CultureInfo.InvariantCulture, out result);

    /// <summary>
    /// Attempts to parse a string into a <see cref="ConcurrencyVersion"/>.
    /// </summary>
    /// <param name="s">The string to parse.</param>
    /// <param name="provider">An object that provides culture-specific formatting information.</param>
    /// <param name="result">When this method returns, contains the parsed <see cref="ConcurrencyVersion"/> if successful; otherwise, <see cref="None"/>.</param>
    /// <returns><see langword="true"/> if parsing succeeded; otherwise, <see langword="false"/>.</returns>
    public static bool TryParse(string? s, IFormatProvider? provider, out ConcurrencyVersion result)
    {
        if (s is not null && TryParse(s.AsSpan(), provider, out result))
        {
            return true;
        }

        result = None;
        return false;
    }

    /// <summary>
    /// Attempts to parse a string into a <see cref="ConcurrencyVersion"/> using invariant culture.
    /// </summary>
    /// <param name="s">The string to parse.</param>
    /// <param name="result">When this method returns, contains the parsed <see cref="ConcurrencyVersion"/> if successful; otherwise, <see cref="None"/>.</param>
    /// <returns><see langword="true"/> if parsing succeeded; otherwise, <see langword="false"/>.</returns>
    public static bool TryParse(string? s, out ConcurrencyVersion result) =>
        TryParse(s, CultureInfo.InvariantCulture, out result);

    /// <summary>
    /// Parses a span of characters into a <see cref="ConcurrencyVersion"/>.
    /// </summary>
    /// <param name="s">The span of characters to parse.</param>
    /// <param name="provider">An object that provides culture-specific formatting information.</param>
    /// <returns>The parsed <see cref="ConcurrencyVersion"/>.</returns>
    /// <exception cref="FormatException"><paramref name="s"/> is not in the correct format or represents a negative value</exception>
    public static ConcurrencyVersion Parse(ReadOnlySpan<char> s, IFormatProvider? provider = null)
    {
        if (TryParse(s, provider, out ConcurrencyVersion result))
        {
            return result;
        }

        throw new FormatException($"Input string '{s.ToString()}' was not in a correct format for a non-negative {nameof(ConcurrencyVersion)}.");
    }

    /// <summary>
    /// Parses a string into a <see cref="ConcurrencyVersion"/>.
    /// </summary>
    /// <param name="s">The string to parse.</param>
    /// <param name="provider">An object that provides culture-specific formatting information.</param>
    /// <returns>The parsed <see cref="ConcurrencyVersion"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="s"/> is <see langword="null"/></exception>
    /// <exception cref="FormatException"><paramref name="s"/> is not in the correct format or represents a negative value</exception>
    public static ConcurrencyVersion Parse(string s, IFormatProvider? provider = null)
    {
        ArgumentNullException.ThrowIfNull(s);
        return Parse(s.AsSpan(), provider);
    }
}
