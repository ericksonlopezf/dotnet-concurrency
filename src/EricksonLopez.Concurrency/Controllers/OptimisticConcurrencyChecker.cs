// Copyright © Erickson Lopez. MIT License.
using System.Diagnostics.CodeAnalysis;
using EricksonLopez.Concurrency.Abstractions;
using EricksonLopez.Concurrency.Diagnostics;

namespace EricksonLopez.Concurrency.Controllers;

/// <summary>
/// Provides zero-allocation synchronous evaluation of optimistic concurrency conditions and tokens.
/// </summary>
public sealed class OptimisticConcurrencyChecker : IConcurrencyChecker
{
    /// <summary>
    /// Gets the shared singleton instance of <see cref="OptimisticConcurrencyChecker"/>.
    /// </summary>
    public static readonly OptimisticConcurrencyChecker Instance = new();

    /// <inheritdoc />
    public bool CheckVersion(
        ExpectedVersion expected,
        ConcurrencyVersion actual,
        string entityId,
        string entityType,
        [NotNullWhen(false)] out ConcurrencyConflict? conflict)
    {
        if (expected.Matches(actual))
        {
            conflict = null;
            return true;
        }

        conflict = ConcurrencyConflict.VersionMismatch(
            entityId: entityId,
            entityType: entityType,
            expected: expected,
            actual: ActualVersion.From(actual));

        ConcurrencyDiagnostics.RecordConflict(null, nameof(ConcurrencyConflictType.VersionMismatch), entityType);
        return false;
    }

    /// <inheritdoc />
    public bool CheckToken(
        IConcurrencyToken expected,
        IConcurrencyToken actual,
        string entityId,
        string entityType,
        [NotNullWhen(false)] out ConcurrencyConflict? conflict)
    {
        if (expected is not null && expected.Equals(actual))
        {
            conflict = null;
            return true;
        }

        conflict = ConcurrencyConflict.TokenMismatch(
            entityId: entityId,
            entityType: entityType,
            expected: expected ?? ConcurrencyToken.None,
            actual: actual);

        ConcurrencyDiagnostics.RecordConflict(null, nameof(ConcurrencyConflictType.TokenMismatch), entityType);
        return false;
    }
}
