# ADR-010: ASP.NET Core Integration, RFC 7807 ProblemDetails, and HTTP ETag Semantics

- **Status**: Accepted
- **Date**: 2026-08-20
- **Component**: EricksonLopez.Concurrency

## Context
Web APIs built with ASP.NET Core require standardized translation of optimistic concurrency conflicts into HTTP 409 Conflict status codes, RFC 7807 ProblemDetails response bodies, and HTTP ETag / If-Match precondition headers.

## Decision
Provide EricksonLopez.Concurrency.AspNetCore with ConcurrencyConflictMiddleware, ConcurrencyProblemDetails, Results.Extensions.ConcurrencyConflict, and HTTP header helpers.

## Consequences
Turnkey REST API integration with 1 line of middleware registration; 100% Native AOT trimming safe.

## Compliance & Invariants
- **Native AOT Compatible**: Yes
- **Zero Heap Allocations in Hot Path**: Yes
- **Thread Safety Guaranteed**: Yes
