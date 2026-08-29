## Description

Please include a summary of the change and which issue is fixed (if any).
Include relevant motivation and context.

## Affected Packages
Please check all packages that are affected by this PR:
- [ ] `EricksonLopez.Concurrency` (Core)
- [ ] `EricksonLopez.Concurrency.Abstractions`
- [ ] `EricksonLopez.Concurrency.AspNetCore`
- [ ] `EricksonLopez.Concurrency.Dapper`
- [ ] `EricksonLopez.Concurrency.MariaDb`
- [ ] `EricksonLopez.Concurrency.Mediator`
- [ ] `EricksonLopez.Concurrency.MySql`
- [ ] `EricksonLopez.Concurrency.Oracle`
- [ ] `EricksonLopez.Concurrency.PostgreSql`
- [ ] `EricksonLopez.Concurrency.Result`
- [ ] `EricksonLopez.Concurrency.Sqlite`
- [ ] `EricksonLopez.Concurrency.SqlServer`
- [ ] `EricksonLopez.Concurrency.Testing`

## Checklist

Before submitting this PR, please verify the following:
- [ ] I have performed a self-review of my own code.
- [ ] I have updated the `CHANGELOG.md` (if applicable).
- [ ] I have added/updated unit tests or integration tests.
- [ ] Local build passes (`dotnet build EricksonLopez.Concurrency.slnx -c Release`).
- [ ] Local tests pass (`dotnet test EricksonLopez.Concurrency.slnx`).
- [ ] I verified compliance using `./scripts/verify-compliance.ps1`.
- [ ] Stryker mutation testing maintains the **95%** mutation score threshold.
- [ ] Benchmarks confirmed no regressions.
