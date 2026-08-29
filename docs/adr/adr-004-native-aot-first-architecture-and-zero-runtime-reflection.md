# ADR-004: Native AOT First Architecture and Zero Runtime Reflection

- **Status**: Accepted
- **Date**: 2026-08-16
- **Component**: EricksonLopez.Concurrency

## Context
Modern microservices require rapid startup, low memory footprint, and Native AOT compilation compatibility.

## Decision
Enforce EnableTrimAnalyzer=true and TreatWarningsAsErrors=true. All dependency injection extensions and serializers must avoid unannotated runtime reflection.

## Consequences
100% Native AOT trimming safe; fast cold starts in containerized environments.

## Compliance & Invariants
- **Native AOT Compatible**: Yes
- **Zero Heap Allocations in Hot Path**: Yes
- **Thread Safety Guaranteed**: Yes
