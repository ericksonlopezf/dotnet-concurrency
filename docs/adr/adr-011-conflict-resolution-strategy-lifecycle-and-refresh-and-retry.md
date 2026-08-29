# ADR-011: Conflict Resolution Strategy Lifecycle and RefreshAndRetry Resolver Pattern

- **Status**: Accepted
- **Date**: 2026-08-21
- **Component**: EricksonLopez.Concurrency

## Context
When optimistic concurrency conflicts occur, some domain applications need to automatically reload the latest persistent state, reconcile differences, and retry the operation.

## Decision
Provide RefreshAndRetryConflictResolver<TEntity> implementing IConcurrencyConflictResolver<TEntity>. The resolver verifies that the conflict classification is retryable, invokes a storage re-fetch delegate, optionally applies a custom domain merge delegate, and outputs ConflictResolution<TEntity>.

## Consequences
Predictable conflict reconciliation; domain-level control over re-evaluation; clean isolation between database queries and domain logic.

## Compliance & Invariants
- **Native AOT Compatible**: Yes
- **Zero Heap Allocations in Hot Path**: Yes
- **Thread Safety Guaranteed**: Yes
