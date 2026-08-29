// Copyright © Erickson Lopez. MIT License.
using System;
using System.Globalization;

namespace EricksonLopez.Concurrency.Abstractions;

/// <summary>
/// Represents an optimistic expectation of an entity's version before applying modifications.
/// </summary>
public readonly record struct ExpectedVersion : IComparable<ExpectedVersion>, IComparable
{
    /// <summary>
    /// Represents an expectation matching any version, effectively bypassing optimistic version checks.
    /// </summary>
    public static readonly ExpectedVersion Any = new(0, ExpectedVersionKind.Any);

    /// <summary>
    /// Represents an expectation that the target entity does not yet exist in storage (version 0).
    /// </summary>
    public static readonly ExpectedVersion New = new(0, ExpectedVersionKind.New);

    /// <summary>
    /// Represents an expectation that the target entity exists in storage with any non-zero version.
    /// </summary>
    public static readonly ExpectedVersion Exists = new(0, ExpectedVersionKind.Exists);

    /// <summary>
    /// Gets the specific version value when <see cref="Kind"/> is <see cref="ExpectedVersionKind.Specific"/>.
    /// </summary>
    public ConcurrencyVersion Version { get; }

    /// <summary>
    /// Gets the category of version expectation.
    /// </summary>
    public ExpectedVersionKind Kind { get; }

    private ExpectedVersion(long value, ExpectedVersionKind kind)
    {
        Version = new ConcurrencyVersion(value);
        Kind = kind;
    }

    /// <summary>
    /// Creates an <see cref="ExpectedVersion"/> representing an exact numeric version value.
    /// </summary>
    /// <param name="version">The exact expected 64-bit integer version value.</param>
    /// <returns>A new <see cref="ExpectedVersion"/> configured with <see cref="ExpectedVersionKind.Specific"/>.</returns>
    public static ExpectedVersion Specific(long version) => new(version, ExpectedVersionKind.Specific);

    /// <summary>
    /// Creates an <see cref="ExpectedVersion"/> representing an exact <see cref="ConcurrencyVersion"/>.
    /// </summary>
    /// <param name="version">The exact expected concurrency version.</param>
    /// <returns>A new <see cref="ExpectedVersion"/> configured with <see cref="ExpectedVersionKind.Specific"/>.</returns>
    public static ExpectedVersion Specific(ConcurrencyVersion version) => new(version.Value, ExpectedVersionKind.Specific);

    /// <summary>
    /// Determines whether the specified actual version satisfies this expected version contract.
    /// </summary>
    /// <param name="actual">The actual concurrency version found in storage or memory.</param>
    /// <returns><see langword="true"/> if the actual version matches the expectation; otherwise, <see langword="false"/>.</returns>
    public bool Matches(ConcurrencyVersion actual) => Kind switch
    {
        ExpectedVersionKind.Any => true,
        ExpectedVersionKind.New => actual.IsNone,
        ExpectedVersionKind.Exists => !actual.IsNone,
        ExpectedVersionKind.Specific => Version == actual,
        _ => false
    };

    /// <summary>
    /// Converts a <see cref="ConcurrencyVersion"/> to a specific <see cref="ExpectedVersion"/>.
    /// </summary>
    /// <param name="version">The specific concurrency version to convert.</param>
    /// <returns>A new <see cref="ExpectedVersion"/> configured with <see cref="ExpectedVersionKind.Specific"/>.</returns>
    public static implicit operator ExpectedVersion(ConcurrencyVersion version) => Specific(version);

    /// <summary>
    /// Converts a 64-bit integer value to a specific <see cref="ExpectedVersion"/>.
    /// </summary>
    /// <param name="value">The specific 64-bit integer version value to convert.</param>
    /// <returns>A new <see cref="ExpectedVersion"/> configured with <see cref="ExpectedVersionKind.Specific"/>.</returns>
    public static implicit operator ExpectedVersion(long value) => Specific(value);

    /// <inheritdoc />
    public int CompareTo(ExpectedVersion other)
    {
        int kindComparison = Kind.CompareTo(other.Kind);
        if (kindComparison != 0)
        {
            return kindComparison;
        }

        return Version.CompareTo(other.Version);
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentException"><paramref name="obj"/> is not an instance of <see cref="ExpectedVersion"/></exception>
    public int CompareTo(object? obj)
    {
        if (obj is null)
        {
            return 1;
        }

        if (obj is ExpectedVersion other)
        {
            return CompareTo(other);
        }

        throw new ArgumentException($"Object must be of type {nameof(ExpectedVersion)}.", nameof(obj));
    }

    /// <summary>
    /// Determines whether the left expected version is less than the right expected version.
    /// </summary>
    /// <param name="left">The left expected version to compare.</param>
    /// <param name="right">The right expected version to compare.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> is less than <paramref name="right"/>; otherwise, <see langword="false"/>.</returns>
    public static bool operator <(ExpectedVersion left, ExpectedVersion right) => left.CompareTo(right) < 0;

    /// <summary>
    /// Determines whether the left expected version is less than or equal to the right expected version.
    /// </summary>
    /// <param name="left">The left expected version to compare.</param>
    /// <param name="right">The right expected version to compare.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> is less than or equal to <paramref name="right"/>; otherwise, <see langword="false"/>.</returns>
    public static bool operator <=(ExpectedVersion left, ExpectedVersion right) => left.CompareTo(right) <= 0;

    /// <summary>
    /// Determines whether the left expected version is greater than the right expected version.
    /// </summary>
    /// <param name="left">The left expected version to compare.</param>
    /// <param name="right">The right expected version to compare.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> is greater than <paramref name="right"/>; otherwise, <see langword="false"/>.</returns>
    public static bool operator >(ExpectedVersion left, ExpectedVersion right) => left.CompareTo(right) > 0;

    /// <summary>
    /// Determines whether the left expected version is greater than or equal to the right expected version.
    /// </summary>
    /// <param name="left">The left expected version to compare.</param>
    /// <param name="right">The right expected version to compare.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> is greater than or equal to <paramref name="right"/>; otherwise, <see langword="false"/>.</returns>
    public static bool operator >=(ExpectedVersion left, ExpectedVersion right) => left.CompareTo(right) >= 0;

    /// <inheritdoc />
    public override string ToString() => Kind switch
    {
        ExpectedVersionKind.Any => "[Expected:Any]",
        ExpectedVersionKind.New => "[Expected:New]",
        ExpectedVersionKind.Exists => "[Expected:Exists]",
        ExpectedVersionKind.Specific => $"[Expected:{Version.Value.ToString(CultureInfo.InvariantCulture)}]",
        _ => $"[Expected:{Kind}]"
    };
}
