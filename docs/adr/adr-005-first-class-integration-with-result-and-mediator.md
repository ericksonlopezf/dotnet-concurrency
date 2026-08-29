# ADR-005: First-Class Integration with EricksonLopez.Result and EricksonLopez.Mediator

- **Status**: Accepted
- **Date**: 2026-08-17
- **Component**: EricksonLopez.Concurrency

## Context
The EricksonLopez ecosystem utilizes functional error handling via Result<T> and zero-allocation CQRS via EricksonLopez.Mediator.

## Decision
Provide dedicated integration packages EricksonLopez.Concurrency.Result and EricksonLopez.Concurrency.Mediator that translate conflicts into typed Error models and observability pipeline behaviors.

## Consequences
Harmonious developer experience across all tier-1 ecosystem libraries without polluting core abstractions.

## Compliance & Invariants
- **Native AOT Compatible**: Yes
- **Zero Heap Allocations in Hot Path**: Yes
- **Thread Safety Guaranteed**: Yes
