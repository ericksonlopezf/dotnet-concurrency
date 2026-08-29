// Copyright © Erickson Lopez. MIT License.
using System.Diagnostics.CodeAnalysis;

namespace EricksonLopez.Concurrency.Abstractions;

/// <summary>
/// Defines a contract for zero-allocation synchronous evaluation of optimistic concurrency conditions and tokens.
/// </summary>
public interface IConcurrencyChecker
{
    /// <summary>
    /// Evaluates whether an expected version matches the actual version found in persistence or memory.
    /// </summary>
    /// <param name="expected">The expected version constraint.</param>
    /// <param name="actual">The actual concurrency version found.</param>
    /// <param name="entityId">The unique identifier of the entity.</param>
    /// <param name="entityType">The type name or discriminator of the entity.</param>
    /// <param name="conflict">When this method returns <see langword="false"/>, contains the populated conflict descriptor; otherwise, <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if the version requirement is satisfied; otherwise, <see langword="false"/>.</returns>
    bool CheckVersion(
        ExpectedVersion expected,
        ConcurrencyVersion actual,
        string entityId,
        string entityType,
        [NotNullWhen(false)] out ConcurrencyConflict? conflict);

    /// <summary>
    /// Evaluates whether an expected concurrency token matches the actual token found in storage or memory.
    /// </summary>
    /// <param name="expected">The expected concurrency token constraint.</param>
    /// <param name="actual">The actual concurrency token found.</param>
    /// <param name="entityId">The unique identifier of the entity.</param>
    /// <param name="entityType">The type name or discriminator of the entity.</param>
    /// <param name="conflict">When this method returns <see langword="false"/>, contains the populated conflict descriptor; otherwise, <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if the tokens match; otherwise, <see langword="false"/>.</returns>
    bool CheckToken(
        IConcurrencyToken expected,
        IConcurrencyToken actual,
        string entityId,
        string entityType,
        [NotNullWhen(false)] out ConcurrencyConflict? conflict);
}
