# SQLite Concurrency Integration Guide

## Overview

The `EricksonLopez.Concurrency.Sqlite` package provides lightweight SQLite file database concurrency handling, lock contention detection, and SQLite error code classification using `Microsoft.Data.Sqlite`.

---

## 1. SQLite Concurrency Characteristics

SQLite is a serverless, file-backed database engine. Write operations lock the entire database file (unless WAL mode is enabled).

Lock contention manifests primarily as `SQLITE_BUSY` (another process is holding a lock) or `SQLITE_LOCKED` (another thread within the same process is holding a lock).

---

## 2. SQLite Error Classification

`SqliteConcurrencyErrorClassifier` maps SQLite extended result codes:

| Result Code | Name | Classification |
|---|---|---|
| `5` | `SQLITE_BUSY` | `LockNotAvailable` (Transient) |
| `6` | `SQLITE_LOCKED` | `LockNotAvailable` (Transient) |
| `19` | `SQLITE_CONSTRAINT` | `UniqueViolation` (Permanent) |

---

## 3. Dependency Injection

```csharp
builder.Services.AddEricksonLopezConcurrencySqlite();
```
