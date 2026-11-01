# Windows optimization & management tools: profiles
Compiled 1 Nov 2026 (UTC+2); **fact-checked 1 Nov 2026** against GitHub API, vendor pages and press (sources inline). Licences marked ✔ were verified via `api.github.com/repos/<repo>`. RAM/footprint figures were **not measured** and are marked *(unverified)*.

## Privacy / tweak utilities
### O&O ShutUp10++ — https://www.oo-software.com/en/shutup10
- Features: nearly 300 privacy/security settings in 20+ categories, count varies by Windows edition ([manual](https://manuals.oo-software.com/ooshutup10/oosu10-manual-en.pdf); latest 3.6.1135, Sep 2026 — [blog](https://blog.oo-software.com/en/new-oo-shutup10-version-3-6-1135-with-9-new-program-languages-settings-filter-by-category-more/)). Switches cover (Windows, Edge, Copilot/AI, Office, location, updates).
- UX: a single list grouped by area. Each setting has a traffic-light rating (green Recommended, yellow Limited, red Not recommended) and a plain description. Bulk "apply recommended" actions.
- Safety: offers to create a restore point. Undo all, export/import .cfg configs.
- Footprint: portable single exe, no service, no telemetry, zero idle footprint. Freeware, closed source.
- Controversy: minor (closed source). This is the gold standard for presenting risk.

### privacy.sexy — https://privacy.sexy, https://github.com/undergroundwires/privacy.sexy
- Features: thousands of scripts for privacy, cleanup and bloat. It generates a .bat/.ps1 and shows you the code.
- UX: web or desktop app with Standard/Strict presets. Each item shows its script and a revert script.
- Safety: revert scripts for every item, full transparency. No automatic restore point.
- Footprint: nothing installed (the scripts run once). AGPL-3.0 ✔.

### Winaero Tweaker — https://winaero.com/winaero-tweaker/
- Features: 200+ shell and UI tweaks (context menus, classic apps, branding, behaviour).
- UX: tree navigation with a description per tweak and "reset to defaults" per page. Export/import.
- Footprint: portable mode available, no service. Freeware, closed source.

### Optimizer (hellzerg) — https://github.com/hellzerg/optimizer
- Features: tweaks for telemetry, services, startup manager, hosts editor, app installer, cleaner, UWP removal.
- UX: dense WinForms toggles, multilingual. Templates for unattended runs.
- Safety: weak granularity and no per-item risk labels. Footprint: portable exe, GPL-3.0 ✔.
- Note: the repo is **archived (read-only)**; last push 20 Jan 2026 ([GitHub](https://github.com/hellzerg/optimizer)).

### Win11Debloat (Raphire) — https://github.com/Raphire/Win11Debloat
- Features: a PowerShell script that removes bloat apps, disables telemetry, Bing search and Copilot/Recall, and applies Explorer and taskbar tweaks.
- UX: a default mode, custom interactive mode and CLI switches (sysprep/new-user mode).
- Safety: restore-point prompt, .reg files to undo many tweaks. MIT ✔, zero footprint.

### Sophia Script — https://github.com/farag2/Sophia-Script-for-Windows
- Features: 150+ functions covering almost every setting. Every function has Enable/Disable parameters.
- UX: edit Sophia.ps1 to comment functions in or out, or use the SophiApp GUI (being rewritten). Tab completion.
- Safety: symmetric undo by design, restore point, checks before running. MIT ✔, zero footprint.

### Chris Titus WinUtil
See /workspace/windows-box-services/ (separate research stream).

## Cleaners / suites
### BleachBit — https://www.bleachbit.org
- Features: cleans caches, logs and temp files via CleanerML definitions (the "2,000+ entries" figure is *unverified*; extra coverage comes from optional Winapp2.ini import), shreds files, wipes free space.
- UX: checklist with a warning per option and a preview before deleting.
- Safety: preview, warnings on risky items. No registry cleaning.
- Footprint: portable, no service, GPL-3.0 ✔. The model to follow.

### CCleaner (Gen Digital/Avast) — https://www.ccleaner.com
- Features: cleaner, registry cleaner, driver/software updater, "performance optimizer" (puts apps to sleep), health check.
- 2017 breach: the signed **32-bit** CCleaner 5.33.6162 (released 15 Aug 2017) and CCleaner Cloud 1.07.3191 carried a two-stage backdoor; 2.27M users ran it; fixed 12–15 Sep 2017. Second stage reached at least 20 machines in 8 tech/telecom organisations ([Piriform notice](https://community.ccleaner.com/t/security-notification-for-ccleaner-v5-33-6162-and-ccleaner-cloud-v1-07-3191-for-32-bit-windows-users/64184), [Talos](https://blog.talosintelligence.com/avast-distributes-malware/), [Avast](https://blog.avast.com/progress-on-ccleaner-investigation)).
- Aug 2018: v5.45 pulled after backlash over "Active Monitoring" usage-data collection that couldn't be disabled ([BleepingComputer](https://www.bleepingcomputer.com/news/software/ccleaner-v545-pulled-due-to-anger-over-usage-data-collection/)).
- **July 2020**: Defender flagged free/trial installers as PUA (`PUA:Win32/CCleaner`, `PiriformBundler`) for bundling Avast/AVG/Chrome/Google Toolbar; fixed installer released 29 Jul 2020 ([Microsoft](https://www.microsoft.com/en-us/wdsi/threats/malware-encyclopedia-description?Name=PUA%3AWin32%2FPiriformBundler), [BleepingComputer](https://www.bleepingcomputer.com/news/microsoft/microsoft-now-detects-ccleaner-as-a-potentially-unwanted-application/)). Owner is now Gen Digital (Avast merger 2022).
- Footprint: resident background/monitoring tasks, telemetry *(RAM unverified)*. Freemium.
- Lesson: anti-pattern on almost every axis.

### Wise Care 365 — https://www.wisecleaner.com
- Features: cleaner, registry cleaner and defrag, privacy eraser, startup manager, process monitor.
- UX: one-click "check" score. Footprint: tray agents. Freemium with upsell nags. Its registry cleaning is snake oil.

### Glary Utilities — https://www.glarysoft.com
- Features: 20+ modules (registry repair, shortcuts fixer, startup, uninstaller, file shredder, duplicate finder).
- UX: "1-Click Maintenance". Safety: registry backups. Freemium, upsell and bundled offers in installers, background scheduling.

## Uninstallers / app management
### Bulk Crap Uninstaller — https://www.bcuninstaller.com, https://github.com/BCUninstaller/Bulk-Crap-Uninstaller (moved from Klocman/)
- Features: batch and quiet uninstall of Win32, Store, Steam and other apps. Scans for leftovers with confidence scores, finds orphaned apps.
- UX: rich sortable grid. Safety: leftover confidence ratings, restore point. Apache-2.0 ✔, portable.

### Revo Uninstaller — https://www.revouninstaller.com
- Features: uninstall plus leftover scan (Safe/Moderate/Advanced), install tracing (Pro), Hunter mode.
- Safety: restore point plus registry backup. Free and Pro versions, closed source. The Pro version adds tray monitoring.

### UniGetUI (formerly WingetUI, Martí Climent; now Devolutions) — https://github.com/Devolutions/UniGetUI, official site https://devolutions.net/unigetui/
- Features: a GUI over WinGet, Scoop, Chocolatey, pip, npm, Bun, .NET Tool and PowerShell Gallery (newer builds also macOS/Linux managers) ([README](https://github.com/Devolutions/UniGetUI/blob/main/README.md)). Built on WinUI 3/.NET since v3.0 (Mar 2024). Batch install/update/uninstall, bundles (import/export), auto-update notifications.
- UX: excellent. Footprint: optional tray/background update checks (can be switched off). MIT ✔.
- Controversy: renamed WingetUI → UniGetUI (announced 13 Mar 2024, [#1900](https://github.com/Devolutions/UniGetUI/discussions/1900)). Third parties run **fake sites wingetui.com and unigetui.com**, flagged in the README; the registrar declined to act (May 2025, [#3631](https://github.com/Devolutions/UniGetUI/discussions/3631)). The project is now **maintained by Devolutions** (stewardship, still MIT), not merely sponsored. *(The earlier "AI-generated bundleware site" wording is unverified.)*

### Ninite — https://ninite.com
- Features: choose apps, get one installer. Silently says no to toolbars. Ninite Pro adds updates and management: $1/machine/month for first 20, $0.50 next 400, $0.25 beyond ([ninite.com/pro](https://ninite.com/pro)).
- UX: the simplest there is. Closed source, freemium. Limited catalogue.

## Shell / UI customisation
### ExplorerPatcher — https://github.com/valinet/ExplorerPatcher
- Features: Windows 10 taskbar/Start, classic context menu, Alt-Tab, weather.
- Footprint: injects DLLs into explorer and drops files in C:\Windows. Breaks with Windows updates. GPL-2.0 ✔.
- Controversy: repeated Defender **detections (project says false positives)** through 2024: Trojan:Win32/Wacatac.B!ml (21 Feb 2024, [#2885](https://github.com/valinet/ExplorerPatcher/issues/2885)), HackTool:Win64/ExplorerPatcher!MTB (May 2024, [#3228](https://github.com/valinet/ExplorerPatcher/issues/3228)), Trojan:Win32/Casdet!rfn (Jul 2024, [#3444](https://github.com/valinet/ExplorerPatcher/issues/3444)). Since release 69.4 (Oct 2025) the release notes say Defender no longer flags it, but warn future builds may be flagged ([releases](https://github.com/valinet/ExplorerPatcher/releases)). Also warns of malware copies on unofficial sites.

### StartAllBack — https://www.startallback.com
- Features: polished Windows 7/10-style taskbar, Start and Explorer. Paid: $4.99 for 1 PC, $8.99 for 2, $11.99 for 3, one-time; 30-day full trial ([startisback.com](https://startisback.com/)). Closed source, very light *(unverified)*.
- Also breaks after updates.

### ExplorerTabUtility — https://github.com/w4po/ExplorerTabUtility
- Features: opens new folders as tabs in the existing Explorer window, with hotkeys. Tray-resident, MIT ✔ (last push May 2025).

### TranslucentTB — https://github.com/TranslucentTB/TranslucentTB
- Features: transparent taskbar states, available from the Store. Tray process (RAM *unverified*). GPL-3.0 ✔.

### Windhawk — https://windhawk.net
- Features: a mod marketplace (taskbar, Explorer and more). Each mod's C++ source is viewable and compiled locally.
- Footprint: a resident service plus injection. Mods can be toggled individually. GPL-3.0 ✔ ([GitHub](https://github.com/ramensoftware/windhawk)). The best extensibility model in this list.

### PowerToys — https://github.com/microsoft/PowerToys
- Features: 25+ utilities *(exact count unverified; changes per release)* (Run/Command Palette, FancyZones, Keyboard Manager, Awake, File Locksmith, Hosts editor, Registry Preview, etc.).
- UX: Fluent settings, toggle per module. Footprint: runner plus enabled modules (RAM *unverified*). Optional telemetry. MIT ✔.

## Power-user / system
### Sysinternals Autoruns / Process Explorer — https://learn.microsoft.com/sysinternals
- Autoruns lists every autostart location (Run keys, services, tasks, drivers, shell extensions, Winsock, codecs). It hides Microsoft entries, checks VirusTotal and signatures, and disables entries non-destructively.
- Portable, free, no footprint. This is the depth benchmark for startup management.

### Process Lasso (Bitsum) — https://bitsum.com
- Features: ProBalance auto-priority, CPU affinity, power-plan switching, rules per process.
- Footprint: resident core engine (small). Freemium with nags in the free version.

## Distros and image builders
### Atlas OS — https://atlasos.net
- Applied to stock Windows with AME Wizard. Offers **optional** toggles for Defender and CPU mitigations; its own docs advise against disabling Defender ([Atlas docs: security](https://docs.atlasos.net/post-install/atlas-folder/security/)). GPL-3.0 ✔.
- Documents every change, but some changes are hard to reverse. Controversy over security trade-offs; reports of fake "Atlas" malware copies are *unverified here*.

### ReviOS — https://revi.cc
- Similar to Atlas: a playbook run through AME Wizard. Large reductions in services and tasks. The same reversibility concerns.

### Tiny11 builder (NTDEV) — https://github.com/ntdevlabs/tiny11builder
- PowerShell that strips a Windows ISO with DISM. Tiny11 core version can't be serviced. **No licence file** ✔ (all rights reserved by default).
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
- **Windows built-ins:** Storage Sense, winget, PC Manager (Microsoft; launched in China, distributed outside mainland China from v1.2.4.0, still not offered in some EU countries — [docs](https://mspcmanager.github.io/mspcm-docs/zh-cn/get-started/about.html), [heise](https://www.heise.de/en/news/Microsoft-Optimierungssoftware-PC-Manager-wird-Bestandteil-von-Windows-11-9732809.html)).
- **Flyoobe (formerly Flyby11, MIT ✔) / Rufus** (bypassing setup requirements).
