# ADR-009: Testability and Mock-Free Test Doubles via FakeConcurrencyController

- **Status**: Accepted
- **Date**: 2026-08-19
- **Component**: EricksonLopez.Concurrency

## Context
Unit testing domain command handlers that depend on IConcurrencyController traditionally required mocking frameworks. Mocking value structs, generic delegates, and asynchronous CAS calls introduces test fragility and verbosity.

## Decision
Provide a dedicated EricksonLopez.Concurrency.Testing package containing FakeConcurrencyController and ConcurrencyConflictBuilder.

## Consequences
Developers can verify optimistic concurrency behavior, simulate conflicts, queue transient retries, and inspect invocation histories with zero mocking boilerplate.

## Compliance & Invariants
- **Native AOT Compatible**: Yes
- **Zero Heap Allocations in Hot Path**: Yes
- **Thread Safety Guaranteed**: Yes
