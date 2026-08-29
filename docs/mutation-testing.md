# Mutation Testing

Mutation testing for `EricksonLopez.Concurrency` is performed using [Stryker.NET](https://stryker-mutator.io/docs/stryker-net/introduction/) (`dotnet-stryker`). Mutation tests supplement unit test coverage by verifying that the test suite can detect actual behavioral changes (mutations) in the production code.

---

## 1. Philosophy & Quality Gate Architecture

The `EricksonLopez.Concurrency` framework enforces strict mutation testing quality standards using a **deferred quality gate architecture**:

- **Quality Gate Architecture**: Stryker mutation testing is decoupled from git pushes to avoid resource saturation and lengthy CI queues. It operates strictly as an asynchronous Quality Gate.
- **Scheduled Regressions**: Weekly scheduled runs (Sundays at 03:00 UTC) continuously audit the codebase against regressions.
- **Manual Dispatches**: On-demand manual runs support tier profile selections (`Basic`, `Standard`, `Advanced`).
- **Release Gate (Pre-Publish Verification)**: Package publishing (`publish.yml`) verifies that the commit SHA being released has achieved a mutation score $\ge 95\%$ on `main` before allowing NuGet package deployment. If fresh evidence is missing or expired (>7 days), Stryker executes conditionally as a mandatory pre-release gate.

### Mutation Thresholds

Thresholds are centralized in `stryker-config.json` (and package-specific config files):

| Threshold | Value | Meaning & Gate Policy |
|---|---|---|
| **Break** | 95% | **Hard Gate**: Pipeline fails and release is blocked if mutation score $< 95\%$. |
| **Low** | 98% | **Quality Advisory**: Score $\ge 98\%$ yields `LOW`/`HIGH` status. Scores $95\% \le \text{score} < 98\%$ generate `WARNING` but do not break builds. |
| **High** | 100% | **Target Goal**: Represents complete mutant eradication (`HIGH`). |

Status classification:
- `✅ HIGH`: $\ge 100\%$ (or 0 mutants)
- `🟡 LOW`: $\ge 98\%$ and $< 100\%$
- `🟠 WARNING`: $\ge 95\%$ and $< 98\%$
- `❌ FAILED`: $< 95\%$ (Triggers CI failure and blocks release)

---

## 2. Per-Package Stryker Configuration Files

Each source package has a dedicated Stryker configuration file in the repository root:

| Source Package | Config File |
|---|---|
| `EricksonLopez.Concurrency` (Core) | `stryker-core-config.json` |
| `EricksonLopez.Concurrency.Abstractions` | `stryker-abstractions-config.json` |
| `EricksonLopez.Concurrency.AspNetCore` | `stryker-aspnetcore-config.json` |
| `EricksonLopez.Concurrency.Dapper` | `stryker-dapper-config.json` |
| `EricksonLopez.Concurrency.MariaDb` | `stryker-mariadb-config.json` |
| `EricksonLopez.Concurrency.Mediator` | `stryker-mediator-config.json` |
| `EricksonLopez.Concurrency.MySql` | `stryker-mysql-config.json` |
| `EricksonLopez.Concurrency.Oracle` | `stryker-oracle-config.json` |
| `EricksonLopez.Concurrency.PostgreSql` | `stryker-postgresql-config.json` |
| `EricksonLopez.Concurrency.Result` | `stryker-result-config.json` |
| `EricksonLopez.Concurrency.Sqlite` | `stryker-sqlite-config.json` |
| `EricksonLopez.Concurrency.SqlServer` | `stryker-sqlserver-config.json` |
| `EricksonLopez.Concurrency.Testing` | `stryker-testing-config.json` |

`stryker-config.json` serves as the generic fallback configuration template.

---

## 3. Standard Configuration Format

All package-specific config files adhere to the canonical schema:

```json
{
  "stryker-config": {
    "ignore-methods": [
      "ConfigureAwait",
      "Dispose"
    ],
    "test-projects": [
      "EricksonLopez.Concurrency.<Package>.Tests.csproj"
    ],
    "project": "EricksonLopez.Concurrency.<Package>.csproj",
    "reporters": [
      "html",
      "json",
      "cleartext",
      "progress"
    ],
    "mutate": [
      "**/*.cs",
      "!bin/**",
      "!obj/**",
      "!**/*.g.cs",
      "!**/*.AssemblyInfo.cs"
    ],
    "thresholds": {
      "break": 95,
      "high": 100,
      "low": 98
    },
    "coverage-analysis": "all"
  }
}
```

**Key settings**:
- `ignore-methods`: `ConfigureAwait` and `Dispose` are excluded because mutations to these produce equivalent code (no behavioral change in test context).
- `coverage-analysis: all`: Stryker links every mutation to coverage information before running tests, executing only tests that cover the mutated AST nodes.
- `reporters`: Produces HTML (browser report), JSON (machine-parseable), cleartext (CI console), and progress (real-time progress).

---

## 4. Running Mutation Tests Locally

### Prerequisites

```bash
# Install Stryker CLI (one-time)
dotnet tool install -g dotnet-stryker

# Verify installation
dotnet-stryker --version
```

### Run Mutation Tests for a Specific Package

Run from the repository root directory:

```bash
# Core package
dotnet-stryker --config-file stryker-core-config.json

# Abstractions
dotnet-stryker --config-file stryker-abstractions-config.json

# PostgreSQL adapter
dotnet-stryker --config-file stryker-postgresql-config.json

# SQL Server adapter
dotnet-stryker --config-file stryker-sqlserver-config.json

# Dapper adapter
dotnet-stryker --config-file stryker-dapper-config.json

# Result adapter
dotnet-stryker --config-file stryker-result-config.json

# Mediator adapter
dotnet-stryker --config-file stryker-mediator-config.json

# Testing utilities
dotnet-stryker --config-file stryker-testing-config.json

# Database dialect adapters
dotnet-stryker --config-file stryker-mysql-config.json
dotnet-stryker --config-file stryker-mariadb-config.json
dotnet-stryker --config-file stryker-oracle-config.json
dotnet-stryker --config-file stryker-sqlite-config.json
```

### View Mutation Reports

After execution, Stryker generates reports in `./StrykerOutput/ci-<package>/reports/`:
- **HTML Report**: Open `mutation-report.html` in a web browser for interactive source-code drilldown.
- **JSON Report**: `mutation-report.json` for structured score evaluation and tooling ingestion.

---

## 5. CI/CD Orchestration & Tier Profiles

Mutation testing is orchestrated via `.github/workflows/mutation-testing.yml`:

### Tier Profiles (`workflow_dispatch`)

| Tier Profile | Target Packages | Use Case |
|---|---|---|
| **`Basic`** | `core`, `abstractions`, `result` | Fast smoke validation of foundational concurrency primitives and domain contracts (~10-15 min). |
| **`Standard`** | `core`, `abstractions`, `result`, `mediator`, `dapper`, `testing`, `postgresql`, `sqlserver`, `sqlite` | Targeted validation of core engine and standard database adapters (~30-45 min). |
| **`Advanced`** | All 13 ecosystem packages | Full, comprehensive mutation testing suite executed on `main` push and weekly schedule. |

### Release Gate Enforcement (`publish.yml`)

The package publishing pipeline incorporates `scripts/verify-release-mutation-gate.js` prior to executing `dotnet pack` and `dotnet nuget push`:
1. Resolves the commit SHA being published.
2. Verifies that the commit has an official GitHub Commit Status attestation (`quality-gate/stryker-mutation`) with `state = success`.
3. If mutation score $\ge 95\%$, release proceeds immediately without re-running Stryker.
4. If score $< 95\%$ or no mutation testing has run for this commit, release is terminated and blocked.

---

## 6. Interpreting Results

| Mutation Status | Meaning | Action Required |
|---|---|---|
| **Killed** ✅ | The test suite detected the mutation and caused a test failure. | Desired outcome — test assertion verifies mutant behavior. |
| **Survived** ❌ | The mutation was not caught by any test. | Add or improve a unit test to detect this behavioral modification. |
| **No Coverage** ⚠️ | No test executes the mutated code path. | Add targeted unit tests covering this line. |
| **Timeout** ⚠️ | Running tests with this mutation caused an infinite loop or timeout. | Review logic and ensure appropriate cancellation/timeout guards. |
| **Compile Error** | The mutated code could not compile. | Automatically handled by Stryker; no action needed. |
