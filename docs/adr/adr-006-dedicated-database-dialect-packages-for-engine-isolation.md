# ADR-006: Dedicated Database Dialect Packages for Engine Isolation

- **Status**: Accepted
- **Date**: 2026-08-17
- **Component**: EricksonLopez.Concurrency

## Context
Database-specific error classification requires driver-specific references (Npgsql, Microsoft.Data.SqlClient, MySqlConnector, Oracle.ManagedDataAccess.Core, Microsoft.Data.Sqlite). Forcing all ADO.NET drivers onto every application bloats dependency graphs and binary sizes.

## Decision
Segregate each database provider into its own standalone package referencing only Abstractions and the specific ADO.NET driver.

## Consequences
Applications only reference their target database driver; minimal dependency footprint; zero transitive driver bloat.

## Compliance & Invariants
- **Native AOT Compatible**: Yes
- **Zero Heap Allocations in Hot Path**: Yes
- **Thread Safety Guaranteed**: Yes
