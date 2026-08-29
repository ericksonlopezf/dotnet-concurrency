// Copyright © Erickson Lopez. MIT License.

namespace EricksonLopez.Concurrency.Abstractions;

/// <summary>
/// Specifies the nature and category of a detected concurrency conflict.
/// </summary>
public enum ConcurrencyConflictType : byte
{
    /// <summary>The entity version does not match the expected version.</summary>
    VersionMismatch = 0,
    /// <summary>The concurrency token does not match the expected token.</summary>
    TokenMismatch = 1,
    /// <summary>The target entity was deleted or not found during update execution.</summary>
    StateDeleted = 2,
    /// <summary>The target entity already exists when expected to be new.</summary>
    AlreadyExists = 3,
    /// <summary>A database transaction serialization failure occurred (e.g. PostgreSQL SQLSTATE 40001).</summary>
    SerializationFailure = 4,
    /// <summary>A database deadlock was detected between concurrent operations (e.g. PostgreSQL SQLSTATE 40P01).</summary>
    Deadlock = 5,
    /// <summary>A lock acquisition timed out or was unavailable (e.g. PostgreSQL NOWAIT / 55P03).</summary>
    LockUnavailable = 6,
    /// <summary>Custom or user-defined conflict condition.</summary>
    Custom = 7
}
