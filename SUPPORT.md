# Support Policy

Welcome to the **`EricksonLopez.Concurrency`** support guide. This document explains where and how to get help when using or integrating the framework.

---

## 🧭 Getting Help

### 1. Documentation & Guides
Before opening an issue, we recommend consulting our comprehensive documentation suite:

- **[System Overview](docs/overview.md)**: Architectural foundations, DDD alignment, and zero-allocation philosophy.
- **[Showcase Guide](docs/showcase-guide.md)**: Walkthrough of the 11-level executable showcase project in `samples/EricksonLopez.Concurrency.Showcase`.
- **[Cookbook & Recipes](docs/cookbook.md)**: Real-world recipes for REST ETags, Dapper zero-roundtrip updates, CAS state transitions, domain conflict merges, and database error mapping.
- **[Public API Reference](docs/api-reference.md)**: Microsoft Learn-style reference of all types, structs, interfaces, and extension methods.
- **[Architecture & Flow](docs/architecture-flow.md)**: Mermaid sequence diagrams, state machines, and resilience demarcation.
- **[ADR Decisions](docs/adr-decisions.md)**: Formal records of architectural choices (ADR-001 through ADR-012).

### 2. GitHub Discussions
For general questions, design advice, and architectural patterns:
- Visit **[GitHub Discussions](https://github.com/ericksonlopezf/dotnet-concurrency/discussions)**.
- Share code snippets, use cases, or database dialect questions.

### 3. GitHub Issues (Bug Reports & Feature Requests)
If you encounter a bug or have a concrete feature proposal:
- Search existing issues on **[GitHub Issues](https://github.com/ericksonlopezf/dotnet-concurrency/issues)** to avoid duplicates.
- Submit a detailed report using the official issue templates with:
  - Exact package version (`EricksonLopez.Concurrency.*`).
  - Target Framework (`net8.0`, `net9.0`, or `net10.0`).
  - Database engine and provider version (e.g., Npgsql 10.0.3, Microsoft.Data.SqlClient 5.2.2, MySqlConnector 2.4.0, Oracle.ManagedDataAccess.Core 23.7.0, Microsoft.Data.Sqlite 10.0.3).
  - Minimal reproducible example or failing test case.

### 4. Direct Maintainer & Commercial Support
For technical inquiries, ecosystem integration assistance, or direct maintainer contact:
- Email the maintainer at **[ericksonlopezf@gmail.com](mailto:ericksonlopezf@gmail.com)**.

---

## 🔒 Security Vulnerabilities

Please do NOT report security issues via public GitHub issues. Follow the procedure described in [SECURITY.md](SECURITY.md) and contact **[ericksonlopezf@gmail.com](mailto:ericksonlopezf@gmail.com)**.
