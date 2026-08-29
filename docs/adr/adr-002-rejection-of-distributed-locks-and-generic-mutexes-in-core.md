# ADR-002: Rejection of Distributed Locks and Generic Mutexes in Core

- **Status**: Accepted
- **Date**: 2026-08-15
- **Component**: EricksonLopez.Concurrency

## Context
Distributed locks (e.g. Redis Redlock, Consul mutexes) introduce external network dependencies, deadlocks, lease expiration hazards, and high latency.

## Decision
Exclude distributed locks, Redis locks, and generic mutex wrappers from EricksonLopez.Concurrency. Concurrency control in this framework is optimistic-first and database-arbitrated.

## Consequences
Ultra-high throughput, predictable execution paths, no external cluster lock dependencies.

## Compliance & Invariants
- **Native AOT Compatible**: Yes
- **Zero Heap Allocations in Hot Path**: Yes
- **Thread Safety Guaranteed**: Yes
