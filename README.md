# Windows Box

**The Windows 11 manager people trust.** Explain every change, measure the benefit, undo anything, leave **zero** footprint when closed.

Inspired by Chris Titus Tech’s [WinUtil](https://github.com/ChrisTitusTech/winutil) (MIT) — never a copy.

[![CI](https://github.com/modded-soldier-9/windows-box/actions/workflows/ci.yml/badge.svg)](https://github.com/modded-soldier-9/windows-box/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

## Priorities

1. **Truly great UX** — Fluent WinUI, preview/diff before apply, honest risk labels, undo history  
2. **No compromise on resources** — no resident service, tray, or updater; Elevator elevates only for a batch then exits  
3. **Real depth** — services (flagship) and software management, same engine everywhere  

## What’s here (M1 stack)

| Area | Status |
|-|-|
| Shared engine (SCM/registry adapters, preview, journal, verify, drift, restore) | Stacked PRs |
| Undo layer + app-open verifier | Stacked PRs |
| Elevator (UAC batch helper) | Stacked PRs |
| WinUI 3 shell (preview + history) | Stacked PRs |
| Startup module (Autoruns-grade coverage) | Stacked PRs |
| Services module (88 YAML + 290 inventory) | Stacked PRs |
| Opt-in `\WindowsBox\Verify` task | Stacked PRs |
| Network privacy one-shot (`wbox netcheck`) | Stacked PRs |
| Offline signing + WPF fallback gate | Stacked PRs |

## Stack

- C# / **.NET 10 LTS**
- WinUI 3 (Windows App SDK 1.8), Windows 11 only (`≥ 22621`)
- Native AOT for `wbox` + Elevator
- Licence: **MIT**

## Quick start

Full walkthrough: **[docs/quick-start.md](docs/quick-start.md)**.

```bash
dotnet test tests/WindowsBox.Tests/WindowsBox.Tests.csproj
dotnet run --project src/WindowsBox.Cli -- catalog validate --catalog catalog/services
dotnet run --project src/WindowsBox.Cli -- version
```

On Windows (after a real preview — never against a machine you care about without a journaled dry-run):

```bash
wbox preview --profile balanced
wbox dry-run --profile privacy
wbox verify --quiet
wbox netcheck
```

## Docs

| Doc | Why |
|-|-|
| [docs/quick-start.md](docs/quick-start.md) | First-run guide (offline-first) |
| [MASTER-PLAN.md](docs/MASTER-PLAN.md) | Source of truth |
| [ARCHITECTURE.md](ARCHITECTURE.md) | Layers & pipeline |
| [docs/wiki/](docs/wiki/) | Wiki source (architecture, engine, modules, catalog) |
| [docs/signing.md](docs/signing.md) | What Ron must supply for Authenticode |
| [docs/network-safeguards.md](docs/network-safeguards.md) | Network features: opt-in, off by default |
| [docs/trust-guide.md](docs/trust-guide.md) | Inspect `install.ps1` before `irm \| iex` |
| [CONTRIBUTING.md](CONTRIBUTING.md) · [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md) · [SECURITY.md](SECURITY.md) | Community |
| [docs/research/](docs/research/) | Fact-checked landscape (kept in-repo) |

## Non-negotiables

- No tweaks on install  
- No phone-home  
- Prefer **Manual** over **Disabled**  
- Journal **real** prior values for every apply  
- Tests use **fakes** — never mutate CI runners or developer hosts  

## Contributing

PRs welcome — especially catalog YAML with sources. Read [CONTRIBUTING.md](CONTRIBUTING.md) first.
