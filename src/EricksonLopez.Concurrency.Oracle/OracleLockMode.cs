// Copyright © Erickson Lopez. MIT License.

namespace EricksonLopez.Concurrency.Oracle;

/// <summary>
/// Specifies Oracle locking clauses for pessimistic record locking.
/// </summary>
public enum OracleLockMode
{
    /// <summary>
    /// Exclusive write lock (<c>FOR UPDATE</c>).
    /// </summary>
    ForUpdate,

    /// <summary>
    /// Exclusive write lock failing immediately if locked (<c>FOR UPDATE NOWAIT</c>).
    /// </summary>
    ForUpdateNowait,

    /// <summary>
    /// Exclusive write lock skipping locked records (<c>FOR UPDATE SKIP LOCKED</c>).
    /// </summary>
    ForUpdateSkipLocked
}
