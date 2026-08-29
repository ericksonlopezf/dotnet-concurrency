// Copyright © Erickson Lopez. MIT License.

namespace EricksonLopez.Concurrency.PostgreSql;

/// <summary>
/// Specifies PostgreSQL row-level locking modes used in pessimistic queries.
/// </summary>
public enum PostgreSqlLockMode : byte
{
    /// <summary>
    /// Locks selected rows with exclusive write lock (<c>FOR UPDATE</c>).
    /// </summary>
    ForUpdate = 0,

    /// <summary>
    /// Attempts to lock selected rows with exclusive write lock without waiting (<c>FOR UPDATE NOWAIT</c>).
    /// </summary>
    ForUpdateNoWait = 1,

    /// <summary>
    /// Locks available selected rows, skipping any rows currently locked by concurrent transactions (<c>FOR UPDATE SKIP LOCKED</c>).
    /// </summary>
    ForUpdateSkipLocked = 2,

    /// <summary>
    /// Locks selected rows with shared read lock (<c>FOR SHARE</c>).
    /// </summary>
    ForShare = 3,

    /// <summary>
    /// Locks selected rows without blocking non-key updates (<c>FOR NO KEY UPDATE</c>).
    /// </summary>
    ForNoKeyUpdate = 4
}
