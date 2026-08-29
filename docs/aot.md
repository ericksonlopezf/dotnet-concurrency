# Native AOT & Trimming Compatibility

**Author:** Erickson Lopez ([ericksonlopezf@gmail.com](mailto:ericksonlopezf@gmail.com))  
**License:** MIT License  

---

## 1. Native AOT & Trimming Mandate

`EricksonLopez.Concurrency` is designed from the ground up to be **100% Native AOT Compatible** on .NET 10.

The following properties are configured in `Directory.Build.props` and apply to all source projects:

```xml
<PropertyGroup>
  <!-- Native AOT trim-safe annotation enforcement -->
  <IsAotCompatible>true</IsAotCompatible>
  <EnableTrimAnalyzer>true</EnableTrimAnalyzer>
  <!-- Zero-tolerance for compiler warnings (includes trim and AOT warnings) -->
  <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
</PropertyGroup>
```

> **Note**: `<EnableSingleFileAnalyzer>` is not configured in `Directory.Build.props`. Single-file publish compatibility is validated indirectly by the Native AOT trim analysis, which is a strict superset of single-file requirements.

---

## 2. Zero Reflection & Value-Type Optimistic Concurrency

- `ConcurrencyVersion`, `ExpectedVersion`, `ActualVersion`, and dialect tokens (`XminConcurrencyToken`, `SqlServerRowVersionToken`, `OracleRowScnToken`) are implemented as zero-allocation `readonly struct` / `readonly record struct` value types.
- No runtime reflection or dynamic code generation is used for version comparison, state verification, or CAS operations.
- Interceptors and pipeline behaviors in `EricksonLopez.Concurrency.Mediator` use strongly typed delegate dispatching and struct-based behaviors.

---

## 3. Native AOT Test Suite (`EricksonLopez.Concurrency.AotSmokeTest`)

The dedicated Native AOT test suite is located in `tests/EricksonLopez.Concurrency.AotSmokeTest`:

```xml
<PropertyGroup>
  <OutputType>Exe</OutputType>
  <TargetFramework>net10.0</TargetFramework>
  <PublishAot>true</PublishAot>
  <EnableTrimAnalyzer>true</EnableTrimAnalyzer>
  <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
</PropertyGroup>
```

### Why a Standalone Executable Project?

1. **Test Runner Reflection Isolation**: Standard test harnesses (xUnit, Moq, AwesomeAssertions) depend on runtime reflection and dynamic IL generation for test discovery, which are fundamentally incompatible with full Native AOT trimming (`PublishAot=true`).
2. **True Compilation & Trimming Verification**: A standalone executable project compiles directly with `PublishAot=true` and exercises the library's real public APIs, CAS controllers, dialect tokens, conflict resolvers, diagnostics, and result mappings directly as native machine code.

### Running the Native AOT Smoke Test

```bash
# Run directly (via dotnet run, no native compilation)
dotnet run --project tests/EricksonLopez.Concurrency.AotSmokeTest/EricksonLopez.Concurrency.AotSmokeTest.csproj

# Publish as a self-contained Linux native binary (as in CI)
dotnet publish tests/EricksonLopez.Concurrency.AotSmokeTest/EricksonLopez.Concurrency.AotSmokeTest.csproj \
  -c Release -r linux-x64 --self-contained -o ./aot-output

./aot-output/EricksonLopez.Concurrency.AotSmokeTest
```

### CI Integration

The smoke test is run automatically as part of the CI pipeline (`aot-smoke-test.yml`) after every successful build and test pass on `main` and `develop` branches. See [docs/ci-cd.md](ci-cd.md) for the full pipeline description.
