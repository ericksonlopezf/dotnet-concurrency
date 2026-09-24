# ADR-013: In-Memory Striped Lock Pooling and Reentrancy Guards for CAS Operations

## Status
Accepted

## Date
2026-09-23

- **Status**: Accepted
- **Date**: 2026-09-23
- **Component**: EricksonLopez.Concurrency (Core & Abstractions)

## Context
While optimistic concurrency control is fundamentally arbitrated at the database layer (via version-conditioned `UPDATE` statements), hot in-memory state mutations via `IConcurrencyController.ExecuteCasAsync` require thread safety when multiple worker threads within the same process execute concurrent mutations against the same entity ID.

Previous iterations left in-memory mutual exclusion entirely to the caller (ADR-007 Addendum). However, this created two significant challenges:
1. Callers frequently forgot to synchronize, leading to in-memory lost updates before database persistence.
2. Naive per-entity locking introduces memory leaks (retaining locks indefinitely in a dictionary) or lock contention bottlenecks (global lock).
3. Re-entrant execution paths (e.g., domain events or nested domain logic invoking CAS on the same entity) led to self-deadlocks.

## Decision
1. **Striped RefCounted Lock Pooling**: Implement an internal, zero-allocation striped lock pool (`RefCountedLock`) keyed by aggregate/entity ID. Semaphores are acquired on-demand, reference-counted, and returned to the pool when the count reaches zero, avoiding unbounded memory growth.
2. **Reentrancy Protection via AsyncLocal (Fail-Fast)**: Utilize an `AsyncLocal<ReentrancyNode>` call-context chain to track currently held locks within the asynchronous execution flow. Nested or recursive CAS calls for the same entity ID within the same async context are explicitly detected and rejected by throwing `InvalidOperationException` (fail-fast), eliminating self-deadlocks and preventing corrupted intermediate state.
3. **Dual Timeout Boundaries**: Expose `DefaultMaxAcquisitionTimeout` (default 10s) and `DefaultMaxExecutionTimeout` (default 30s) on `ConcurrencyOptions`, guaranteeing that lock acquisition and mutation execution cannot hang indefinitely under extreme contention.
4. **Direct In-Place Mutation Support**: Introduce `IMutableVersionedEntity` to allow zero-allocation version advancement directly on mutable entities without wrapping or cloning.

## Consequences
- Thread-safe, leak-free in-memory CAS mutations out of the box.
- Safe execution with deterministic deadlock prevention: recursive CAS invocations fail fast with clear diagnostic exceptions.
- Bounded latency under extreme contention via configurable acquisition and execution timeouts (`DefaultMaxAcquisitionTimeout`, `DefaultMaxExecutionTimeout`).
- 100% Native AOT trimming safe with zero runtime reflection.

## Compliance & Invariants
- **Native AOT Compatible**: Yes
- **Zero Memory Leaks on Pooled Locks**: Yes (reference counting with automatic cleanup)
- **Reentrancy Deadlock Prevention**: Yes (call-context tracking with fail-fast rejection)
- **Thread Safety Guaranteed**: Yes
