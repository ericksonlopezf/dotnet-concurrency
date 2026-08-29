# ADR-007: Domain Invariant Compare-And-Swap (CAS) Mutation Boundaries

- **Status**: Accepted
- **Date**: 2026-08-18
- **Component**: EricksonLopez.Concurrency

## Context
In-memory domain aggregates require safe state transitions when subjected to concurrent actor calls or asynchronous batch processes.

## Decision
Provide IConcurrencyController.ExecuteCasAsync<TEntity> which validates version preconditions before applying the domain mutation delegate and monotonically advancing Version.Next().

## Consequences
Predictable in-memory state transitions; prevention of Lost Updates before database persistence; clean integration with OpenTelemetry metrics.

## Compliance & Invariants
- **Native AOT Compatible**: Yes
- **Zero Heap Allocations in Hot Path**: Yes
- **Thread Safety Guaranteed**: Yes
