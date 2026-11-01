# Services optimization design (Windows Box)

## Principles
1. **Manual before Disabled.** Manual/trigger-start costs ~0 RAM when idle; Disabled is reserved for vetted, risk ≤2 items.
2. **Measure, don't assume.** Read the real current start type, delayed flag, trigger info and failure actions at apply time.
3. **Detect before recommend.** Hardware/role probes gate every item (printers installed? BT radio? battery? Hyper-V/WSL on? domain/Intune joined? Xbox app/Game Pass installed? webcam?).
4. **Never fight Windows.** Protected services → policy alternatives; no ownership hacks, no killing WaaSMedic.
5. **Explain everything.** Each card: what it does, what you gain (measured idle RAM/CPU of that svchost), what breaks, risk, how to undo, source links.

## Data model (per service, YAML in repo)
```yaml
id: svc.diagtrack
name: DiagTrack
display: Connected User Experiences and Telemetry
defaults: {pro: Auto, home: Auto, enterprise: Auto}   # verified per build by CI VM
builds: ">=22621"
risk: 2
recommend: {privacy: Disabled, balanced: Manual, gamer: Disabled}
gates: {not: [mdm_enrolled]}
breaks: ["Optional diagnostic data upload", "Some Feedback Hub features"]
alternatives: [{policy: "HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\DataCollection!AllowTelemetry=0"}]
sources: [https://learn.microsoft.com/...]
```

## Profiles
Safe (risk ≤1 only) · Balanced (default) · Gamer · Laptop/Battery · Office · Privacy · Developer · Minimal (low-RAM). Profiles compose (e.g. Laptop+Privacy); conflicts resolved by the most conservative choice; the result is shown as a **preview diff** ("12 services: 9 → Manual, 3 → Disabled; est. −140 MB idle").

## Apply pipeline
1. **Probe** hardware/roles/edition/build/joined state.
2. **Plan**: resolve profile → candidate set → dependency check (walk `DependOnService` both ways via SCM `EnumDependentServices`; block disabling anything that a non-disabled Auto service depends on) → remove gated items.
3. **Preview** diff with per-item toggles; one-way or risk ≥3 needs explicit confirm.
4. **Snapshot**: JSON journal of exact pre-state (`Start`, `DelayedAutostart`, `TriggerInfo` presence, `FailureActions`, current running state) + `reg export` of each service key + system restore point (handle 24h throttle via `SystemRestorePointCreationFrequency` only with consent; otherwise journal alone).
5. **Apply** via SCM API (`ChangeServiceConfig`, `ChangeServiceConfig2`) in a short-lived elevated helper; do not stop running services unless asked ("takes effect after reboot").
6. **Verify**: re-read config; after reboot (opt-in scheduled one-shot check, no resident service), run health checks: network up, audio endpoint present, Start/Search responsive, Windows Update scan succeeds, Store reachable, printers enumerate if any.
7. **Rollback**: per item, per batch, or full "factory defaults for this build" (from our verified defaults table). Offline recovery: journal + generated `restore-services.cmd`/`.reg` saved to `%ProgramData%\WindowsBox\backups` and optionally Desktop, usable from Safe Mode/WinRE.

## Drift detection
On app open (no background agent), compare the journal's intended state to live state; show "Windows Update reset 3 services" with one-click re-apply or accept.

## Measurement (honesty feature)
Show per-service idle working set (svchost split makes this measurable on ≥3.5 GB RAM systems), boot-time contribution (from ETW boot trace optional), and label claims like "SysMain off = faster" as **unproven** where benchmarks don't support them.

## Risks to manage
- Per-user `_xxxx` services: edit template, warn.
- Build-specific defaults: CI matrix on Home/Pro/Enterprise × 23H2/24H2/25H2.
- Enterprise/MDM: detect and default to read-only "advisory" mode.
- Antivirus heuristics: signed binaries, no PowerShell download cradles.
