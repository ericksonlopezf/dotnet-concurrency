// Copyright © Erickson Lopez. MIT License.
using System;
using System.Diagnostics.CodeAnalysis;
using EricksonLopez.Concurrency.Abstractions;

namespace EricksonLopez.Concurrency.SqlServer;

/// <summary>
/// Represents an immutable concurrency token wrapping a Microsoft SQL Server 8-byte <c>ROWVERSION</c> or <c>TIMESTAMP</c> binary sequence.
/// </summary>
public readonly struct SqlServerRowVersionToken : IConcurrencyToken, IEquatable<SqlServerRowVersionToken>, IComparable<SqlServerRowVersionToken>, IComparable
{
    private readonly byte[]? _bytes;

    /// <summary>
    /// Represents an empty or uninitialized SQL Server rowversion token.
    /// </summary>
    public static readonly SqlServerRowVersionToken None = new(Array.Empty<byte>());

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlServerRowVersionToken"/> struct with the specified byte array.
    /// </summary>
    /// <param name="rowVersionBytes">The 8-byte rowversion binary array.</param>
    public SqlServerRowVersionToken(byte[]? rowVersionBytes)
    {
        _bytes = rowVersionBytes is not null ? (byte[])rowVersionBytes.Clone() : Array.Empty<byte>();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlServerRowVersionToken"/> struct from a read-only byte span.
    /// </summary>
    /// <param name="span">The byte span representing the rowversion data.</param>
    public SqlServerRowVersionToken(ReadOnlySpan<byte> span)
    {
        _bytes = span.ToArray();
    }

    /// <inheritdoc />
    public string Value => _bytes is not null ? Convert.ToHexString(_bytes) : string.Empty;

    /// <inheritdoc />
    public string TokenKind => "SqlServer.RowVersion";

    /// <inheritdoc />
    public bool IsEmpty => _bytes is null || _bytes.Length == 0;

    /// <summary>
    /// Returns a copy of the underlying rowversion byte array.
    /// </summary>
    /// <returns>A copy of the raw bytes.</returns>
    public byte[] ToByteArray() => _bytes is not null ? (byte[])_bytes.Clone() : Array.Empty<byte>();

    /// <summary>
    /// Parses a hexadecimal string representation into a <see cref="SqlServerRowVersionToken"/>.
    /// </summary>
    /// <param name="hexString">The hexadecimal string to parse.</param>
    /// <returns>A new <see cref="SqlServerRowVersionToken"/> instance.</returns>
    /// <exception cref="ArgumentException"><paramref name="hexString"/> is <see langword="null"/> or whitespace</exception>
    public static SqlServerRowVersionToken Parse(string hexString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hexString);
        string cleaned = hexString.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? hexString[2..] : hexString;
        byte[] bytes = Convert.FromHexString(cleaned);
        return new SqlServerRowVersionToken(bytes);
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

    /// <summary>
    /// Determines whether the current instance is equal to another <see cref="SqlServerRowVersionToken"/>.
    /// </summary>
    /// <param name="other">The token to compare with.</param>
    /// <returns><see langword="true"/> if both tokens have identical values; otherwise, <see langword="false"/>.</returns>
    public bool Equals(SqlServerRowVersionToken other)
    {
        return string.Equals(Value, other.Value, StringComparison.Ordinal);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is SqlServerRowVersionToken other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return string.GetHashCode(Value, StringComparison.Ordinal);
    }

    /// <inheritdoc />
    public int CompareTo(SqlServerRowVersionToken other)
    {
        return string.Compare(Value, other.Value, StringComparison.Ordinal);
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentException"><paramref name="obj"/> is not an instance of <see cref="SqlServerRowVersionToken"/></exception>
    public int CompareTo(object? obj)
    {
        if (obj is null)
        {
            return 1;
        }

        if (obj is SqlServerRowVersionToken other)
        {
            return CompareTo(other);
        }

        throw new ArgumentException("Object must be of type SqlServerRowVersionToken.", nameof(obj));
    }

    /// <summary>
    /// Determines whether two <see cref="SqlServerRowVersionToken"/> instances represent the same value.
    /// </summary>
    /// <param name="left">The left token to compare.</param>
    /// <param name="right">The right token to compare.</param>
    /// <returns><see langword="true"/> if both tokens are equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator ==(SqlServerRowVersionToken left, SqlServerRowVersionToken right) => left.Equals(right);

    /// <summary>
    /// Determines whether two <see cref="SqlServerRowVersionToken"/> instances represent different values.
    /// </summary>
    /// <param name="left">The left token to compare.</param>
    /// <param name="right">The right token to compare.</param>
    /// <returns><see langword="true"/> if both tokens are not equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator !=(SqlServerRowVersionToken left, SqlServerRowVersionToken right) => !left.Equals(right);

    /// <summary>
    /// Determines whether the left token is less than the right token.
    /// </summary>
    /// <param name="left">The left token to compare.</param>
    /// <param name="right">The right token to compare.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> is less than <paramref name="right"/>; otherwise, <see langword="false"/>.</returns>
    public static bool operator <(SqlServerRowVersionToken left, SqlServerRowVersionToken right) => left.CompareTo(right) < 0;

    /// <summary>
    /// Determines whether the left token is less than or equal to the right token.
    /// </summary>
    /// <param name="left">The left token to compare.</param>
    /// <param name="right">The right token to compare.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> is less than or equal to <paramref name="right"/>; otherwise, <see langword="false"/>.</returns>
    public static bool operator <=(SqlServerRowVersionToken left, SqlServerRowVersionToken right) => left.CompareTo(right) <= 0;

    /// <summary>
    /// Determines whether the left token is greater than the right token.
    /// </summary>
    /// <param name="left">The left token to compare.</param>
    /// <param name="right">The right token to compare.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> is greater than <paramref name="right"/>; otherwise, <see langword="false"/>.</returns>
    public static bool operator >(SqlServerRowVersionToken left, SqlServerRowVersionToken right) => left.CompareTo(right) > 0;

    /// <summary>
    /// Determines whether the left token is greater than or equal to the right token.
    /// </summary>
    /// <param name="left">The left token to compare.</param>
    /// <param name="right">The right token to compare.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> is greater than or equal to <paramref name="right"/>; otherwise, <see langword="false"/>.</returns>
    public static bool operator >=(SqlServerRowVersionToken left, SqlServerRowVersionToken right) => left.CompareTo(right) >= 0;

    /// <inheritdoc />
    public override string ToString() => IsEmpty ? "[RowVersion:Empty]" : $"[RowVersion:0x{Value}]";
}
