// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using EricksonLopez.Concurrency.Abstractions;
using EricksonLopez.Result;

namespace EricksonLopez.Concurrency.Result;

/// <summary>
/// Provides factory methods to create structured <see cref="Error"/> instances representing optimistic concurrency conflicts.
/// </summary>
public static class ConcurrencyErrors
{
    /// <summary>
    /// Specifies the default machine-readable error code for general optimistic concurrency conflicts.
    /// </summary>
    public const string ConcurrencyConflictCode = "Concurrency.Conflict";

    /// <summary>
    /// Specifies the machine-readable error code for optimistic version mismatches.
    /// </summary>
    public const string VersionMismatchCode = "Concurrency.VersionMismatch";

    /// <summary>
    /// Specifies the machine-readable error code for optimistic token mismatches.
    /// </summary>
    public const string TokenMismatchCode = "Concurrency.TokenMismatch";

    /// <summary>
    /// Specifies the machine-readable error code when an entity is missing or deleted.
    /// </summary>
    public const string EntityDeletedCode = "Concurrency.EntityDeleted";

    /// <summary>
    /// Specifies the machine-readable error code for database serialization failures.
    /// </summary>
    public const string SerializationFailureCode = "Concurrency.SerializationFailure";

    /// <summary>
    /// Specifies the machine-readable error code for database deadlock conditions.
    /// </summary>
    public const string DeadlockCode = "Concurrency.Deadlock";

    /// <summary>
    /// Converts a <see cref="ConcurrencyConflict"/> into a structured <see cref="Error"/> with metadata and retryability classification.
    /// </summary>
    /// <param name="conflict">The rich conflict descriptor containing failure diagnostics.</param>
    /// <returns>A structured <see cref="Error"/> configured with <see cref="ErrorType.Conflict"/>.</returns>
    public static Error FromConflict(ConcurrencyConflict conflict)
    {
        ArgumentNullException.ThrowIfNull(conflict);

        string errorCode = conflict.ConflictType switch
        {
            ConcurrencyConflictType.VersionMismatch => VersionMismatchCode,
            ConcurrencyConflictType.TokenMismatch => TokenMismatchCode,
            ConcurrencyConflictType.StateDeleted => EntityDeletedCode,
            ConcurrencyConflictType.SerializationFailure => SerializationFailureCode,
            ConcurrencyConflictType.Deadlock => DeadlockCode,
            _ => ConcurrencyConflictCode
        };

        ErrorRetryability retryability = conflict.Classification switch
        {
            ConcurrencyConflictClassification.Transient => ErrorRetryability.Transient,
            ConcurrencyConflictClassification.Retryable => ErrorRetryability.Transient,
            ConcurrencyConflictClassification.NonRetryable => ErrorRetryability.Permanent,
            ConcurrencyConflictClassification.Fatal => ErrorRetryability.Permanent,
            _ => ErrorRetryability.NotApplicable
        };

        var metadata = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["entityId"] = conflict.EntityId,
            ["entityType"] = conflict.EntityType,
            ["operation"] = conflict.Operation,
            ["conflictType"] = conflict.ConflictType.ToString(),
            ["classification"] = conflict.Classification.ToString(),
            ["timestamp"] = conflict.Timestamp.ToString("O")
        };

        if (conflict.ExpectedVersion.HasValue)
        {
            metadata["expectedVersion"] = conflict.ExpectedVersion.Value.ToString();
        }

        if (conflict.ActualVersion.HasValue)
        {
            metadata["actualVersion"] = conflict.ActualVersion.Value.ToString();
        }

        if (conflict.ExpectedToken is not null)
        {
            metadata["expectedToken"] = conflict.ExpectedToken.Value;
        }

        if (conflict.ActualToken is not null)
        {
            metadata["actualToken"] = conflict.ActualToken.Value;
        }

        foreach (KeyValuePair<string, string> kvp in conflict.Metadata)
        {
            metadata[kvp.Key] = kvp.Value;
        }

        return Error.Create(errorCode, conflict.Message)
            .WithType(ErrorType.Conflict)
            .WithSeverity(ErrorSeverity.Warning)
            .WithRetryability(retryability)
            .WithMetadata(metadata)
            .Build();
    }

    /// <summary>
    /// Creates a structured <see cref="Error"/> representing an optimistic version mismatch.
    /// </summary>
    /// <param name="entityId">The unique identifier of the entity involved in the conflict.</param>
    /// <param name="entityType">The type name of the entity involved in the conflict.</param>
    /// <param name="expected">The expected version constraint.</param>
    /// <param name="actual">The actual version discovered in storage, if known.</param>
    /// <returns>A structured conflict <see cref="Error"/>.</returns>
    public static Error VersionMismatch(string entityId, string entityType, ExpectedVersion expected, ActualVersion? actual = null)
    {
        ConcurrencyConflict conflict = ConcurrencyConflict.VersionMismatch(entityId, entityType, expected, actual);
        return FromConflict(conflict);
    }

    /// <summary>
    /// Creates a structured <see cref="Error"/> representing an optimistic token mismatch.
    /// </summary>
    /// <param name="entityId">The unique identifier of the entity involved in the conflict.</param>
    /// <param name="entityType">The type name of the entity involved in the conflict.</param>
    /// <param name="expected">The expected concurrency token constraint.</param>
    /// <param name="actual">The actual concurrency token discovered in storage, if known.</param>
    /// <returns>A structured conflict <see cref="Error"/>.</returns>
    public static Error TokenMismatch(string entityId, string entityType, IConcurrencyToken expected, IConcurrencyToken? actual = null)
    {
        ConcurrencyConflict conflict = ConcurrencyConflict.TokenMismatch(entityId, entityType, expected, actual);
        return FromConflict(conflict);
    }
}
