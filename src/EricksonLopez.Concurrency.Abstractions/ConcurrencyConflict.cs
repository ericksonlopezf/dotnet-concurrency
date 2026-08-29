// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;

namespace EricksonLopez.Concurrency.Abstractions;

/// <summary>
/// Encapsulates rich, immutable diagnostic and domain details of a detected concurrency conflict.
/// </summary>
public sealed record ConcurrencyConflict
{
    private static readonly IReadOnlyDictionary<string, string> EmptyMetadata =
        new ReadOnlyDictionary<string, string>(new Dictionary<string, string>());

    /// <summary>
    /// Gets the unique identifier of the entity or aggregate involved in the conflict.
    /// </summary>
    public string EntityId { get; init; }

    /// <summary>
    /// Gets the type name or discriminator of the entity or aggregate involved in the conflict.
    /// </summary>
    public string EntityType { get; init; }

    /// <summary>
    /// Gets the expected version specified by the caller, or <see langword="null"/> if not version-based.
    /// </summary>
    public ExpectedVersion? ExpectedVersion { get; init; }

    /// <summary>
    /// Gets the actual version discovered in persistence or memory, or <see langword="null"/> if not known.
    /// </summary>
    public ActualVersion? ActualVersion { get; init; }

    /// <summary>
    /// Gets the expected concurrency token specified by the caller, or <see langword="null"/> if not token-based.
    /// </summary>
    public IConcurrencyToken? ExpectedToken { get; init; }

    /// <summary>
    /// Gets the actual concurrency token discovered in storage, or <see langword="null"/> if not known.
    /// </summary>
    public IConcurrencyToken? ActualToken { get; init; }

    /// <summary>
    /// Gets the specific category of concurrency conflict.
    /// </summary>
    public ConcurrencyConflictType ConflictType { get; init; }

    /// <summary>
    /// Gets the high-level operational classification of the conflict (e.g., Transient, StaleState, NonRetryable).
    /// </summary>
    public ConcurrencyConflictClassification Classification { get; init; }

    /// <summary>
    /// Gets the name of the operation during which the conflict occurred.
    /// </summary>
    public string Operation { get; init; }

    /// <summary>
    /// Gets the diagnostic message describing the conflict.
    /// </summary>
    public string Message { get; init; }

    /// <summary>
    /// Gets the UTC date and time when the conflict was detected.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; }

    /// <summary>
    /// Gets contextual metadata associated with the conflict.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrencyConflict"/> record.
    /// </summary>
    /// <param name="entityId">The unique identifier of the entity involved in the conflict.</param>
    /// <param name="entityType">The type name or discriminator of the entity.</param>
    /// <param name="conflictType">The specific category of concurrency conflict.</param>
    /// <param name="classification">The operational classification of the conflict.</param>
    /// <param name="operation">The operation during which the conflict occurred.</param>
    /// <param name="message">The descriptive explanation of the conflict.</param>
    /// <param name="expectedVersion">The expected version constraint, if applicable.</param>
    /// <param name="actualVersion">The actual version discovered, if known.</param>
    /// <param name="expectedToken">The expected concurrency token, if applicable.</param>
    /// <param name="actualToken">The actual concurrency token discovered, if known.</param>
    /// <param name="timestamp">The UTC date and time when the conflict occurred, or <see langword="null"/> to use the current time.</param>
    /// <param name="metadata">Additional key-value pairs providing context for the conflict.</param>
    public ConcurrencyConflict(
        string entityId,
        string entityType,
        ConcurrencyConflictType conflictType,
        ConcurrencyConflictClassification classification,
        string operation,
        string message,
        ExpectedVersion? expectedVersion = null,
        ActualVersion? actualVersion = null,
        IConcurrencyToken? expectedToken = null,
        IConcurrencyToken? actualToken = null,
        DateTimeOffset? timestamp = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        EntityId = entityId ?? string.Empty;
        EntityType = entityType ?? "Unknown";
        ConflictType = conflictType;
        Classification = classification;
        Operation = operation ?? "Update";
        Message = message ?? "A concurrency conflict occurred.";
        ExpectedVersion = expectedVersion;
        ActualVersion = actualVersion;
        ExpectedToken = expectedToken;
        ActualToken = actualToken;
        Timestamp = timestamp ?? DateTimeOffset.UtcNow;
        Metadata = metadata ?? EmptyMetadata;
    }

    /// <summary>
    /// Creates a <see cref="ConcurrencyConflict"/> representing a numeric version mismatch.
    /// </summary>
    /// <param name="entityId">The identifier of the entity involved in the conflict.</param>
    /// <param name="entityType">The type name of the entity involved in the conflict.</param>
    /// <param name="expected">The expected version constraint.</param>
    /// <param name="actual">The actual version discovered in storage, if known.</param>
    /// <param name="operation">The operation name during which the conflict occurred.</param>
    /// <param name="metadata">Additional contextual metadata.</param>
    /// <returns>A new <see cref="ConcurrencyConflict"/> configured for a version mismatch.</returns>
    public static ConcurrencyConflict VersionMismatch(
        string entityId,
        string entityType,
        ExpectedVersion expected,
        ActualVersion? actual = null,
        string operation = "Update",
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        string msg = actual.HasValue
            ? string.Format(CultureInfo.InvariantCulture, "Optimistic concurrency conflict on '{0}' with ID '{1}'. Expected {2}, but found {3}.", entityType, entityId, expected, actual.Value)
            : string.Format(CultureInfo.InvariantCulture, "Optimistic concurrency conflict on '{0}' with ID '{1}'. Expected {2}, but row count affected was 0.", entityType, entityId, expected);

        return new ConcurrencyConflict(
            entityId: entityId,
            entityType: entityType,
            conflictType: ConcurrencyConflictType.VersionMismatch,
            classification: ConcurrencyConflictClassification.StaleState,
            operation: operation,
            message: msg,
            expectedVersion: expected,
            actualVersion: actual,
            metadata: metadata);
    }

    /// <summary>
    /// Creates a <see cref="ConcurrencyConflict"/> representing an opaque token mismatch.
    /// </summary>
    /// <param name="entityId">The identifier of the entity involved in the conflict.</param>
    /// <param name="entityType">The type name of the entity involved in the conflict.</param>
    /// <param name="expected">The expected token constraint.</param>
    /// <param name="actual">The actual token found in storage, if known.</param>
    /// <param name="operation">The operation name during which the conflict occurred.</param>
    /// <param name="metadata">Additional contextual metadata.</param>
    /// <returns>A new <see cref="ConcurrencyConflict"/> configured for a token mismatch.</returns>
    public static ConcurrencyConflict TokenMismatch(
        string entityId,
        string entityType,
        IConcurrencyToken expected,
        IConcurrencyToken? actual = null,
        string operation = "Update",
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        string msg = actual is not null
            ? string.Format(CultureInfo.InvariantCulture, "Concurrency token mismatch on '{0}' with ID '{1}'. Expected '{2}', but found '{3}'.", entityType, entityId, expected.Value, actual.Value)
            : string.Format(CultureInfo.InvariantCulture, "Concurrency token mismatch on '{0}' with ID '{1}'. Expected '{2}', but row count affected was 0.", entityType, entityId, expected.Value);

        return new ConcurrencyConflict(
            entityId: entityId,
            entityType: entityType,
            conflictType: ConcurrencyConflictType.TokenMismatch,
            classification: ConcurrencyConflictClassification.StaleState,
            operation: operation,
            message: msg,
            expectedToken: expected,
            actualToken: actual,
            metadata: metadata);
    }

    /// <summary>
    /// Creates a <see cref="ConcurrencyConflict"/> indicating that the target entity was deleted or missing.
    /// </summary>
    /// <param name="entityId">The identifier of the entity involved in the conflict.</param>
    /// <param name="entityType">The type name of the entity involved in the conflict.</param>
    /// <param name="operation">The operation name during which the conflict occurred.</param>
    /// <returns>A new <see cref="ConcurrencyConflict"/> indicating that the entity does not exist.</returns>
    public static ConcurrencyConflict Deleted(
        string entityId,
        string entityType,
        string operation = "Update")
    {
        return new ConcurrencyConflict(
            entityId: entityId,
            entityType: entityType,
            conflictType: ConcurrencyConflictType.StateDeleted,
            classification: ConcurrencyConflictClassification.NonRetryable,
            operation: operation,
            message: string.Format(CultureInfo.InvariantCulture, "Entity '{0}' with ID '{1}' does not exist or has been deleted.", entityType, entityId),
            actualVersion: new ActualVersion(ConcurrencyVersion.None, exists: false));
    }
}
