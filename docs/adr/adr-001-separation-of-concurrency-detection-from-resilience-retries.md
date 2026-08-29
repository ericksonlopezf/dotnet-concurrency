# ADR-001: Separation of Concurrency Detection from Resilience Retries

- **Status**: Accepted
- **Date**: 2026-08-15
- **Component**: EricksonLopez.Concurrency

## Context
Concurrency conflicts can occur due to stale user updates or transient database race conditions. Combining automatic retry logic into the concurrency layer couples it to retry policies, backoff strategies, and resilience mechanics.

## Decision
EricksonLopez.Concurrency is strictly responsible for detecting, classifying, and reporting conflicts (Transient, StaleState, NonRetryable). Retries must be orchestrated by EricksonLopez.Resilience or explicit application policies.

## Consequences
Zero circular dependencies between Concurrency and Resilience; clean Single Responsibility Principle.

## Compliance & Invariants
- **Native AOT Compatible**: Yes
- **Zero Heap Allocations in Hot Path**: Yes
- **Thread Safety Guaranteed**: Yes
