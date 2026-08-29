// Copyright © Erickson Lopez. MIT License.
using System;
using System.Globalization;

namespace EricksonLopez.Concurrency.Abstractions;

/// <summary>
/// Represents the actual version discovered during persistence or conflict inspection.
/// </summary>
public readonly record struct ActualVersion : IComparable<ActualVersion>, IComparable
{
    /// <summary>
    /// Represents an actual version indicating that the target entity was not found or has been deleted.
    /// </summary>
    public static readonly ActualVersion NotFound = new(ConcurrencyVersion.None, exists: false);

    /// <summary>
    /// Gets the actual concurrency version discovered.
    /// </summary>
    public ConcurrencyVersion Version { get; }

    /// <summary>
    /// Gets a value indicating whether the entity exists in storage.
    /// </summary>
    public bool Exists { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ActualVersion"/> struct.
    /// </summary>
    /// <param name="version">The actual numeric concurrency version discovered in storage or memory.</param>
    /// <param name="exists">A value indicating whether the entity exists in storage.</param>
    public ActualVersion(ConcurrencyVersion version, bool exists = true)
    {
        Version = version;
        Exists = exists;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ActualVersion"/> struct from a raw numeric value.
    /// </summary>
    /// <param name="version">The raw 64-bit integer version discovered in storage.</param>
    /// <param name="exists">A value indicating whether the entity exists in storage.</param>
    public ActualVersion(long version, bool exists = true)
        : this(new ConcurrencyVersion(version), exists)
    {
    }

    /// <summary>
    /// Creates an actual version instance from the specified numeric version value.
    /// </summary>
    /// <param name="version">The 64-bit integer representing the actual version value.</param>
    /// <returns>A new <see cref="ActualVersion"/> instance indicating that the entity exists.</returns>
    public static ActualVersion From(long version) => new(version, exists: true);

    /// <summary>
    /// Creates an actual version instance from the specified <see cref="ConcurrencyVersion"/>.
    /// </summary>
    /// <param name="version">The concurrency version discovered in storage.</param>
    /// <returns>A new <see cref="ActualVersion"/> instance indicating that the entity exists.</returns>
    public static ActualVersion From(ConcurrencyVersion version) => new(version, exists: true);

    /// <summary>
    /// Converts a <see cref="ConcurrencyVersion"/> to an <see cref="ActualVersion"/>.
    /// </summary>
    /// <param name="version">The concurrency version to convert.</param>
    /// <returns>A new <see cref="ActualVersion"/> instance wrapping the specified concurrency version.</returns>
    public static implicit operator ActualVersion(ConcurrencyVersion version) => From(version);

    /// <summary>
    /// Converts a 64-bit integer value to an <see cref="ActualVersion"/>.
    /// </summary>
    /// <param name="value">The raw version value to convert.</param>
    /// <returns>A new <see cref="ActualVersion"/> instance wrapping the specified 64-bit integer version value.</returns>
    public static implicit operator ActualVersion(long value) => From(value);

    /// <inheritdoc />
    public int CompareTo(ActualVersion other)
    {
        int existsComparison = Exists.CompareTo(other.Exists);
        if (existsComparison != 0)
        {
            return existsComparison;
        }

        return Version.CompareTo(other.Version);
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentException"><paramref name="obj"/> is not an instance of <see cref="ActualVersion"/></exception>
    public int CompareTo(object? obj)
    {
        if (obj is null)
        {
            return 1;
        }

        if (obj is ActualVersion other)
        {
            return CompareTo(other);
        }

        throw new ArgumentException($"Object must be of type {nameof(ActualVersion)}.", nameof(obj));
    }

    /// <summary>
    /// Determines whether the left actual version is less than the right actual version.
    /// </summary>
    /// <param name="left">The left actual version to compare.</param>
    /// <param name="right">The right actual version to compare.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> is less than <paramref name="right"/>; otherwise, <see langword="false"/>.</returns>
    public static bool operator <(ActualVersion left, ActualVersion right) => left.CompareTo(right) < 0;

    /// <summary>
    /// Determines whether the left actual version is less than or equal to the right actual version.
    /// </summary>
    /// <param name="left">The left actual version to compare.</param>
    /// <param name="right">The right actual version to compare.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> is less than or equal to <paramref name="right"/>; otherwise, <see langword="false"/>.</returns>
    public static bool operator <=(ActualVersion left, ActualVersion right) => left.CompareTo(right) <= 0;

    /// <summary>
    /// Determines whether the left actual version is greater than the right actual version.
    /// </summary>
    /// <param name="left">The left actual version to compare.</param>
    /// <param name="right">The right actual version to compare.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> is greater than <paramref name="right"/>; otherwise, <see langword="false"/>.</returns>
    public static bool operator >(ActualVersion left, ActualVersion right) => left.CompareTo(right) > 0;

    /// <summary>
    /// Determines whether the left actual version is greater than or equal to the right actual version.
    /// </summary>
    /// <param name="left">The left actual version to compare.</param>
    /// <param name="right">The right actual version to compare.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> is greater than or equal to <paramref name="right"/>; otherwise, <see langword="false"/>.</returns>
    public static bool operator >=(ActualVersion left, ActualVersion right) => left.CompareTo(right) >= 0;

    /// <inheritdoc />
    public override string ToString() => Exists
        ? $"[Actual:{Version.Value.ToString(CultureInfo.InvariantCulture)}]"
        : "[Actual:NotFound]";
}
