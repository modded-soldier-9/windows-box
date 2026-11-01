# Windows Box: Master Plan

Status: v1.0, 1 Nov 2026. This is the single source of truth. It merges:
`docs/research/` (fact-checked landscape, licences, stack), `docs/PLAN.md`, `docs/design.md`,
`docs/winutil-reference.md`, `docs/winutil-inspiration.md`, `docs/services-design.md`,
`docs/catalogs/services.md`, and what PR #1 (`cursor/m0-foundation-a433`, open, not merged) actually implements.
Where this file disagrees with an older doc, this file wins (see "Decisions and resolved contradictions").

## 1. Vision

The Windows 11 manager people trust. It explains every change in plain language, shows exactly what will happen before it happens, measures the real benefit, undoes anything, and leaves nothing running afterwards. Inspired by Chris Titus Tech's WinUtil (MIT) and the wider optimizer landscape, never a copy of them.

## 2. Ron's three priorities (in order)

1. **Truly great UX.** Native Fluent UI, plain language, one card per item, preview before apply, history you can roll back, keyboard first, accessible.
2. **No compromise on resources.** Zero resident processes: no Windows service, tray icon, scheduler or updater of our own. Cost when closed is zero. Fast cold start. Our own app sends no telemetry.
3. **Real depth.** Services optimization is the flagship, software management is second flagship. Every other area gets the same engine and rigour.

Tie-break rule: when priorities conflict, safety and reversibility beat depth, and depth beats breadth.

## 3. Landscape summary (verified)

Full detail with sources: `docs/research/tools.md`, `matrix.md`, `synthesis.md`, `licenses.md`.

- **What the best tools do well:** per-item risk ratings (O&O ShutUp10++), full transparency of the exact script and its revert (privacy.sexy), symmetric undo (Sophia Script), zero footprint (ShutUp10++, Autoruns, WinUtil), depth benchmarks (Autoruns for startup, BleachBit for cleaning, BCUninstaller for uninstall, UniGetUI for packages, NTLite for images).
- **Where they fall short:** walls of checkboxes with no impact explanation; WinUtil assumes default values instead of measuring them; nobody detects when Windows Update silently reverts tweaks; suites carry resident agents and bundleware (CCleaner: 2017 supply chain breach in 32-bit 5.33.6162, July 2020 Defender PUA flag for bundling); distros (Atlas, ReviOS, Tiny11) trade security and serviceability; shell mods inject DLLs and get AV-flagged (ExplorerPatcher, repeated Defender detections in 2024).
- **Licences:** MIT (reusable with notice): WinUtil, Win11Debloat, Sophia Script, BloatyNosy, Privatezilla, UniGetUI, winget, Flyoobe. Apache-2.0: BCUninstaller. Read only: Optimizer (GPL-3.0, archived), Atlas (GPL-3.0), ReviOS playbook (CC-BY-SA-4.0), privacy.sexy (AGPL-3.0), tiny11builder (no licence), all proprietary freeware.

## 4. Feature areas and depth targets

Every area uses the same item model, the same safety pipeline and the same card UI.

| Area | Depth target (definition of "done well") | Milestone |
|-|-|-|
| **Services (flagship)** | ~90 catalogued services with verified per-build, per-edition defaults; hardware and role gating; dependency checks both directions; 8 profiles; per-user template handling; protected services routed to policy alternatives; drift detection; offline restore kit | M1 |
| **Software (flagship)** | Own multi-source catalog (winget COM API first, then choco, scoop, Store, direct download with SHA256); unified inventory (winget + ARP registry + Appx); install queue (parallel download, serial install, continue on error); update review with pin and ignore; bundles; JSONL logs; rollback of what a run installed | M2 (winget), later for other sources |
| Privacy and AI | Policy-first switches for telemetry, Copilot, Recall, ads, Bing and Edge, Office; ShutUp10++-level coverage | M3 |
| Startup | Autoruns-grade coverage of autostart locations, signature checks, "hide Microsoft" filter, non-destructive disable | M3 |
| Scheduled tasks | Full enumeration, catalogued telemetry tasks, disable not delete | M3 |
| Appx debloat | Disable or deprovision with a recorded reinstall path; never remove Store, WebView2 or servicing stack | M3 |
| Visual, power, network | Laptop-aware gating (battery detected changes defaults), Ultimate Performance only on desktops, DNS presets | M3 |
| Windows features | WSL, Hyper-V, Sandbox, .NET 3.5 with edition and virtualization prerequisites checked | Later |
| Updates | Default or security-only deferral via policy; "disable updates" is not offered | M3 |
| Repair | SFC, DISM, Windows Update reset, network reset, winget reinstall, search index rebuild | Later |
| Cleaner | BleachBit-style with preview and component store cleanup; no registry cleaning | Later |
| Import | WinUtil JSON and ShutUp10++ .cfg mapped onto our items | M4 |
| Image builder | Serviceable debloated ISO with dependency guard | Out of scope for v1 |

Cross-cutting features: item cards (what it does, what you gain, what breaks, risk, reversibility, exact change, source links), global search, dry run, history timeline, composable signed profiles, `wbox` CLI, enterprise and MDM advisory mode, honesty labels on unproven tweaks.

### Top differentiators
1. Measured prior state journal, so undo never assumes Microsoft defaults.
2. Hardware and role aware recommendations.
3. Dependency-checked services engine with CI-verified defaults per build.
4. Preview diff and dry run with quantified impact and honesty labels.
5. Drift detection on app open, no background agent.
6. Offline recovery kit usable from Safe Mode or WinRE.
7. Unified software inventory with multi-source fallback and install rollback.
8. Composable, shareable profiles plus WinUtil and ShutUp10++ import.
9. Signed native app with zero idle footprint, no `irm | iex`.
10. MDM and domain detection switches to read-only advisory mode.

## 5. Services design (flagship)

Principles: Manual before Disabled; measure, don't assume; detect before recommending; never fight Windows (no ownership hacks on protected services); explain everything. Catalog content: `catalog/services/*.yaml` (PR #1, 88 entries), human-readable table in `docs/catalogs/services.md`.

**Risk scale (0 to 4, matches PR #1 schema).** 0 never touch, 1 safe to set Manual, 2 safe to Disable if feature unused, 3 situational and breaks a visible feature, 4 breaks updates, Store, login or security.
**touch_policy.** `allow` (engine may change it), `leave` (shown, recommended untouched), `never` (boot or security critical, read only), `policy_only` (protected service; offer the documented policy alternative instead).
**Profiles (8).** safe, balanced, gamer, laptop, office, privacy, developer, minimal. Profiles compose; on conflict the most conservative start type wins.
**Per-user services** (`_xxxxx` suffix) are changed on the template key and flagged `per_user: true`, with a warning naming the feature that breaks.

## 6. Safety pipeline

Implemented in PR #1 as `ApplyPipeline` in `WindowsBox.Core` with fakes for tests; real Windows adapters land in M1.

1. **Detect.** `ISystemProbe`: edition, build, laptop, battery, Bluetooth, printer, biometric, WWAN, webcam, domain joined, MDM enrolled, Insider, Xbox or Game Pass, Xbox controller, enabled features.
2. **Preview.** `PreviewPlanner`: profile to candidates, apply gates (`not`, `hardware_any`, `feature_any`), honour `touch_policy`, dependency check, diff of current to target. Items with risk 3 or more, or one-way items, need explicit confirmation.
3. **Snapshot.** `JournalStore` writes an `ApplyJournal` (id, time, profile, build, edition) with one `JournalEntry` per item: prior start type, prior delayed autostart, prior triggers present, prior running state, target, `reg export` text, status, error. A system restore point is optional and requested with consent (handles the 24 hour throttle).
4. **Apply.** `WindowsBox.Elevator`, a short-lived UAC-elevated process per batch, calls `ChangeServiceConfig` and `ChangeServiceConfig2`. Running services are not stopped unless the user asks.
5. **Health check.** `BasicHealthChecker` re-reads critical services immediately. Richer post-reboot checks (network, audio, Start and Search, Windows Update scan, Store, printers) are an opt-in one-shot, never a resident agent.
6. **Rollback.** `RollbackAsync` replays journal priors per item, per batch or all. Offline kit: `.reg` and `.cmd` under `%ProgramData%\WindowsBox\backups`, runnable from Safe Mode or WinRE (M1).
7. **Drift.** On app open, compare the latest journal targets to live state and offer re-apply or accept.

## 7. Catalog schema (matches PR #1)

Source of truth: `catalog/schemas/service.schema.json` (JSON Schema 2020-12, `schema_version: 1`, `additionalProperties: false`). Validated by `ServiceCatalogValidator` and `wbox catalog validate` in CI.

| Field | Type | Required | Notes |
|-|-|-|-|
| schema_version | int, const 1 | yes | bump with migration |
| id | string `^svc\.[a-z0-9][a-z0-9_.-]*$` | yes | e.g. `svc.diagtrack` |
| area | const `services` | yes | other areas get sibling schemas |
| name, display | string | yes | SCM name; template name for per-user |
| category | enum: telemetry, privacy, gaming, hardware, performance, network, updates, security, virtualization | yes | |
| description | string | yes | plain language |
| defaults | {pro (required), home, enterprise}: startType | yes | verified by VM harness |
| builds | string `^>=\d+$` | yes | e.g. `>=22621` |
| risk | int 0 to 4 | yes | see section 5 |
| touch_policy | allow, leave, never, policy_only | yes | |
| recommend | map profile to startType | yes | Manual preferred |
| sources | uri[] (min 1) | yes | |
| protected, per_user, optional | bool | no | default false |
| depends_on, dependents | string[] | no | checked live too |
| breaks | string[] | no | shown on the card |
| gates | {not, hardware_any, feature_any} | no | probe role names |
| profiles | profile[] | no | which profiles benefit |
| alternatives | [{policy, value?, description}] | no | for policy_only items |
| notes | string | no | |
| honesty_label | unproven, measured, community | no | |

startType enum: `Automatic`, `AutomaticDelayed`, `Manual`, `ManualTrigger`, `Disabled`.

Example (from PR #1):
```yaml
schema_version: 1
id: svc.diagtrack
area: services
name: DiagTrack
display: Connected User Experiences and Telemetry
category: telemetry
description: Sends diagnostic and usage data to Microsoft.
defaults: {pro: Automatic, home: Automatic, enterprise: Automatic}
builds: ">=22621"
risk: 2
touch_policy: allow
recommend: {privacy: Disabled, gamer: Disabled, minimal: Disabled, balanced: Manual}
depends_on: [RpcSs]
breaks: [Optional diagnostic data upload, Some Feedback Hub features]
gates: {not: [mdm_enrolled]}
profiles: [privacy, gamer, minimal]
sources: ["https://learn.microsoft.com/windows/privacy/manage-connections-from-windows-operating-system-components-to-microsoft-services"]
```

**Planned schema additions (schema_version 2, M1/M3):** `gain` (text plus optional measured idle RAM), `reversibility` (auto, script, one_way), `confirm` flag; sibling schemas for registry, task, appx, feature and package items sharing `id`, `risk`, `sources`, `honesty_label`; package catalog schema (sources ranked, silent args, detection rule, licence, FOSS flag).

## 8. Architecture and stack (decided)

- **Language and runtime:** C# on .NET 9 (`global.json` pins SDK 9.0.100, roll forward latest feature).
- **UI:** WinUI 3 (Windows App SDK), Fluent NavigationView shell, MVVM with CommunityToolkit.
- **Target:** Windows 11 only (build 22621 and later).
- **Licence:** MIT. Third-party MIT or Apache data ships with notices in THIRD_PARTY_NOTICES. GPL, AGPL, CC-BY-SA and unlicensed projects are reading material only.
- **Projects (PR #1):** `WindowsBox.Core` (catalog loader and validator, preview planner, apply pipeline, journal, health checks, abstractions `IServiceController`, `IRegistry`, `ISystemProbe`), `WindowsBox.Cli` (`wbox catalog validate|list`, `preview`, `apply`, `rollback`, `journals`), `WindowsBox.App` (WinUI shell, placeholder pages), `WindowsBox.Elevator` (short-lived elevated helper), `tests/WindowsBox.Tests` (catalog, pipeline, journal with fakes).
- **Data paths:** journals in `%ProgramData%\WindowsBox\journals`, backups in `%ProgramData%\WindowsBox\backups`.
- **Distribution:** signed portable ZIP, MSIX and winget manifest. Signing via SignPath (free for OSS) or Azure Trusted Signing (to be chosen).
- **Testing:** unit tests on Linux and Windows CI with fakes; VM round-trip matrix (Home, Pro, Enterprise on 23H2, 24H2, 25H2) per `docs/testing-vms.md`, also harvesting real default start types.
- **Fallback stack if WinUI 3 blocks us:** WPF with WPF-UI. Tauri and Electron rejected (resources).

## 9. Milestones

| Milestone | Scope | Exit criteria |
|-|-|-|
| **M0 Foundation** (PR #1, in review) | Solution layout, service schema v1, 88 service YAMLs, loader and validator, preview planner, apply pipeline, journal, health check, Elevator stub, CLI, WinUI shell, CI | CI green, PR merged |
| **M1 Services MVP** (4 wks) | Real Windows adapters (SCM, registry, probe), live preview, Elevator apply, rollback, offline restore kit, Services page with item cards, VM harness verifying defaults and apply then undo round-trips | Apply then rollback returns identical state on all VM targets |
| **M2 Software MVP** (4 wks) | winget COM adapter, curated catalog (~150 apps), unified inventory, install queue, update review with pin and ignore, bundles, JSONL logs, install rollback | Bundle install and rollback on clean VM |
| **M3 Breadth on one engine** (3 wks) | Privacy and AI, startup, tasks, appx, visual, power, network, update deferral; schema v2; onboarding wizard; history timeline | Every item has undo or explicit one-way flag |
| **M4 Public beta** (2 wks) | Drift detection, post-reboot health one-shot, WinUtil and ShutUp10++ import, signing, MSIX, winget publish | Signed release, no AV detections on VirusTotal major engines |
| Later | choco, scoop and Store adapters; ETW impact measurement; repair tab; cleaner; Windows features; localisation | |

## 10. Decisions and resolved contradictions

| # | Topic | Conflict | Decision |
|-|-|-|-|
| 1 | Target OS | `design.md` schema said `os: ">=10.0.19045"` (Windows 10); `winutil-inspiration.md` mentioned Win10/11 build ranges | **Windows 11 only**, `builds: ">=22621"` as in PR #1 |
| 2 | Stack | `research/stack.md` listed Tauri and WPF as alternatives; synthesis suggested "Rust/C# AOT" | **C# .NET 9 + WinUI 3**, decided. WPF only as emergency fallback |
| 3 | Risk scale | `design.md` used low/medium/high; synthesis used Safe, Situational, Advanced, Dangerous; services docs used 0 to 4 | **Integer 0 to 4** in data (PR #1). UI badge labels map: 0 "Never touched", 1 "Safe", 2 "Situational", 3 "Advanced", 4 "Dangerous" |
| 4 | Profile names | `design.md`: balanced, performance; inspiration: Safe, Balanced, Performance, Privacy, Laptop; synthesis: Minimal, Balanced, Gaming, Dev, Privacy-max, Creator | **8 profiles from PR #1:** safe, balanced, gamer, laptop, office, privacy, developer, minimal. "Performance" maps to gamer, "Dev" to developer. Creator deferred |
| 5 | Restore point | Inspiration and PLAN said mandatory restore point before any apply; PR #1 makes it optional and later | **Journal is mandatory; restore point is optional with consent.** Our undo must work without System Restore, and the 24 hour throttle makes mandatory points unreliable |
| 6 | Update control | WinUtil offers Disable; PLAN listed "Disable Windows Update" as an open question | **Not offered.** Deferral via policy only; wuauserv, UsoSvc, WaaSMedicSvc, BITS are risk 4 |
| 7 | Edge and OneDrive removal | WinUtil removes them; synthesis says avoid removals | **Disable and hide, no removal** in v1. Removal reconsidered after M4 with a tested reinstall path |
| 8 | Drift check scheduling | services-design hinted at a scheduled post-reboot check; synthesis allows only an opt-in one-off task | **Drift on app open. Post-reboot check is an opt-in single-run task that deletes itself** |
| 9 | Schema shape | `design.md` generic `actions[]` model vs PR #1 service-specific schema | **Per-area schemas** sharing core fields (section 7). Generic `actions[]` dropped |
| 10 | Undo field | `design.md` `undo: auto-from-journal or script or one-way` not in PR #1 | Services are always journal-undoable, so no field in v1; `reversibility` added in schema v2 for other areas |
| 11 | SharedAccess | WinUtil sets Disabled | **Manual**; Disabled breaks Mobile Hotspot |
| 12 | SysMain | Commonly disabled by optimizers | **Leave by default**, honesty label `unproven` on any recommendation to change it |
| 13 | MicroWin and image builder | Matrix and WinUtil history rated it strong | Removed upstream; **out of scope** for v1 |
| 14 | Package managers | WinUtil auto-installs Chocolatey | **Never install a package manager silently**; offer with consent |
| 15 | Delivery channel | WinUtil uses `irm | iex` | **Signed binaries only**; no download-and-execute scripts |
| 16 | Elevation | Earlier drafts unspecified | Unelevated UI and CLI; elevated helper per batch only (PR #1 Elevator) |
| 17 | Footprint numbers | Research quoted RAM figures | Treated as **unverified** until measured by our own harness; never claimed in UI without measurement |

### Known gaps (to close)
- Service defaults in YAML come from prior knowledge, not yet harvested from clean VMs (M1 harness).
- Journal (PR #1) does not yet record `FailureActions` or full trigger definitions, only trigger presence.
- No live dependency walk yet; PR #1 relies on catalog `depends_on` and `dependents` (M1 adds SCM `EnumDependentServices`).
- No `gain` or measured impact fields yet (schema v2).
- Package catalog schema not yet written (M2).
- Code signing provider and project domain not chosen.
- Enterprise advisory mode is gated in data (`not: [mdm_enrolled]`) but has no UI yet.

## 11. Open questions for Ron
1. Signing: SignPath OSS or Azure Trusted Signing?
2. Default audience: enthusiasts or general users (affects which profile is preselected)?
3. Install catalog size: small curated (~150) or WinUtil-scale (~240)?
4. Accept community catalog PRs from M1 or after beta?
5. Opt-in crash reports for Windows Box itself, or strictly none?
