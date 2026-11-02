# Windows Box — Plan (v0.1, 1 Nov 2026)

Inputs: `../windows-box-landscape/` (tools, matrix, synthesis, licenses, stack), `design.md`, `winutil-inspiration.md`, `winutil-reference.md`, `../windows-box-services/` (catalog, design).

## Vision
The one Windows 11 manager people trust: explains every change, measures the real benefit, undoes anything, and leaves zero footprint. Inspired by WinUtil's breadth and community model, not a copy of it.

## Priorities
1. **Truly great UX**: native Fluent, plain language, preview before apply, history timeline.
2. **No compromise on resources**: no resident service/tray/updater; fast cold start; ~0 idle cost.
3. **Real depth**: services optimization (flagship) and software management.

## Feature set
**Flagship: services.** Per-service catalog with verified per-build defaults, hardware/role gating, dependency checks, profiles (Safe/Balanced/Gamer/Laptop/Office/Privacy/Dev/Minimal), preview diff with estimated RAM saved, journal + .reg/.cmd offline restore, post-reboot health check, drift detection.
**Flagship: software.** Own catalog (multi-source: winget COM API → choco/scoop → Store → direct+SHA256), unified inventory (winget + ARP + Appx), update review with pin/ignore, bundles, silent-install queue, JSONL logs, rollback of what a run installed.
**Also:** privacy & telemetry (policy-first), startup apps (Autoruns-grade coverage of autostart locations), scheduled tasks, Appx debloat (disable over remove), visual/power/network tweaks (laptop-aware), Windows features, repair tools (DISM/SFC/WU reset), updates (deferral, not disable), import of WinUtil JSON / ShutUp10 .cfg.
**Cross-cutting:** item cards (what/gain/breaks/risk/undo/exact command/source), global search, dry run, timeline rollback, signed shareable profiles, CLI `wbox` for scripting.

### Top differentiators
1. Measured pre-state journal + undo for every item (WinUtil assumes defaults).
2. Hardware/role-aware recommendations (printer, BT, battery, Hyper-V/WSL, MDM).
3. Dependency-checked services engine with per-build verified defaults.
4. Preview diff + dry run + quantified impact ("−140 MB idle") with honesty labels on unproven tweaks.
5. Drift detection after Windows Updates without a background agent.
6. Offline recovery kit (works from Safe Mode/WinRE).
7. Unified software inventory + multi-source fallback + install rollback.
8. Profiles as composable, signed, shareable files; imports from WinUtil/ShutUp10.
9. Zero-footprint native app (WinUI 3), signed, no `irm | iex`.
10. Enterprise/MDM detection → advisory read-only mode; policy-based, never fights protected services.

## Architecture / stack
C# .NET 10 LTS + **WinUI 3** (Windows App SDK 1.8), unpackaged self-contained + MSIX + winget; Native AOT for `wbox` CLI and Elevator. Layers: `WindowsBox.Core` (catalog loader, planner, journal, providers: Service/Registry/Task/Appx/Feature/Package) → `wbox` CLI → WinUI shell (MVVM, CommunityToolkit). Short-lived elevated helper per apply batch. Catalogs as schema-validated YAML in repo. CI: VM matrix Home/Pro/Ent × 23H2/24H2/25H2 verifying apply→undo round-trips and harvesting service defaults. Licence: MIT; reuse only MIT/Apache data with notices; GPL (Optimizer, Atlas) and CC-BY-SA (ReviOS) are reading-only. Fallback stack: WPF + WPF-UI. Details: `../windows-box-landscape/stack.md`.

## MVP scope & milestones
- **M0 (2 wks)** Repo, schema, Core skeleton, journal format, VM harness that dumps service defaults per build.
- **M1 (4 wks) Services MVP**: catalog (~80 services), probes, planner + dependency checks, preview, apply/undo, offline restore kit, CLI. UI: Services page + item cards.
- **M2 (4 wks) Software MVP**: winget COM adapter, curated catalog (~150 apps), inventory, install queue, update review, bundles, logs.
- **M3 (3 wks)** Privacy/startup/tasks/debloat with same engine; profiles + onboarding wizard; timeline.
- **M4 (2 wks)** Drift detection, health checks, import WinUtil/ShutUp, signing, winget/MSIX release → **public beta**.
- Later: choco/scoop/Store adapters, impact measurement (ETW), repair tab, localisation.

## Open questions for Ron
1. Stack OK — WinUI 3/C#, or do you prefer Tauri (web UI) or WPF?
2. Target: Windows 11 only, or also Windows 10 22H2 (EOL Oct 2025, ESU)?
3. Licence: MIT ok? Project name/domain/code-signing (SignPath OSS vs Azure Trusted Signing)?
4. Audience default: enthusiasts (more power) or general users (safer defaults)?
5. Should "Disable Windows Update" / Edge-OneDrive removal be offered at all?
6. Any telemetry for the app itself (opt-in crash reports) or strictly none?
7. Install catalog: curated small (quality) vs large (WinUtil-size)?
8. Contribution model: community PRs to catalogs from day one?
