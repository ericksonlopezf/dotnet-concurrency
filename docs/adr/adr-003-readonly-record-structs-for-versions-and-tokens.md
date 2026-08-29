# ADR-003: Readonly Record Structs for Versions and Tokens

- **Status**: Accepted
- **Date**: 2026-08-16
- **Component**: EricksonLopez.Concurrency

## Context
Version numbers and tokens are evaluated on every write path across high-frequency transaction pipelines. Allocating heap objects for each check creates unnecessary GC pressure.

## Decision
Represent ConcurrencyVersion, ConcurrencyVersion<T>, ConcurrencyToken, ExpectedVersion, ActualVersion, and XminConcurrencyToken as readonly record struct value types.

## Consequences
Zero heap allocations on check paths, sub-nanosecond comparison performance.

## Compliance & Invariants
- **Native AOT Compatible**: Yes
- **Zero Heap Allocations in Hot Path**: Yes
- **Thread Safety Guaranteed**: Yes
