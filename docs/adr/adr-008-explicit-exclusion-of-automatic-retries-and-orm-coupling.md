# ADR-008: Explicit Exclusion of Automatic Retries, Distributed Locks, and ORM Coupling from Core Scope

- **Status**: Accepted
- **Date**: 2026-08-18
- **Component**: EricksonLopez.Concurrency

## Context
As concurrency frameworks evolve, there is constant pressure to add generic distributed locks, background retries, ORM extensions, event stream versioning, and saga coordination.

## Decision
The following features are permanently out of scope: automatic retry/backoff loops in core, distributed locks (Redis/Consul), Entity Framework Core DbContext coupling, Event Sourcing streams, CRDT, and Saga orchestration.

## Consequences
Controlled API surface, zero dependency bloat, pristine architectural boundaries, and ultra-high reliability.

## Compliance & Invariants
- **Native AOT Compatible**: Yes
- **Zero Heap Allocations in Hot Path**: Yes
- **Thread Safety Guaranteed**: Yes
