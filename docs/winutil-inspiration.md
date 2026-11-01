# WinUtil as inspiration for Windows Box

WinUtil (ChrisTitusTech/winutil, MIT, © CT Tech Group LLC) is a reference, not a blueprint. Windows Box keeps its own schema, structure and services-first vision. Full inventory: `winutil-reference.md`. If any data (app IDs, registry values) is reused, ship the MIT notice in THIRD_PARTY_NOTICES.

## 1. Software installation & management (user favourite)
| WinUtil does | Verdict | Windows Box approach |
|-|-|-|
| 239-app catalog in 10 categories, each with winget + choco IDs, FOSS flag | Adopt idea | Own catalog schema (see design.md §Software): multiple sources ranked, publisher, license, verified hash policy, tags beyond one category |
| winget primary, Chocolatey fallback, auto-installs choco, repairs winget via Microsoft.WinGet.Client | Adapt | Source adapters (winget, choco, scoop, Store, direct MSI); detect + offer repair, never silently install a package manager |
| Batch install of checked apps | Adopt | Queue with per-item status, parallel downloads, serial installs, continue-on-error |
| Upgrade all (`winget upgrade --all`, `choco upgrade all`) | Improve | Show diff of available updates first; per-app pin/skip; respect winget pins (WinUtil already checks pins for PS7 MSI) |
| Uninstall, "Get Installed" | Adopt | Inventory from winget list + ARP registry + Appx, reconciled into one view |
| Export/import selection JSON, skips retired keys | Adopt + improve | Versioned profile files covering apps AND optimizations; migration on import; shareable bundles |
| Errors logged, step timing | Improve | Structured JSONL log per run, exit-code dictionary, retry with alternate source, summary report |
| No rollback of installs | Improve | Record pre-state; uninstall-on-rollback for apps installed by the run; restore point before large batches |

## 2. Optimization depth
| Area | WinUtil | Adopt / adapt / improve |
|-|-|-|
| Tiers | Essential / Customize / Advanced-CAUTION | Adapt: Windows Box uses explicit per-item risk (Low/Med/High) + impact + reversibility, not just a folder |
| Presets | Minimal(4), Standard(12), Advanced(17), AppxDefault | Adopt concept; Windows Box presets are profile-based (Safe, Balanced, Performance, Privacy, Laptop) and show a preview diff |
| Data-driven JSON with OriginalValue/OriginalType | Adopt idea, own schema | Capture actual pre-state at apply time instead of hardcoded "original" (WinUtil assumes defaults, wrong on customised PCs) |
| Undo scripts | Adopt | Every item must declare undo or be flagged one-way; one-way items need explicit confirm |
| Restore point (optional tweak) | Improve | Mandatory snapshot (restore point + own state journal) before any apply; handle 24h restore-point throttle |
| **Services → Manual** | Core lesson | WinUtil shrank its list from ~100+ to 5 after breakage. Windows Box (core priority): per-service catalog with dependencies, hardware/role detection (printer, Bluetooth, Xbox, Hyper-V), Manual over Disabled by default, Disabled only for a vetted few, health check after apply |
| Telemetry/privacy | registry + DiagTrack/WerSvc + tasks | Adopt categories; split into granular items with impact text |
| Visual effects, power (Ultimate Performance, hibernation), network (IPv6, Teredo, DNS presets), Edge/OneDrive/AI removal, appx debloat | Adopt categories | Laptop-aware gating (WinUtil only labels "NOT FOR LAPTOPS"); avoid hard removals, prefer disable |
| Updates: Default / Security / Disable | Adapt | Offer Default + Security-deferral; "Disable" hidden behind High-risk warning with auto-revert reminder |
| Fixes: WU reset, network reset, DISM/SFC, winget reinstall | Adopt | Repair tab |
| Features (WSL, Hyper-V, Sandbox, .NET 3.5, NFS, reg backup, F8 recovery) | Adopt | Features section with requirements check (edition, virtualization) |
| Legacy panel shortcuts, PS profile, SSH server, autologon | Optional | Low priority |
| MicroWin ISO builder | Removed upstream at this commit | Out of scope |
| UI: checkbox lists, descriptions, docs link per item, Tweak Explorer, Config Builder | Adopt presentation | Every item shows description, risk badge, impact, undo, "learn more"; preview before apply |

## Do differently / better
1. Measure real pre-state, never assume defaults. 2. Dry-run/preview diff. 3. Post-apply health checks + auto-rollback. 4. Hardware/role-aware recommendations. 5. Signed, offline-capable releases instead of `irm | iex`. 6. Per-item compatibility (Win10/11 build ranges).
