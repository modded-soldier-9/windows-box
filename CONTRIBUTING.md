# Contributing to Windows Box

Thanks for helping build a Windows 11 manager people can trust.

## Principles (non-negotiable)

1. **Great UX** — plain language, preview before apply, honest risk labels.
2. **Zero footprint** — no background service, tray app, scheduled updater, or install-time tweaks.
3. **No phone-home** — the app never phones home. No telemetry unless a future opt-in crash reporter is explicitly approved.
4. **Undo everything** — every apply journals **real prior values** (not assumed defaults).
5. **Manual before Disabled** — prefer Manual/trigger-start; Disabled only for vetted risk ≤ 2 items.
6. **Never fight Windows** — protected services get policy alternatives, not ownership hacks.

## Licence

MIT. Reuse only MIT/Apache (or similarly permissive) data with notices. GPL (Optimizer, Atlas) and CC-BY-SA (ReviOS) are **reading-only** — do not copy their code or configs into this repo.

## Stack

- C# / .NET 10 LTS
- WinUI 3 (Windows App SDK 1.8), Windows 11 only (`10.0.22621+`)
- Native AOT for `wbox` CLI and Elevator
- Catalogs: schema-validated YAML under `catalog/`

## Getting started

```bash
dotnet restore src/WindowsBox.Core/WindowsBox.Core.csproj
dotnet test tests/WindowsBox.Tests/WindowsBox.Tests.csproj
dotnet run --project src/WindowsBox.Cli -- catalog validate --catalog catalog/services
```

The WinUI shell (`WindowsBox.App`) builds on Windows 11 with the Windows App SDK installed. Core, CLI, Elevator, and tests are portable and run on Linux CI via fakes.

## Catalog PRs (welcome from day one)

1. Add or edit `catalog/services/<name>.yaml`.
2. Follow `catalog/schemas/service.schema.json` (wire it in your editor).
3. Cite sources (Microsoft docs preferred).
4. Prefer `Manual` over `Disabled`.
5. Set `touch_policy: never` for boot/security/update services.
6. Run `wbox catalog validate`.
7. Note any hardware/role gates and what breaks.

Do **not** add tweaks that run on install or that contact the network without an explicit user action.

## Code PRs

- Keep engine logic in `WindowsBox.Core`; UI stays a thin shell.
- Abstract Windows APIs (`IServiceController`, `IRegistry`, `ISystemProbe`) so unit tests use fakes on Linux.
- Cover detect → preview → snapshot → apply → health → rollback with tests.
- No `irm | iex`, no resident helpers, no silent elevation.

## Project layout

| Path | Role |
|------|------|
| `src/WindowsBox.Core` | Catalog loader, planner, journal, pipeline |
| `src/WindowsBox.Cli` | `wbox` CLI |
| `src/WindowsBox.Elevator` | Short-lived elevated helper (exits after batch) |
| `src/WindowsBox.App` | WinUI 3 shell (Fluent navigation) |
| `tests/WindowsBox.Tests` | Unit tests with fakes |
| `catalog/services` | Service YAML (~80 entries) |
| `docs/` | Plan, design, VM test plan |

## Code of conduct expectations

Be precise, cite evidence for risky changes, and default to the safer recommendation when unsure.
