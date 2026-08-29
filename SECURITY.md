# Security Policy

## 🛡️ Supported Versions

We actively maintain and provide security updates for the current active releases of `EricksonLopez.Concurrency`.

| Version | Supported Target Frameworks | Supported | Security Maintenance |
|---|---|---|---|
| **1.1.x** | `.NET 8.0`, `.NET 9.0`, `.NET 10.0` | ✅ Yes | Current Active Release |
| **1.0.x** | `.NET 8.0`, `.NET 9.0`, `.NET 10.0` | ✅ Yes | Maintenance |
| `< 1.0.0` | Pre-release / Experimental | ❌ No | Deprecated |

---

## 🔒 Reporting a Vulnerability

The security of `EricksonLopez.Concurrency` and its downstream consumers is of paramount importance.

If you discover a security vulnerability or suspect a concurrency flaw (such as race conditions leading to state corruption, data leakage, or injection vectors):

1. **Do NOT open a public GitHub issue**.
2. Email your report directly to **[ericksonlopezf@gmail.com](mailto:ericksonlopezf@gmail.com)**.
3. Include the following details:
   - Package name and exact version.
   - Proof of Concept (PoC) code or reproducible test case.
   - Impact assessment (e.g., race condition in multi-tenant isolation, memory disclosure).
   - Any suggested mitigations.

### Response SLA
- **Initial Response**: Within 24 hours acknowledging receipt.
- **Triage & Reproduction**: Within 72 hours.
- **Fix & Advisory**: A coordinated patch and GitHub Security Advisory (GHSA) will be released prior to public disclosure.

---

## 🧱 Supply Chain Security & Build Verification

- **Strong Name Signing**: All production assemblies are signed with an official strong name key (`EricksonLopez.snk`) to ensure binary authenticity and assembly identity protection.
- **SourceLink & Deterministic Builds**: All packages are built with deterministic compiler flags and SourceLink metadata enabled (`PublishRepositoryUrl=true`, `EmbedUntrackedSources=true`, `SymbolPackageFormat=snupkg`).
- **Zero Third-Party Reflection**: Core abstractions and controllers rely on zero runtime IL generation or reflection, eliminating entire classes of dynamic execution vulnerabilities.
- **Central Package Management (CPM)**: All dependency versions are centrally locked in `Directory.Packages.props` with automated vulnerability scanning via Dependabot.

---

## 🛡️ Security Boundaries & Invariants

1. **Multi-Tenancy Isolation**:
   - When using `OptimisticUpdateBuilder.BuildVersionedUpdate`, the `tenantColumn` and `tenantParam` must ALWAYS be supplied in multi-tenant environments to enforce tenant partition boundaries at the SQL level.
2. **Parameterized SQL Queries**:
   - `ConcurrencyDapperExtensions` strictly uses parameterized queries (`@Id`, `@ExpectedVersion`). Raw string concatenation of user input into SQL templates is strictly prohibited.
3. **Compare-And-Swap Domain Isolation**:
   - `IConcurrencyController.ExecuteCasAsync` verifies version matches before applying the domain mutation delegate. Callers must ensure that the entity mutated inside the delegate does not expose mutable state to unmanaged external threads outside the CAS boundary.
