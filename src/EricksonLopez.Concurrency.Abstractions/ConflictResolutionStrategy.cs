// Copyright © Erickson Lopez. MIT License.

namespace EricksonLopez.Concurrency.Abstractions;

/// <summary>
/// Defines the strategy used to resolve a detected optimistic concurrency conflict.
/// </summary>
public enum ConflictResolutionStrategy : byte
{
    /// <summary>Reject the update and return a conflict failure to the caller (default).</summary>
    Reject = 0,
    /// <summary>Explicitly overwrite storage with the latest in-memory state (explicit Last-Write-Wins; not recommended by default).</summary>
    LastWriteWinsExplicit = 1,
    /// <summary>Execute a domain-specific merge reconciling both states without losing business invariant consistency.</summary>
    MergeDomainSpecific = 2,
    /// <summary>Reload the latest state from storage, re-apply the domain mutation, and retry.</summary>
    RefreshAndRetry = 3
}
