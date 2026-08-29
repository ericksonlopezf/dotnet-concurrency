# Functional Architecture & Concurrency Flows

This document details the system architecture, execution flows, state transitions, and sequence diagrams for the **`EricksonLopez.Concurrency`** ecosystem.

---

## 🏛️ Overall Component Architecture

```mermaid
graph TD
    subgraph ClientLayer [Application / Ingress Layer]
        API[ASP.NET Core Web API / Minimal APIs]
        HttpExt[ConcurrencyHttpExtensions & ETag Middleware]
        Med[EricksonLopez.Mediator Pipeline]
    end

    subgraph CoreAbstractions [Abstractions & Core]
        Abs[EricksonLopez.Concurrency.Abstractions]
        Core[EricksonLopez.Concurrency]
        Structs["Zero-Allocation Structs (ConcurrencyVersion, ExpectedVersion, ConcurrencyToken)"]
        Controller[IConcurrencyController / ConcurrencyController]
        Checker[IConcurrencyChecker / OptimisticConcurrencyChecker]
    end

    subgraph IntegrationLayer [Integration & Infrastructure Layer]
        DapperPkg[EricksonLopez.Concurrency.Dapper]
        ResultPkg[EricksonLopez.Concurrency.Result]
        MediatorPkg[EricksonLopez.Concurrency.Mediator]
        TestingPkg["EricksonLopez.Concurrency.Testing (FakeConcurrencyController, ConcurrencyConflictBuilder)"]
        AspNetCorePkg["EricksonLopez.Concurrency.AspNetCore (ConcurrencyProblemDetails, Middleware)"]
    end

    subgraph DialectLayer [Database Dialects Layer]
        Pg["PostgreSql (xmin token / SQLSTATE 40001, 40P01)"]
        SqlServ["SqlServer (ROWVERSION token / 1205, 3960)"]
        MySql["MySql (1213, 1205, 1062)"]
        MariaDb["MariaDb (1213, 1205, WAIT n)"]
        Ora["Oracle (ORA_ROWSCN / ORA-00060, ORA-08177)"]
        Sqlite["Sqlite (SQLITE_BUSY, SQLITE_LOCKED)"]
    end

    subgraph DiagnosticsLayer [Observability & Telemetry]
        OTel["OpenTelemetry ActivitySource & Meter (concurrency.conflicts, successes, duration)"]
    end

    API --> HttpExt
    HttpExt --> AspNetCorePkg
    API --> Med
    Med --> MediatorPkg
    MediatorPkg --> Core
    MediatorPkg --> Abs
    AspNetCorePkg --> Abs
    TestingPkg --> Abs
    Abs --> Core
    Controller --> Checker
    Checker --> Structs
    Core --> ResultPkg
    Core --> DapperPkg
    Abs --> ResultPkg
    Abs --> DapperPkg
    Abs --> Pg
    Core --> Pg
    Abs --> SqlServ
    Abs --> MySql
    Abs --> MariaDb
    Abs --> Ora
    Abs --> Sqlite
    Core --> DiagnosticsLayer
```

---

## 🔄 State Transition Flow (In-Memory Compare-And-Swap)

```mermaid
stateDiagram-v2
    [*] --> InitialState : Entity loaded with Version = N
    InitialState --> VerifyCondition : ExecuteCasAsync(ExpectedVersion)
    
    VerifyCondition --> CheckMatch : ExpectedVersion.Matches(N)?
    
    state CheckMatch <<choice>>
    CheckMatch --> MutateState : true (Match)
    CheckMatch --> ConflictDetected : false (Mismatch)
    
    MutateState --> ApplyDelegate : mutate(entity, ct)
    ApplyDelegate --> IncrementVersion : Version = N.Next() (N + 1)
    IncrementVersion --> CasSuccess : CasResult.Succeeded(mutated, N + 1)
    CasSuccess --> [*]
    
    ConflictDetected --> BuildConflict : ConcurrencyConflict.VersionMismatch
    BuildConflict --> RecordMetrics : ConcurrencyDiagnostics.RecordConflict()
    RecordMetrics --> CasConflict : CasResult.Conflicted(conflict)
    CasConflict --> [*]
```

---

## 🗄️ Database Optimistic Write Sequence (Dapper Zero-Roundtrip)

```mermaid
sequenceDiagram
    autonumber
    actor Service as Application Service
    participant Repo as Repository
    participant Dapper as ConcurrencyDapperExtensions
    participant DB as Relational Database
    participant Diag as ConcurrencyDiagnostics

    Service->>Repo: UpdateAccount(account, expectedVersion = 1)
    Repo->>Dapper: connection.ExecuteOptimisticAsync(sql, param, expectedVersion)
    
    Note over Dapper,DB: UPDATE accounts SET balance = @Balance, version = version + 1<br/>WHERE id = @Id AND version = 1;
    Dapper->>DB: ExecuteAsync(command)
    
    alt Rows Affected == 1 (Success)
        DB-->>Dapper: rowsAffected = 1
        Dapper->>Diag: SuccessesCounter.Add(1)
        Dapper-->>Repo: return null (no conflict)
        Repo-->>Service: Result.Success()
    else Rows Affected == 0 (Concurrent Conflict)
        DB-->>Dapper: rowsAffected = 0
        Dapper->>Diag: ConflictsCounter.Add(1)
        Dapper-->>Repo: return ConcurrencyConflict.VersionMismatch()
        Repo-->>Service: Result.Failure(Error.Conflict("Concurrency.VersionMismatch"))
    end
```

---

## 🌐 ASP.NET Core RFC 7807 Conflict Handling Flow

```mermaid
sequenceDiagram
    autonumber
    actor Client as HTTP Client
    participant MW as ConcurrencyConflictMiddleware
    participant EP as Minimal API / Controller
    participant CC as IConcurrencyController

    Client->>MW: PUT /api/v1/accounts/101 (If-Match: "1")
    MW->>EP: Invoke Next
    EP->>CC: VerifyVersion(account, ExpectedVersion.Specific(1))
    
    alt Version Matches
        CC-->>EP: null (No conflict)
        EP-->>MW: 200 OK (ETag: "2")
        MW-->>Client: 200 OK (ETag: "2")
    else Version Mismatch -> Exception Thrown
        CC-->>EP: ConcurrencyConflict
        EP->>MW: throw ConcurrencyException(conflict)
        MW->>MW: Catch ConcurrencyException
        MW->>MW: Create ConcurrencyProblemDetails (Status: 409)
        MW-->>Client: 409 Conflict (application/problem+json)
    end
```

---

## 🚦 Architectural Demarcation: Concurrency vs Resilience (ADR-001)

```mermaid
flowchart LR
    A[Write Operation] --> B{Conflict Detected?}
    B -- No --> C[Success: State Persisted]
    B -- Yes --> D[EricksonLopez.Concurrency]
    
    subgraph ConcurrencyScope [Scope: Concurrency]
        D --> E[Classify Conflict]
        E --> F1[Transient: Deadlock 40P01 / 1205]
        E --> F2[StaleState: Version Mismatch]
        E --> F3[NonRetryable: Entity Deleted]
        F1 --> G1[Return ConcurrencyConflict / Error]
        F2 --> G2[Return ConcurrencyConflict / Error]
        F3 --> G3[Return ConcurrencyConflict / Error]
    end
    
    subgraph ResilienceScope [Scope: Resilience / Outer Policies]
        G1 --> H1[Transactional Retry with Backoff & Jitter]
        G2 --> H2[Reload State from DB and Reapply Invariant]
        G3 --> H3[Notify Client: HTTP 404 / 409 / 412]
    end
```
