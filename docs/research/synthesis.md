# Synthesis: what Windows Box should be

## What the best tools do well
1. **Risk rating per item:** O&O's traffic lights, NTLite's compatibility locks, BCU's confidence on leftovers.
2. **Transparency:** privacy.sexy shows the exact script and its revert script, Windhawk shows mod source, Atlas documents every change.
3. **Symmetric undo:** Sophia gives every function an Enable and a Disable. ShutUp10 has undo all.
4. **Zero footprint:** ShutUp10, BleachBit, Autoruns and WinUtil are portable, have no service and send no telemetry.
5. **Depth benchmarks:**
   - Autoruns for startup (every autostart point)
   - BleachBit CleanerML for app cleaning
   - BCU for uninstalling
   - UniGetUI for packages (many sources, bundles)
   - NTLite for image building
6. **Shareable configs:** ShutUp .cfg files, WinUtil JSON, UniGetUI bundles.

## Where they fall short
- **Suites (CCleaner, Wise, Glary):**
  - resident agents, upsells and bundleware
  - registry cleaning that is snake oil
  - "health scores" that play on fear
  - a supply-chain breach (CCleaner 2017)
- **Script tools:** powerful, but the UX is a wall of checkboxes with no explanation of impact or check that settings stayed applied.
- **Distros (Atlas/ReviOS/Tiny11):** security trade-offs (Defender off, CPU mitigations off), they break servicing and updates, and some changes can't be undone.
- **Shell mods:** DLL injection that breaks after Windows updates and gets flagged by antivirus.
- **Drift:** no tool checks whether Windows Update quietly undid your tweaks.
- **Fragmentation:** each tool has its own UI and format, so users run five of them.

## Proposed Windows Box feature set
### P1: a truly great experience
- **Onboarding:** detects the system (laptop or desktop, GPU, edition, domain/MDM, use profile) and suggests a preset: Minimal, Balanced, Gaming, Dev, Privacy-max, Creator.
- **Every item on one card:**
  - what it does and what you gain
  - what breaks
  - a risk badge: Safe, Situational, Advanced, Dangerous
  - reversibility
  - the exact command and registry change, with a "why" source link
- **Before running:**
  - search across everything
  - a diff/preview screen ("these 23 changes")
  - a dry run
- **History:** a timeline you can roll back to any point.
- **Configs:** shareable profiles as signed JSON or YAML, with import from WinUtil and ShutUp .cfg.
- **Look:** Fluent/WinUI, keyboard-first, accessible, plain language.

### P2: no compromise on resources
- **No resident processes:** a portable single exe with no service, tray icon, scheduler or telemetry. Any update check is an opt-in button.
- **Drift check on demand:** "re-verify" reads the current state and doesn't need a daemon. An optional one-off scheduled task is the only exception, created only if the user asks for it.
- **Measured results:** benchmark before and after (idle RAM, number of processes and services, boot time, disk), so gains are measured, not claimed.
- **Light build:** small binary (e.g. Rust/C# AOT) that cleans up after itself.

### P3: real depth throughout
- **Services:** see the services research stream. Prefer Manual over Disabled, track dependencies.
- **Startup:** Autoruns-level coverage, with signature and VirusTotal checks and a "Microsoft hidden" filter.
- **Scheduled tasks:** full coverage.
- **Privacy and AI:** telemetry, Copilot, Recall, ads, Bing/Edge, Office.
- **Apps:**
  - packages across winget, Scoop and Choco: batch install, update all, bundles
  - BCU-style uninstall with leftover confidence scores
  - Store app and provisioned package removal, with a reinstall path
- **Cleaner:** BleachBit-style, with CleanerML-compatible definitions, previews and component-store cleanup (DISM).
- **System settings:** power plans and per-device power, network, Windows Update policy (defer, drivers off), optional features and capabilities.
- **Explorer, taskbar and context menus:** registry-based only, no injection. Link to Windhawk/StartAllBack for deeper changes.
- **Repair:** SFC, DISM, reset Winget, Store, network and the search index.
- **Image builder (later):** debloated ISO that can still be serviced, with an NTLite-style dependency guard.

## Above and beyond
- **Drift detection after Windows feature updates:** "12 tweaks were reverted by update 24H2, re-apply?"
- **Evidence:** every tweak cites a source and has a measured impact, built from community benchmark data (opt-in, anonymous, off by default).
- **Dependency graph:** before disabling something, show what depends on it (services ↔ features ↔ apps).
- **"Explain my PC":** a read-only audit of what's slowing the PC or leaking data, with no changes made.
- **Undo safety net:** restore point plus a per-change registry and state snapshot, so undo works even without System Restore.
- **Safe-boot rollback:** a recovery script on disk that undoes everything from WinRE.
- **Profiles as code:** Git-friendly configs, CLI for fleets or new installs, and an unattend.xml export.
- **Plugins:** an open, community-reviewed tweak catalogue (like Windhawk/privacy.sexy), and each entry must come with a revert.
- **Security posture meter:** warns when a tweak lowers security (Defender, CPU mitigations, UAC, SmartScreen, updates).

## Anti-patterns to avoid
- Registry "cleaning" or defragging, RAM "boosters" and "health score" scare tactics.
- Disabling Defender, CPU mitigations, UAC, Windows Update or VBS/HVCI by default.
- Removing components, such as Edge WebView2, the Store or servicing stacks, in ways that break updates.
- Resident services, tray agents, telemetry, bundleware, upsell nags and dark patterns.
- Opaque one-click "optimize everything" with no preview or undo.
- Placebo tweaks: e.g. LargeSystemCache, SvcHostSplitThreshold myths, prefetch/superfetch removal on SSDs, "network throttling" hacks.
- Running unsigned script downloads (irm|iex) without verification. Sign releases and use reproducible builds.
- DLL injection into explorer.
- Ignoring domain, MDM or enterprise policy. Detect it and warn.
