// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using EricksonLopez.Concurrency.Abstractions;

namespace EricksonLopez.Concurrency.Testing;

/// <summary>
/// Provides a fluent test builder for constructing <see cref="ConcurrencyConflict"/> instances with custom diagnostics and metadata.
/// </summary>
public sealed class ConcurrencyConflictBuilder
{
    private string _entityId = "test-entity-1";
    private string _entityType = "TestAggregate";
    private ConcurrencyConflictType _conflictType = ConcurrencyConflictType.VersionMismatch;
    private ConcurrencyConflictClassification _classification = ConcurrencyConflictClassification.Transient;
    private string _operation = "Update";
    private string _message = "Optimistic concurrency conflict detected during testing.";
    private ExpectedVersion? _expectedVersion = ExpectedVersion.Specific(1);
    private ActualVersion? _actualVersion = ActualVersion.From(2);
    private IConcurrencyToken? _expectedToken;
    private IConcurrencyToken? _actualToken;
    private readonly Dictionary<string, string> _metadata = [];

    /// <summary>
    /// Sets the target entity identifier for the conflict.
    /// </summary>
    /// <param name="entityId">The unique identifier of the entity.</param>
    /// <returns>This builder instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="entityId"/> is <see langword="null"/></exception>
    public ConcurrencyConflictBuilder WithEntityId(string entityId)
    {
        _entityId = entityId ?? throw new ArgumentNullException(nameof(entityId));
        return this;
    }

    /// <summary>
    /// Sets the target entity type name for the conflict.
    /// </summary>
    /// <param name="entityType">The type name of the entity.</param>
    /// <returns>This builder instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="entityType"/> is <see langword="null"/></exception>
    public ConcurrencyConflictBuilder WithEntityType(string entityType)
    {
        _entityType = entityType ?? throw new ArgumentNullException(nameof(entityType));
        return this;
    }

    /// <summary>
    /// Sets the conflict category discriminator.
    /// </summary>
    /// <param name="conflictType">The conflict category.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public ConcurrencyConflictBuilder WithConflictType(ConcurrencyConflictType conflictType)
    {
        _conflictType = conflictType;
        return this;
    }

    /// <summary>
    /// Sets the retryability classification.
    /// </summary>
    /// <param name="classification">The classification severity.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public ConcurrencyConflictBuilder WithClassification(ConcurrencyConflictClassification classification)
    {
        _classification = classification;
        return this;
    }

    /// <summary>
    /// Sets the operation name.
    /// </summary>
    /// <param name="operation">The operation name.</param>
    /// <returns>This builder instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="operation"/> is <see langword="null"/></exception>
    public ConcurrencyConflictBuilder WithOperation(string operation)
    {
        _operation = operation ?? throw new ArgumentNullException(nameof(operation));
        return this;
    }

    /// <summary>
    /// Sets the conflict descriptive message.
    /// </summary>
    /// <param name="message">The human-readable message.</param>
    /// <returns>This builder instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="message"/> is <see langword="null"/></exception>
    public ConcurrencyConflictBuilder WithMessage(string message)
    {
        _message = message ?? throw new ArgumentNullException(nameof(message));
        return this;
    }

    /// <summary>
    /// Sets the expected and actual numeric version constraints.
    /// </summary>
    /// <param name="expected">The expected version.</param>
    /// <param name="actual">The actual current version.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public ConcurrencyConflictBuilder WithVersions(ExpectedVersion expected, ActualVersion actual)
    {
        _expectedVersion = expected;
        _actualVersion = actual;
        return this;
    }

    /// <summary>
    /// Sets the expected and actual concurrency tokens.
    /// </summary>
    /// <param name="expected">The expected token.</param>
    /// <param name="actual">The actual current token.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public ConcurrencyConflictBuilder WithTokens(IConcurrencyToken expected, IConcurrencyToken actual)
    {
        _expectedToken = expected;
        _actualToken = actual;
        return this;
    }

    /// <summary>
    /// Adds a diagnostic metadata entry to the conflict.
    /// </summary>
    /// <param name="key">The metadata key.</param>
    /// <param name="value">The metadata value.</param>
    /// <returns>This builder instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/></exception>
    public ConcurrencyConflictBuilder WithMetadata(string key, string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        _metadata[key] = value;
        return this;
    }

    /// <summary>
    /// Builds the configured <see cref="ConcurrencyConflict"/> instance.
    /// </summary>
    /// <returns>A new <see cref="ConcurrencyConflict"/> with the configured attributes.</returns>
    public ConcurrencyConflict Build()
    {
        return new ConcurrencyConflict(
            entityId: _entityId,
            entityType: _entityType,
            conflictType: _conflictType,
            classification: _classification,
            operation: _operation,
            message: _message,
            expectedVersion: _expectedVersion,
            actualVersion: _actualVersion,
            expectedToken: _expectedToken,
            actualToken: _actualToken,
            metadata: _metadata);
    }

    /// <summary>
    /// Converts a <see cref="ConcurrencyConflictBuilder"/> to a <see cref="ConcurrencyConflict"/>.
    /// </summary>
    /// <param name="builder">The builder instance to convert.</param>
    /// <returns>The built <see cref="ConcurrencyConflict"/> instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/></exception>
    public static implicit operator ConcurrencyConflict(ConcurrencyConflictBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.Build();
    }
}
