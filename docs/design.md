# Windows Box — design (research draft)

Priority: **deep services optimization** first; startup apps, scheduled tasks, visual effects, privacy, power, network, debloat, features, updates and software management are secondary with equal rigour. See winutil-inspiration.md for borrowed ideas (WinUtil, MIT).

## Common item schema (Windows Box's own)
```yaml
id: svc.sysmain            # area.name
area: services|startup|tasks|visual|privacy|power|network|apps|features|updates
title, description, impact: {perf, privacy, battery}
risk: low|medium|high
applies_to: {os: ">=10.0.19045", hardware: [...], roles: [...]}
actions: [{type: service, name: SysMain, set: Manual}]   # registry|service|task|appx|feature|script
undo: auto-from-journal | script | one-way
presets: [balanced, performance]
requires_confirm: bool
```
Apply pipeline: detect → preview diff → snapshot (restore point + journal of real pre-values) → apply → health check → report; rollback replays the journal.

## Services (core)
Per-service catalog with dependencies, triggers, hardware/role gating; Manual by default, Disabled only for vetted items (e.g. DiagTrack); never touch security/boot-critical services.

## Software management
- **Catalog**: own JSON/YAML entries: id, name, publisher, categories+tags, license/FOSS, sources `[{winget: id}, {choco: id}, {scoop: id}, {msstore: id}, {direct: url+sha256}]` in priority order, silent args, detection rule.
- **Sources**: adapter per backend; detect winget/choco presence, offer repair/bootstrap with consent; fallback to next source on failure.
- **Search**: local catalog + live `winget search`; filter by category, FOSS, installed, updates available.
- **Presets/bundles**: Essentials, Developer, Creator, Gaming, Office; user bundles exportable/importable (versioned profile, shared with optimization profile).
- **Update checks**: scheduled scan (winget upgrade / choco outdated), per-app pin/ignore, preview list before upgrade-all.
- **Silent installs**: per-source silent flags, `--accept-*-agreements`, timeouts, reboot-required detection, serial installer queue.
- **Logs**: JSONL per run (app, source, version, exit code, duration, stderr tail) + human summary.
- **Rollback**: journal of apps installed/upgraded per run; rollback uninstalls new apps or reinstalls previous version where source supports pinning; restore point before batches >N.
