# Contributing to EricksonLopez.Concurrency

Thank you for your interest in contributing to **`EricksonLopez.Concurrency`**! This document outlines our development process, design standards, architectural invariants, and quality gates to ensure consistency across the ecosystem.

---

## 📋 Prerequisites

- **.NET SDK**: .NET 10.0 SDK is required as the primary build SDK; .NET 8.0 and 9.0 runtimes are also needed for multi-TFM test execution.  
  > **Note**: The repository does not include a `global.json` — ensure the correct SDK version is installed manually. The CI pipeline uses `8.0.x`, `9.0.x`, and `10.0.x` as declared in `.github/workflows/dotnet-build-test.yml`.
- **C# language**: Latest C# version (configured via `<LangVersion>latest</LangVersion>` in `Directory.Build.props`).
- An IDE with Roslyn analyzer support (Visual Studio 2025+, JetBrains Rider 2025+, or VS Code with C# Dev Kit).
- Optional: Local database instances (PostgreSQL, SQL Server, MySQL, MariaDB, Oracle, SQLite) or Docker if running live provider integration tests.
- **PowerShell 7+** (`pwsh`) for running the compliance script.
- **Node.js** (any LTS version) for the Stryker result recording script (`scripts/record-stryker-result.js`).

---

## 🛠️ Development Workflow & Build Commands

### 1. Restore & Build
The solution uses modern Central Package Management (`Directory.Packages.props`) and enforces `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`.

```bash
# Clone the repository
git clone https://github.com/ericksonlopezf/dotnet-concurrency.git
cd dotnet-concurrency

# Restore dependencies
dotnet restore EricksonLopez.Concurrency.slnx

# Compile solution in Debug mode
dotnet build EricksonLopez.Concurrency.slnx

# Compile in Release mode
dotnet build EricksonLopez.Concurrency.slnx -c Release
```

### 2. Run Tests
The repository enforces a strict 1:1 symmetry between production projects and unit test projects:
- **13 unit test suites** — one per source package in `src/`
- **3 cross-cutting suites** — `ArchitectureTests`, `IntegrationTests`, `AotSmokeTest`
- **16 test projects total**

```bash
# Run all unit, integration, and architecture tests across all TFMs
dotnet test EricksonLopez.Concurrency.slnx

# Run specific project tests
dotnet test tests/EricksonLopez.Concurrency.ArchitectureTests/EricksonLopez.Concurrency.ArchitectureTests.csproj
dotnet test tests/EricksonLopez.Concurrency.IntegrationTests/EricksonLopez.Concurrency.IntegrationTests.csproj
```

### 3. Verify Repository Compliance & Architecture Governance
Automated script validating 7 architectural invariants (kebab-case docs, zero Obsolete in src, canonical MIT headers, One Type Per File, GitHub identity links, security email):

```powershell
pwsh -File scripts/verify-compliance.ps1
```

### 4. Run Executable Showcase
The official reference showcase is executable and provides both automated verification and interactive exploration:

```bash
# Run all 11 showcase levels in batch mode
dotnet run --project samples/EricksonLopez.Concurrency.Showcase -- --run-all

# Run interactive CLI learning menu
dotnet run --project samples/EricksonLopez.Concurrency.Showcase -- --menu
```

### 5. Run Benchmarks
Performance benchmarks measure sub-nanosecond version comparisons and 0-byte allocations using BenchmarkDotNet:

```bash
dotnet run --project benchmarks/EricksonLopez.Concurrency.Benchmarks -c Release
```

### 6. Run Mutation Testing (Stryker.NET)
Each package has a dedicated per-package Stryker configuration file with a **95% break threshold** and **100% high threshold**:

```bash
# Install Stryker CLI (one-time)
dotnet tool install -g dotnet-stryker

# Run mutation testing for a specific package
dotnet-stryker --config-file stryker-core-config.json
dotnet-stryker --config-file stryker-abstractions-config.json
dotnet-stryker --config-file stryker-postgresql-config.json
# etc. — see stryker-*.json files in the repository root
```

See [docs/mutation-testing.md](docs/mutation-testing.md) for full configuration details and threshold enforcement.

### 7. Run Native AOT Smoke Test
```bash
dotnet publish tests/EricksonLopez.Concurrency.AotSmokeTest/EricksonLopez.Concurrency.AotSmokeTest.csproj -c Release -r linux-x64 --self-contained -o ./aot-output
./aot-output/EricksonLopez.Concurrency.AotSmokeTest
```

---

## 📐 Architectural Principles & Invariants

All contributions must strictly observe the following architectural rules:

1. **Zero-Allocation Structs (ADR-003)**:
   - `ConcurrencyVersion`, `ConcurrencyVersion<T>`, `ExpectedVersion`, `ActualVersion`, `ConcurrencyToken`, `XminConcurrencyToken`, `SqlServerRowVersionToken`, and `OracleRowScnToken` MUST remain `readonly record struct` value types.
   - Do NOT introduce heap allocations, unnecessary boxing, or LINQ in hot paths (`IConcurrencyChecker`, `IConcurrencyController.VerifyVersion`).
2. **Native AOT & Trimming Compatibility (ADR-004)**:
   - All code in `src/` must compile with `<IsAotCompatible>true</IsAotCompatible>` and `<EnableTrimAnalyzer>true</EnableTrimAnalyzer>`.
   - Avoid runtime reflection (`Type.GetType()`, runtime dynamic IL generation) and unconstrained generic reflection.
3. **Immutability & Thread-Safety (ADR-007)**:
   - Entities and domain models encapsulate state transitions; concurrency records (`ConcurrencyConflict`, `ConflictResolution<T>`) are immutable records.
   - Core checkers (`OptimisticConcurrencyChecker.Instance`) are stateless and safe for multi-threaded concurrent invocation.
4. **Clean Architecture Boundaries (ADR-001 & ADR-008)**:
   - `EricksonLopez.Concurrency` is strictly responsible for detecting, classifying, and reporting conflicts.
   - Retry loops, exponential backoff, and jitter belong in `EricksonLopez.Resilience` or the application layer. Never add circular dependencies to resilience libraries.
   - Distributed locks, sagas, and ORM coupling are permanently out of core scope.
5. **One Type Per File**:
   - Every top-level type (class, struct, interface, enum, record) in `src/` must reside in its own dedicated file matching the type name.
6. **No Invented APIs**:
   - Documentation, tests, and samples must reflect 100% real, verifiable APIs.

---

## 🌿 Branching & Commit Conventions

- **Branch Naming**:
  - `feature/<short-description>`: New capabilities or extensions.
  - `fix/<short-description>`: Bug fixes or error classifier updates.
  - `docs/<short-description>`: Documentation improvements.
  - `refactor/<short-description>`: Performance or clean code refactorings.
- **Commit Messages**: Follow [Conventional Commits](https://www.conventionalcommits.org/):
  - `feat(core): add delegate conflict resolver support`
  - `fix(postgresql): update 55P03 lock unavailable classification`
  - `docs(cookbook): add multi-tenancy dapper update recipe`
  - `perf(checker): optimize version comparison span formatting`

---

## 🔍 Pull Request Checklist

Before submitting a Pull Request, verify:
- [ ] Code compiles cleanly with 0 warnings (`TreatWarningsAsErrors=true`).
- [ ] All 16 test suites pass (`dotnet test EricksonLopez.Concurrency.slnx`).
- [ ] Architecture tests pass (`EricksonLopez.Concurrency.ArchitectureTests`).
- [ ] Compliance auditor passes (`pwsh -File scripts/verify-compliance.ps1`).
- [ ] Showcase runs successfully (`dotnet run --project samples/EricksonLopez.Concurrency.Showcase -- --run-all`).
- [ ] XML documentation is complete for any new public types and methods (`CS1591` compliant).
- [ ] Documentation in `docs/` is updated if behavior or contracts are modified.
- [ ] If `src/` was modified: mutation testing passes for the affected package(s) (break threshold ≥ 95%).
- [ ] If `src/` or `benchmarks/` was modified: benchmark regression gate has been evaluated (see [docs/ci-cd.md](docs/ci-cd.md)).

---

## 📜 Code of Conduct

Please note that this project is governed by our [Code of Conduct](CODE_OF_CONDUCT.md). By participating, you are expected to uphold this code.
