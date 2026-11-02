# Windows Box

An open-source **Windows 11** manager that explains every change, measures the benefit, can undo anything, and leaves **no background footprint**.

Inspired by (not copied from) Chris Titus Tech’s WinUtil (MIT) and the wider optimizer landscape.

## Status

**M0 — foundation** (this repo milestone): solution layout, YAML service catalog (~80 entries), Core engine skeleton (detect → preview → snapshot → apply → health → rollback), `wbox` CLI, short-lived Elevator, WinUI 3 shell with placeholder pages, unit tests with fakes, CI.

## Priorities

1. Truly great UX (Fluent, preview, history)
2. Minimal resources (no resident service / tray / updater)
3. Real depth in **services** and **software** management

## Stack

- C# / .NET 10 LTS
- WinUI 3 (Windows App SDK 1.8), Windows 11 only
- Native AOT for `wbox` CLI and Elevator
- Licence: MIT

## Quick start

```bash
dotnet test tests/WindowsBox.Tests/WindowsBox.Tests.csproj
dotnet run --project src/WindowsBox.Cli -- catalog validate --catalog catalog/services
```

WinUI app (`src/WindowsBox.App`) builds on Windows 11 with the Windows App SDK.

## Docs

- [Plan](docs/PLAN.md)
- [Design](docs/design.md)
- [Architecture](ARCHITECTURE.md)
- [Contributing](CONTRIBUTING.md)
- [VM test plan](docs/testing-vms.md)
- [Services catalog (human)](docs/catalogs/services.md) → machine YAML in [`catalog/services/`](catalog/services/)

## Non-negotiables

- No tweaks on install
- No phone-home
- Prefer **Manual** over **Disabled**
- Journal **real** prior values for every apply
