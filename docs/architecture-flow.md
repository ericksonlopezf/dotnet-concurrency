# Functional Architecture & Execution Flows

This document details the **Complete Functional Map**, layer transitions, and **Official Architectural Diagrams (Mermaid)** for the **`EricksonLopez.Concurrency`** ecosystem.

---

## 🗺️ Functional Map: Ingestion to Output

The ecosystem operates across 8 structured functional layers guaranteeing transactional integrity, lock-free optimistic conflict detection, and end-to-end distributed observability:

```mermaid
flowchart TD
    subgraph Layer1 [1. Ingestion / Entry Point]
        HTTP["HTTP API / Minimal API (If-Match, ETag)"]
        CQRS["CQRS Command (IConcurrencyAwareRequest)"]
        MemoryClient["In-Memory Client (Worker / Service)"]
    end

    subgraph Layer2 [2. Dispatch Layer]
        MedBehavior["ConcurrencyBehavior<TRequest, TResponse>"]
        HttpMW["ConcurrencyConflictMiddleware"]
    end

    subgraph Layer3 [3. Processing Layer]
        Checker["OptimisticConcurrencyChecker (Zero-Alloc)"]
        CAS["ConcurrencyController (ExecuteCasAsync)"]
    end

    subgraph Layer4 [4. Persistence Layer]
        UpdateBuilder["OptimisticUpdateBuilder (Generated SQL)"]
        DapperExt["ConcurrencyDapperExtensions (ExecuteOptimisticAsync)"]
        Engine["Relational Database Engine (Postgres, SQL Server, MySQL, etc.)"]
    end

    subgraph Layer5 [5. Interception & Classification Layer]
        Classifiers["Dialect Classifiers (SQLSTATE / Native Error Codes)"]
        RowsAffectedCheck{"Rows Affected > 0?"}
    end

    subgraph Layer6 [6. Publishing & Reconciliation Layer]
        Resolvers["IConcurrencyConflictResolver (Reject, Merge, LWW, RefreshAndRetry)"]
        ResultMonad["ConcurrencyResultExtensions (Mapping to Result / Error)"]
    end

    subgraph Layer7 [7. Confirmation & Telemetry]
        OTel["ConcurrencyDiagnostics (ActivitySource, Meter, Tags)"]
        SuccessConfirm["Persisted State Confirmation / Version N+1"]
    end

    subgraph Layer8 [8. Teardown & Response]
        HttpResp["HTTP 200 OK (ETag) / HTTP 409 ProblemDetails"]
        CasFinish["CasResult / Control Returned to Invoker"]
    end

    HTTP --> HttpMW
    CQRS --> MedBehavior
    MemoryClient --> CAS

    HttpMW --> Checker
    MedBehavior --> Checker
    MedBehavior --> CAS

    Checker --> UpdateBuilder
    CAS --> UpdateBuilder
    UpdateBuilder --> DapperExt
    DapperExt --> Engine

    Engine --> RowsAffectedCheck
    Engine -. Exception .-> Classifiers

    RowsAffectedCheck -- Yes (1 row) --> SuccessConfirm
    RowsAffectedCheck -- No (0 rows) --> Resolvers
    Classifiers --> Resolvers

    Resolvers --> ResultMonad
    SuccessConfirm --> OTel
    ResultMonad --> OTel

    OTel --> HttpResp
    OTel --> CasFinish
```

---

### Layer Transitions Explained

1. **Ingestion → Dispatch Layer**:
   - An HTTP request carries `If-Match: "<version>"` or a C# command implements `IConcurrencyAwareRequest`.
   - The dispatch layer (`ConcurrencyConflictMiddleware` or `ConcurrencyBehavior`) captures the precondition (`ExpectedVersion` or `ConcurrencyToken`) and activates an OpenTelemetry `Activity`.

2. **Dispatch → Processing Layer**:
   - For in-memory workflows, `ConcurrencyController.ExecuteCasAsync` acquires an entity-scoped striped lock (`RefCountedLock`), checks for reentrant calls via `AsyncLocal<ReentrancyNode>`, and verifies the expected version against the current entity version.
   - If preconditions fail, execution halts immediately without hitting the database infrastructure.

3. **Processing → Persistence Layer**:
   - `OptimisticUpdateBuilder` constructs a parameterized SQL statement: `UPDATE ... WHERE id = @Id AND version = @ExpectedVersion` (appending `tenant_id = @TenantId` in multi-tenant environments).
   - The query executes via Dapper's `ExecuteOptimisticAsync` in a single network roundtrip.

4. **Persistence → Interception & Classification Layer**:
   - If the database updates 1 row, the transaction succeeded.
   - If 0 rows are affected, a `ConcurrencyConflict.VersionMismatch` is synthesized.
   - If a database exception occurs (e.g. PostgreSQL `40001` serialization failure or `40P01` deadlock, SQL Server `1205`), the dialect classifier (`PostgreSqlConcurrencyErrorClassifier`, `SqlServerErrorClassifier`, etc.) extracts diagnostic metadata and produces a structured conflict record.

5. **Classification → Reconciliation & Publishing Layer**:
   - The conflict is categorized into an operational classification (`Transient`, `StaleState`, `NonRetryable`).
   - If an `IConcurrencyConflictResolver<TEntity>` is configured, it attempts resolution (`Reject`, `Delegate`, `LastWriteWins`, `RefreshAndRetry`).
   - Unresolved conflicts are translated into a `Result.Failure(Error.Conflict)` via `ConcurrencyResultExtensions`.

6. **Publishing → Confirmation, Teardown & Response**:
   - `ConcurrencyDiagnostics` records execution duration and emits telemetry metrics (`concurrency.successes` or `concurrency.conflicts`).
   - In ASP.NET Core, `ConcurrencyConflictMiddleware` catches unhandled `ConcurrencyException` instances and serializes them into RFC 7807 `ConcurrencyProblemDetails` with HTTP status 409 Conflict and an updated `ETag`.

---

## 🏛️ Architectural Diagrams (Mermaid)

### 1. General System Architecture

```mermaid
graph TD
    subgraph ClientApplications [Consumer & Application Layer]
        WebAPI[ASP.NET Core Web API / Minimal APIs]
        Worker[Background Services / Worker Services]
        CommandBus[EricksonLopez.Mediator CQRS Pipeline]
    end

    subgraph CoreDomain [Abstractions & Core Layer]
        Abstractions[EricksonLopez.Concurrency.Abstractions]
        CorePkg[EricksonLopez.Concurrency]
        ValueStructs["Value Structs: ConcurrencyVersion, ExpectedVersion, ConcurrencyToken"]
        Controller[IConcurrencyController / ConcurrencyController]
        Checker[IConcurrencyChecker / OptimisticConcurrencyChecker]
        Resolvers[Conflict Resolvers: Reject, Delegate, LWW, RefreshAndRetry]
    end

    subgraph InfrastructureProviders [Infrastructure Providers]
        DapperExt[EricksonLopez.Concurrency.Dapper]
        AspNetCoreExt[EricksonLopez.Concurrency.AspNetCore]
        ResultExt[EricksonLopez.Concurrency.Result]
        MediatorExt[EricksonLopez.Concurrency.Mediator]
        TestingExt[EricksonLopez.Concurrency.Testing]
    end

    subgraph DialectDrivers [Database Dialects]
        PG[EricksonLopez.Concurrency.PostgreSql]
        MSSQL[EricksonLopez.Concurrency.SqlServer]
        MySQL[EricksonLopez.Concurrency.MySql]
        MariaDB[EricksonLopez.Concurrency.MariaDb]
        Oracle[EricksonLopez.Concurrency.Oracle]
        SQLite[EricksonLopez.Concurrency.Sqlite]
    end

    subgraph ObservabilityStack [Observability Stack]
        OTelDiagnostics[ConcurrencyDiagnostics: Meter & ActivitySource]
    end

    WebAPI --> AspNetCoreExt
    WebAPI --> CommandBus
    Worker --> Controller
    CommandBus --> MediatorExt
    MediatorExt --> Controller

    AspNetCoreExt --> Abstractions
    MediatorExt --> Abstractions
    DapperExt --> Abstractions
    ResultExt --> Abstractions
    TestingExt --> Abstractions

    PG --> Abstractions
    MSSQL --> Abstractions
    MySQL --> Abstractions
    MariaDB --> Abstractions
    Oracle --> Abstractions
    SQLite --> Abstractions

    Abstractions --> CorePkg
    CorePkg --> OTelDiagnostics
```

---

### 2. End-to-End Processing Flow

```mermaid
flowchart TD
    Start([Request Ingestion]) --> ExtractPrecondition[Extract Precondition: ExpectedVersion or ETag]
    ExtractPrecondition --> CheckInMemory{In-Memory or Database?}
    
    CheckInMemory -- In-Memory --> ExecuteCas[ExecuteCasAsync on ConcurrencyController]
    ExecuteCas --> AcquireLock[Acquire Entity-Scoped Striped Lock]
    AcquireLock --> CheckReentrancy{Reentrant on EntityId?}
    CheckReentrancy -- Yes --> ThrowInvalidOp[Throw InvalidOperationException]
    CheckReentrancy -- No --> CasMatch{ExpectedVersion Matches?}
    CasMatch -- Yes --> ApplyMutation[Execute mutate delegate & Advance Version]
    ApplyMutation --> ReleaseLockSuccess[Release Entity Lock]
    ReleaseLockSuccess --> CasSuccess([CAS Succeeded: CasResult.Succeeded])
    CasMatch -- No --> ReleaseLockConflict[Release Entity Lock]
    ReleaseLockConflict --> ReturnCasConflict[Build ConcurrencyConflict.VersionMismatch]
    ReturnCasConflict --> ResolveConflict
    
    CheckInMemory -- Database --> BuildSql[OptimisticUpdateBuilder: WHERE id = @Id AND version = @Expected]
    BuildSql --> ExecuteSql[connection.ExecuteOptimisticAsync]
    ExecuteSql --> SqlResult{RowsAffected > 0?}
    
    SqlResult -- Yes (1) --> SqlSuccess([Persistence Success: Version Advanced])
    SqlResult -- No (0) --> DetectConflict[Detect ConcurrencyConflict.VersionMismatch]
    ExecuteSql -. DB Exception .-> ClassifyDbError[Dialect Classifier: SQLSTATE / Error Code]
    ClassifyDbError --> DetectConflict
    
    DetectConflict --> ResolveConflict{Resolver Configured?}
    ResolveConflict -- Yes --> RunResolver[IConcurrencyConflictResolver.ResolveAsync]
    RunResolver --> ResolverOutcome{Resolved?}
    ResolverOutcome -- Yes --> AppliedResolvedState([Reconciled & Reapplied State])
    ResolverOutcome -- No --> MapError[Map to Error.Conflict / ConcurrencyProblemDetails]
    
    ResolveConflict -- No --> MapError
    MapError --> FinalFailure([Return HTTP 409 or Result.Failure])
```

---

### 3. Sequence Diagram: Optimistic Verification & Dapper Persistence

```mermaid
sequenceDiagram
    autonumber
    actor Client as Invoker / Client
    participant Handler as CommandHandler / Application Service
    participant Dapper as ConcurrencyDapperExtensions
    participant DB as Relational Database
    participant Classifier as Dialect Error Classifier
    participant Diag as ConcurrencyDiagnostics

    Client->>Handler: Execute Mutation (Id = "ACC-101", ExpectedVersion = 2)
    Handler->>Dapper: connection.ExecuteOptimisticAsync(sql, param, ExpectedVersion.Specific(2))
    
    Note over Dapper,DB: UPDATE accounts SET balance = @Balance, version = version + 1<br/>WHERE id = @Id AND version = 2;
    Dapper->>DB: ExecuteAsync(command)
    
    alt Success: 1 Row Affected
        DB-->>Dapper: rowsAffected = 1
        Dapper->>Diag: RecordSuccess("Account")
        Dapper-->>Handler: null (No conflict)
        Handler-->>Client: Result.Success(UpdatedState)
    else Collision: 0 Rows Affected
        DB-->>Dapper: rowsAffected = 0
        Dapper->>Diag: RecordConflict("VersionMismatch", "Account")
        Dapper-->>Handler: ConcurrencyConflict.VersionMismatch("ACC-101", "Account")
        Handler-->>Client: Result.Failure(Error.Conflict)
    else Concurrency Exception (e.g. Deadlock / Serialization Failure)
        DB-->>Dapper: Exception (NpgsqlException / SqlException)
        Dapper->>Classifier: ToConcurrencyConflict(ex, "ACC-101", "Account")
        Classifier-->>Dapper: ConcurrencyConflict (Classification: Transient)
        Dapper-->>Handler: ConcurrencyConflict
        Handler-->>Client: Result.Failure(Error.Conflict [Transient])
    end
```

---

### 4. State Diagram: Versioned Entity Lifecycle

```mermaid
stateDiagram-v2
    [*] --> Uninitialized : In-memory entity (Version = 0)
    
    Uninitialized --> Created : Successful insertion (ExpectedVersion.New) -> Version = 1
    
    Created --> Modified : CAS mutation / Successful update (ExpectedVersion.Specific(N)) -> Version = N + 1
    Modified --> Modified : Subsequent mutations (Version = N + 1)
    
    Modified --> ConflictDetected : Write attempt with ExpectedVersion != CurrentVersion
    
    state ConflictDetected {
        [*] --> EvaluatingStrategy
        EvaluatingStrategy --> Rejected : RejectConflictResolver (Strict)
        EvaluatingStrategy --> Reconciled : DelegateConflictResolver (Merge)
        EvaluatingStrategy --> Overwritten : LastWriteWinsConflictResolver (LWW)
        EvaluatingStrategy --> Refreshed : RefreshAndRetryConflictResolver
    }
    
    Reconciled --> Modified : Version = CurrentVersion + 1
    Overwritten --> Modified : Version = CurrentVersion + 1
    Refreshed --> Modified : Version = CurrentVersion + 1
    
    Rejected --> ErrorReturned : Error.Conflict / HTTP 409 ProblemDetails
    ErrorReturned --> [*]
    
    Modified --> Deleted : Soft / Physical deletion -> ActualVersion.NotFound
    Deleted --> [*]
```

---

### 5. Component Dependency Graph

```mermaid
graph TD
    subgraph CoreAndAbstractions [Core & Abstractions - Zero External Dependencies]
        Abs[EricksonLopez.Concurrency.Abstractions]
        Core[EricksonLopez.Concurrency]
        Abs --> Core
    end

    subgraph AdaptersAndIntegrations [Adapters & Infrastructure]
        DapperPkg[EricksonLopez.Concurrency.Dapper]
        ResultPkg[EricksonLopez.Concurrency.Result]
        MediatorPkg[EricksonLopez.Concurrency.Mediator]
        TestingPkg[EricksonLopez.Concurrency.Testing]
        AspNetCorePkg[EricksonLopez.Concurrency.AspNetCore]
    end

    subgraph DialectPackages [Dialect Classifiers]
        PG[EricksonLopez.Concurrency.PostgreSql]
        MSSQL[EricksonLopez.Concurrency.SqlServer]
        MySQL[EricksonLopez.Concurrency.MySql]
        MariaDB[EricksonLopez.Concurrency.MariaDb]
        Oracle[EricksonLopez.Concurrency.Oracle]
        SQLite[EricksonLopez.Concurrency.Sqlite]
    end

    subgraph ExternalLibraries [Ecosystem & Driver Dependencies]
        ExtDapper[Dapper]
        ExtResult[EricksonLopez.Result]
        ExtMediator[EricksonLopez.Mediator]
        ExtNpgsql[Npgsql]
        ExtSqlClient[Microsoft.Data.SqlClient]
        ExtMySql[MySqlConnector]
        ExtOracle[Oracle.ManagedDataAccess.Core]
        ExtSqlite[Microsoft.Data.Sqlite]
    end

    Abs --> DapperPkg
    Core --> DapperPkg
    DapperPkg --> ExtDapper

    Abs --> ResultPkg
    Core --> ResultPkg
    ResultPkg --> ExtResult

    Abs --> MediatorPkg
    Core --> MediatorPkg
    MediatorPkg --> ExtMediator

    Abs --> TestingPkg
    Abs --> AspNetCorePkg

    Abs --> PG
    Core --> PG
    PG --> ExtNpgsql

    Abs --> MSSQL
    MSSQL --> ExtSqlClient

    Abs --> MySQL
    MySQL --> ExtMySql

    Abs --> MariaDB
    MariaDB --> ExtMySql

    Abs --> Oracle
    Oracle --> ExtOracle

    Abs --> SQLite
    SQLite --> ExtSqlite
```

---

### 6. CQRS Pipeline with Mediator & `ConcurrencyBehavior`

```mermaid
sequenceDiagram
    autonumber
    actor Client as Command Sender
    participant Med as IMediator
    participant Pipe as ConcurrencyBehavior<TRequest, TResponse>
    participant CC as IConcurrencyController
    participant Handler as IRequestHandler<TRequest, TResponse>

    Client->>Med: Send(UpdateInventoryCommand [ExpectedVersion = 4])
    Med->>Pipe: Handle(command, next, ct)
    
    Pipe->>Pipe: ConcurrencyDiagnostics.StartActivity("mediator.concurrency")
    
    alt Command implements IConcurrencyAwareRequest
        Pipe->>Pipe: Extract ExpectedVersion (4)
        Pipe->>Handler: next()
        Handler-->>Pipe: Result<TResponse>
        
        alt Handler reported concurrency conflict
            Pipe->>Pipe: ConcurrencyDiagnostics.RecordConflict()
            Pipe->>Pipe: SetActivityStatus(Error)
        else Operation Succeeded
            Pipe->>Pipe: ConcurrencyDiagnostics.RecordSuccess()
            Pipe->>Pipe: SetActivityStatus(Ok)
        end
    else Non-concurrency Command
        Pipe->>Handler: next() (Zero-overhead bypass)
        Handler-->>Pipe: Result<TResponse>
    end
    
    Pipe-->>Med: Result<TResponse>
    Med-->>Client: Result<TResponse>
```

---

### 7. In-Memory CAS Execution with Contention & Reentrancy Guards

```mermaid
flowchart TD
    StartCAS([ExecuteCasAsync Entry]) --> AcquireLock[Acquire Striped Entity Semaphore]
    AcquireLock --> CheckTimeout{Acquisition Timeout?}
    CheckTimeout -- Yes --> ThrowTimeout[Throw TimeoutException]
    CheckTimeout -- No --> CheckReentrancy{AsyncLocal Reentrancy Check}
    
    CheckReentrancy -- Reentrant on same ID --> ThrowReentrant[Throw InvalidOperationException]
    CheckReentrancy -- Valid --> EvaluateVersion{ExpectedVersion Matches?}
    
    EvaluateVersion -- Matches --> ExecuteDelegate[Execute mutate delegate with Execution Timeout]
    ExecuteDelegate --> CheckDelegateResult{Mutation Succeeded?}
    
    CheckDelegateResult -- Succeeded --> CheckMutable{Implements IMutableVersionedEntity?}
    CheckMutable -- Yes --> MutateVersion[Advance Version in place: Next]
    CheckMutable -- No --> VerifyImmutableVersion{Delegate advanced Version?}
    VerifyImmutableVersion -- Yes --> ReleaseLockSuccess[Release Entity Lock]
    VerifyImmutableVersion -- No --> ThrowVersionProgression[Throw InvalidOperationException]
    
    MutateVersion --> ReleaseLockSuccess
    ReleaseLockSuccess --> BuildCasSuccess[CasResult.Succeeded]
    BuildCasSuccess --> ReturnSuccess([Return CAS Success])
    
    CheckDelegateResult -- Delegate Threw --> ReleaseLockFail[Release Entity Lock]
    ReleaseLockFail --> PropagateEx[Propagate Domain Exception]
    
    EvaluateVersion -- Mismatch --> ReleaseLockConflict[Release Entity Lock]
    ReleaseLockConflict --> BuildConflictRecord[ConcurrencyConflict.VersionMismatch]
    BuildConflictRecord --> BuildCasConflict[CasResult.Conflicted]
    BuildCasConflict --> ReturnConflict([Return CAS Conflict])
```

---

### 8. Operational Error Handling & Classification Taxonomy

```mermaid
flowchart LR
    subgraph ErrorSources [Error Sources]
        DBEx[Database Exception]
        ZeroRows[0 Rows Affected on UPDATE]
        TokenDiff[If-Match / ETag Precondition Mismatch]
    end

    subgraph ErrorClassifiers [Dialect Classifiers]
        Classifier[IConcurrencyErrorClassifier]
        DBEx --> Classifier
    end

    subgraph Taxonomy [Operational Taxonomy]
        Classifier --> Transient[Transient: Retryable with Backoff & Jitter]
        Classifier --> StaleState[StaleState: Requires State Reload]
        Classifier --> NonRetryable[NonRetryable: Permanent Conflict]
        ZeroRows --> StaleState
        TokenDiff --> StaleState
    end

    subgraph ResolutionLayer [Resolution Layer]
        Transient --> RetryPolicy[External Resilience Policy]
        StaleState --> DomainResolver[IConcurrencyConflictResolver]
        NonRetryable --> FailResult[Result.Failure / HTTP 409]
    end
```
