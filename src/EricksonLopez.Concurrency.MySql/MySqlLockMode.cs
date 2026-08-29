// Copyright © Erickson Lopez. MIT License.

namespace EricksonLopez.Concurrency.MySql;

/// <summary>
/// Specifies MySQL locking clauses for pessimistic record locking.
/// </summary>
public enum MySqlLockMode
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
    ForUpdateSkipLocked,

    /// <summary>
    /// Shared read lock (<c>FOR SHARE</c>).
    /// </summary>
    ForShare
}
