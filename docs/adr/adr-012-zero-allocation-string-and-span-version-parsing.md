# ADR-012: Zero-Allocation String and Span-Based Version Parsing Protocols

- **Status**: Accepted
- **Date**: 2026-08-22
- **Component**: EricksonLopez.Concurrency

## Context
Numeric version numbers frequently arrive from HTTP headers (If-Match), query parameters, route variables, or distributed messages as strings or character spans.

## Decision
Implement ISpanParsable<T> and IParsable<T> on ConcurrencyVersion and ConcurrencyVersion<TEntity>, exposing zero-allocation TryParse(ReadOnlySpan<char>, ...) and TryParse(string, ...) methods.

## Consequences
High-performance, non-allocating version validation across web endpoints and message consumers.

## Compliance & Invariants
- **Native AOT Compatible**: Yes
- **Zero Heap Allocations in Hot Path**: Yes
- **Thread Safety Guaranteed**: Yes
