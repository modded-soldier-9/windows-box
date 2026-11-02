# Architecture

Windows Box is a **zero-footprint** Windows 11 manager: the UI and CLI are thin fronts over a shared engine. Nothing stays resident after the window closes.

## Layers

```
┌─────────────────────────────────────────────────────────┐
│  WindowsBox.App (WinUI 3)     WindowsBox.Cli (wbox)     │
│  Fluent NavigationView shell  scripting / CI / headless │
└──────────────────────────┬──────────────────────────────┘
                           │
┌──────────────────────────▼──────────────────────────────┐
│                  WindowsBox.Core                        │
│  Catalog loader/validator · Preview planner             │
│  Apply pipeline · Journal store · Health checks         │
│  Abstractions: IServiceController, IRegistry, IProbe    │
└──────────────────────────┬──────────────────────────────┘
                           │ per apply batch (UAC)
┌──────────────────────────▼──────────────────────────────┐
│              WindowsBox.Elevator                        │
│     Short-lived elevated helper — exits when done       │
└─────────────────────────────────────────────────────────┘
```

## Apply pipeline

1. **Detect** — probe edition/build, hardware, roles (MDM, domain, Xbox, printers, BT, battery, features).
2. **Preview** — resolve profile → candidates → gates → dependency check → diff (current → target).
3. **Snapshot** — journal real prior start type, delayed flag, triggers, running state, plus `reg export` of each service key. Optional restore point (with consent) comes later.
4. **Apply** — Elevator calls SCM (`ChangeServiceConfig` / `ChangeServiceConfig2`). Running services are not stopped unless asked (“takes effect after reboot”).
5. **Health check** — re-read critical services; richer post-reboot checks are opt-in one-shots (no resident agent).
6. **Rollback** — replay journal priors; offline `.reg`/`.cmd` kit under `%ProgramData%\WindowsBox` (M1).

## Catalogs

- Declarative YAML in-repo, community-PR friendly.
- JSON Schema: `catalog/schemas/service.schema.json`.
- Loader + validator in Core; `wbox catalog validate` in CI.
- Principles encoded in data: `touch_policy`, risk 0–4, Manual-before-Disabled, sources required.

## Runtime / packaging

- Target framework: **.NET 10 LTS** (`net10.0`; App uses `net10.0-windows10.0.22621.0`).
- WinUI shell: Windows App SDK **1.8**.
- `wbox` CLI and Elevator publish with **Native AOT** (fast cold start, no .NET runtime install for those tools).

## Elevation model

- App/CLI run as the user.
- Elevator launches only for an apply/rollback batch, then exits.
- **No** Windows service, tray icon, or always-on updater.

## Journals

JSON under `%ProgramData%\WindowsBox\journals` (or a test temp dir). Each entry stores **measured** priors so undo does not assume Microsoft defaults.

## Testing strategy

| Layer | Where | How |
|-------|-------|-----|
| Catalog schema | Linux + Windows CI | `wbox catalog validate` + loader unit tests |
| Pipeline | Linux + Windows CI | Fakes for SCM/registry/probe |
| Round-trip | Hyper-V / Sandbox VMs | See `docs/testing-vms.md` |
| UI | Windows 11 | WinUI shell smoke (manual in M0) |

## Non-goals (by design)

- Background optimization “agents”
- Registry cleaners / “health scores”
- Taking ownership of protected services (WaaSMedic, Defender, DoSvc, …)
- Telemetry from Windows Box itself
- Tweaks that apply on install
