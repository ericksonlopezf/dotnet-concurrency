// Copyright © Erickson Lopez. MIT License.

namespace EricksonLopez.Concurrency.Abstractions;

/// <summary>
/// Classifies a concurrency conflict to guide higher-level resilience, retry, and operational decisions.
/// </summary>
public enum ConcurrencyConflictClassification : byte
{
    /// <summary>Transient database conflict (e.g. deadlock, serialization failure) that may succeed upon immediate transaction retry.</summary>
    Transient = 0,
    /// <summary>Conflict that is safe to retry by reloading state and reapplying domain invariants.</summary>
    Retryable = 1,
    /// <summary>Non-retryable state conflict that requires client interaction or manual resolution.</summary>
    NonRetryable = 2,
    /// <summary>Stale client state where the resource has significantly progressed.</summary>
    StaleState = 3,
    /// <summary>Fatal or corrupted concurrency condition.</summary>
    Fatal = 4
}
