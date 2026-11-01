# Windows optimization & management tools: profiles
Compiled 1 Nov 2026 (UTC+2), mostly from prior knowledge of each project's docs and repos. Footprint numbers are typical and approximate. **Check versions, licences and prices against the source URLs before publishing.**

## Privacy / tweak utilities
### O&O ShutUp10++ — https://www.oo-software.com/en/shutup10
- Features: about 200 privacy and telemetry switches (Windows, Edge, Copilot/AI, Office, location, updates).
- UX: a single list grouped by area. Each setting has a traffic-light rating (green Recommended, yellow Limited, red Not recommended) and a plain description. Bulk "apply recommended" actions.
- Safety: offers to create a restore point. Undo all, export/import .cfg configs.
- Footprint: portable single exe, no service, no telemetry, zero idle footprint. Freeware, closed source.
- Controversy: minor (closed source). This is the gold standard for presenting risk.

### privacy.sexy — https://privacy.sexy, https://github.com/undergroundwires/privacy.sexy
- Features: thousands of scripts for privacy, cleanup and bloat. It generates a .bat/.ps1 and shows you the code.
- UX: web or desktop app with Standard/Strict presets. Each item shows its script and a revert script.
- Safety: revert scripts for every item, full transparency. No automatic restore point.
- Footprint: nothing installed (the scripts run once). AGPL-3.0.

### Winaero Tweaker — https://winaero.com/winaero-tweaker/
- Features: 200+ shell and UI tweaks (context menus, classic apps, branding, behaviour).
- UX: tree navigation with a description per tweak and "reset to defaults" per page. Export/import.
- Footprint: portable mode available, no service. Freeware, closed source.

### Optimizer (hellzerg) — https://github.com/hellzerg/optimizer
- Features: tweaks for telemetry, services, startup manager, hosts editor, app installer, cleaner, UWP removal.
- UX: dense WinForms toggles, multilingual. Templates for unattended runs.
- Safety: weak granularity and no per-item risk labels. Footprint: portable exe, GPL-3.0.
- Note: the repo has been archived/paused at times.

### Win11Debloat (Raphire) — https://github.com/Raphire/Win11Debloat
- Features: a PowerShell script that removes bloat apps, disables telemetry, Bing search and Copilot/Recall, and applies Explorer and taskbar tweaks.
- UX: a default mode, custom interactive mode and CLI switches (sysprep/new-user mode).
- Safety: restore-point prompt, .reg files to undo many tweaks. MIT, zero footprint.

### Sophia Script — https://github.com/farag2/Sophia-Script-for-Windows
- Features: 150+ functions covering almost every setting. Every function has Enable/Disable parameters.
- UX: edit Sophia.ps1 to comment functions in or out, or use the SophiApp GUI (being rewritten). Tab completion.
- Safety: symmetric undo by design, restore point, checks before running. MIT, zero footprint.

### Chris Titus WinUtil
See /workspace/windows-box-services/ (separate research stream).

## Cleaners / suites
### BleachBit — https://www.bleachbit.org
- Features: cleans caches, logs and temp files for 2,000+ application entries (CleanerML), shreds files, wipes free space.
- UX: checklist with a warning per option and a preview before deleting.
- Safety: preview, warnings on risky items. No registry cleaning.
- Footprint: portable, no service, GPL-3.0. The model to follow.

### CCleaner (Gen Digital/Avast) — https://www.ccleaner.com
- Features: cleaner, registry cleaner, driver/software updater, "performance optimizer" (puts apps to sleep), health check.
- 2017 breach: CCleaner 5.33 shipped with a Floxif backdoor (about 2.27M users, second stage aimed at tech firms). Later: Avast data-collection backlash, bundled Avast/Chrome offers, aggressive upsell, monitoring/auto-update that was hard to switch off, and Microsoft Defender flagging it as PUA (2020).
- Footprint: resident background/monitoring tasks, telemetry. Freemium.
- Lesson: anti-pattern on almost every axis.

### Wise Care 365 — https://www.wisecleaner.com
- Features: cleaner, registry cleaner and defrag, privacy eraser, startup manager, process monitor.
- UX: one-click "check" score. Footprint: tray agents. Freemium with upsell nags. Its registry cleaning is snake oil.

### Glary Utilities — https://www.glarysoft.com
- Features: 20+ modules (registry repair, shortcuts fixer, startup, uninstaller, file shredder, duplicate finder).
- UX: "1-Click Maintenance". Safety: registry backups. Freemium, upsell and bundled offers in installers, background scheduling.

## Uninstallers / app management
### Bulk Crap Uninstaller — https://www.bcuninstaller.com, https://github.com/Klocman/Bulk-Crap-Uninstaller
- Features: batch and quiet uninstall of Win32, Store, Steam and other apps. Scans for leftovers with confidence scores, finds orphaned apps.
- UX: rich sortable grid. Safety: leftover confidence ratings, restore point. Apache-2.0, portable.

### Revo Uninstaller — https://www.revouninstaller.com
- Features: uninstall plus leftover scan (Safe/Moderate/Advanced), install tracing (Pro), Hunter mode.
- Safety: restore point plus registry backup. Free and Pro versions, closed source. The Pro version adds tray monitoring.

### UniGetUI (formerly WingetUI, Martí Climent; now Devolutions) — https://github.com/marticliment/UniGetUI
- Features: a GUI over winget, Scoop, Chocolatey, pip, npm, .NET tool and PowerShell Gallery. Batch install/update/uninstall, bundles (import/export), auto-update notifications.
- UX: excellent. Footprint: optional tray/background update checks (can be switched off). MIT.
- Controversy: 2024 claims of an AI-generated, bundleware-style lookalike site, plus the move to Devolutions sponsorship.

### Ninite — https://ninite.com
- Features: choose apps, get one installer. Silently says no to toolbars. Ninite Pro adds updates and management.
- UX: the simplest there is. Closed source, freemium. Limited catalogue.

## Shell / UI customisation
### ExplorerPatcher — https://github.com/valinet/ExplorerPatcher
- Features: Windows 10 taskbar/Start, classic context menu, Alt-Tab, weather.
- Footprint: injects DLLs into explorer. Breaks with Windows updates. GPL-2.0.
- Controversy: Defender flagged it as malware in 2024.

### StartAllBack — https://www.startallback.com
- Features: polished Windows 7/10-style taskbar, Start and Explorer. Paid (about $5), trial, closed source, very light.
- Also breaks after updates.

### ExplorerTabUtility — https://github.com/w4po/ExplorerTabUtility
- Features: opens new folders as tabs in the existing Explorer window, with hotkeys. Tray-resident, MIT.

### TranslucentTB — https://github.com/TranslucentTB/TranslucentTB
- Features: transparent taskbar states, available from the Store. Tray process (about 10–20 MB). GPL-3.0.

### Windhawk — https://windhawk.net
- Features: a mod marketplace (taskbar, Explorer and more). Each mod's C++ source is viewable and compiled locally.
- Footprint: a resident service plus injection. Mods can be toggled individually. GPL-3.0. The best extensibility model in this list.

### PowerToys — https://github.com/microsoft/PowerToys
- Features: about 25 modules (Run/Command Palette, FancyZones, Keyboard Manager, Awake, File Locksmith, Hosts editor, Registry Preview, etc.).
- UX: Fluent settings, toggle per module. Footprint: runner plus enabled modules (about 50–200 MB). Optional telemetry. MIT.

## Power-user / system
### Sysinternals Autoruns / Process Explorer — https://learn.microsoft.com/sysinternals
- Autoruns lists every autostart location (Run keys, services, tasks, drivers, shell extensions, Winsock, codecs). It hides Microsoft entries, checks VirusTotal and signatures, and disables entries non-destructively.
- Portable, free, no footprint. This is the depth benchmark for startup management.

### Process Lasso (Bitsum) — https://bitsum.com
- Features: ProBalance auto-priority, CPU affinity, power-plan switching, rules per process.
- Footprint: resident core engine (small). Freemium with nags in the free version.

## Distros and image builders
### Atlas OS — https://atlasos.net
- Applied to stock Windows with AME Wizard. Disables Defender (optional), mitigations (e.g. Spectre fixes, optional), services and updates.
- Documents every change, but some changes are hard to reverse. Controversy over security trade-offs and fake "Atlas" malware copies.

### ReviOS — https://revi.cc
- Similar to Atlas: a playbook run through AME Wizard. Large reductions in services and tasks. The same reversibility concerns.

### Tiny11 builder (NTDEV) — https://github.com/ntdevlabs/tiny11builder
- PowerShell that strips a Windows ISO with DISM. Tiny11 core version can't be serviced.
- Lesson: removing components breaks updates.

### NTLite — https://www.ntlite.com
- A pro image editor: component removal with compatibility protection, drivers, updates, unattended setup, presets.
- Paid tiers, closed source. Safety comes from "compatibility" locks that keep dependencies. The depth benchmark for builders.

## Others worth noting
- **Wintoys** (Store, all-in-one, closed source).
- **WinaeroTweaker-like Ultimate Windows Tweaker** (TheWindowsClub).
- **Debotnet**, **W10Privacy** (fine-grained, with colour-coded risk).
- **Patch My PC Home Updater**.
- **WizTree/WinDirStat** (disk usage).
- **Windows built-ins:** Storage Sense, winget, PC Manager (Microsoft, China first).
- **Flyby11 / Rufus** (bypassing setup requirements).
