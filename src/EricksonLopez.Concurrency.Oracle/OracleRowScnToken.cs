// Copyright © Erickson Lopez. MIT License.
using System;
using System.Globalization;
using EricksonLopez.Concurrency.Abstractions;

namespace EricksonLopez.Concurrency.Oracle;

/// <summary>
/// Represents an immutable concurrency token wrapping the Oracle pseudo-column <c>ORA_ROWSCN</c> (System Change Number).
/// </summary>
public readonly record struct OracleRowScnToken : IConcurrencyToken, IComparable<OracleRowScnToken>, IComparable
{
    /// <summary>
    /// Represents an uninitialized or empty Oracle SCN token.
    /// </summary>
    public static readonly OracleRowScnToken None = new(0);

    /// <summary>
    /// Gets the underlying 64-bit System Change Number.
    /// </summary>
    public long RowScn { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="OracleRowScnToken"/> struct with the specified SCN.
    /// </summary>
    /// <param name="rowScn">The non-negative Oracle SCN value.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="rowScn"/> is negative</exception>
    public OracleRowScnToken(long rowScn)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(rowScn);
        RowScn = rowScn;
    }

    /// <inheritdoc />
    public string Value => RowScn.ToString(CultureInfo.InvariantCulture);

    /// <inheritdoc />
    public string TokenKind => "Oracle.ORA_ROWSCN";

    /// <inheritdoc />
    public bool IsEmpty => RowScn == 0;

    /// <summary>
    /// Parses a string representation of an Oracle SCN into an <see cref="OracleRowScnToken"/>.
    /// </summary>
    /// <param name="scnString">The string representation of the SCN.</param>
    /// <returns>A new <see cref="OracleRowScnToken"/> instance.</returns>
    /// <exception cref="ArgumentException"><paramref name="scnString"/> is <see langword="null"/> or whitespace</exception>
    /// <exception cref="FormatException"><paramref name="scnString"/> is not in a valid numeric format</exception>
    public static OracleRowScnToken Parse(string scnString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scnString);
        long parsed = long.Parse(scnString, CultureInfo.InvariantCulture);
        return new OracleRowScnToken(parsed);
    }

    /// <inheritdoc />
    public bool Equals(IConcurrencyToken? other)
    {
        if (other is null)
        {
            return false;
        }

        return string.Equals(Value, other.Value, StringComparison.Ordinal);
    }

    /// <inheritdoc />
    public int CompareTo(OracleRowScnToken other)
    {
        return RowScn.CompareTo(other.RowScn);
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentException"><paramref name="obj"/> is not an instance of <see cref="OracleRowScnToken"/></exception>
    public int CompareTo(object? obj)
    {
        if (obj is null)
        {
            return 1;
        }

        if (obj is OracleRowScnToken other)
        {
            return CompareTo(other);
        }

        throw new ArgumentException("Object must be of type OracleRowScnToken.", nameof(obj));
    }

    /// <summary>
    /// Determines whether the left token is less than the right token.
    /// </summary>
    /// <param name="left">The left token to compare.</param>
    /// <param name="right">The right token to compare.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> is less than <paramref name="right"/>; otherwise, <see langword="false"/>.</returns>
    public static bool operator <(OracleRowScnToken left, OracleRowScnToken right) => left.CompareTo(right) < 0;

    /// <summary>
    /// Determines whether the left token is less than or equal to the right token.
    /// </summary>
    /// <param name="left">The left token to compare.</param>
    /// <param name="right">The right token to compare.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> is less than or equal to <paramref name="right"/>; otherwise, <see langword="false"/>.</returns>
    public static bool operator <=(OracleRowScnToken left, OracleRowScnToken right) => left.CompareTo(right) <= 0;

    /// <summary>
    /// Determines whether the left token is greater than the right token.
    /// </summary>
    /// <param name="left">The left token to compare.</param>
    /// <param name="right">The right token to compare.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> is greater than <paramref name="right"/>; otherwise, <see langword="false"/>.</returns>
    public static bool operator >(OracleRowScnToken left, OracleRowScnToken right) => left.CompareTo(right) > 0;

    /// <summary>
    /// Determines whether the left token is greater than or equal to the right token.
    /// </summary>
    /// <param name="left">The left token to compare.</param>
    /// <param name="right">The right token to compare.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> is greater than or equal to <paramref name="right"/>; otherwise, <see langword="false"/>.</returns>
    public static bool operator >=(OracleRowScnToken left, OracleRowScnToken right) => left.CompareTo(right) >= 0;

    /// <inheritdoc />
    public override string ToString() => $"[ORA_ROWSCN:{Value}]";
}
