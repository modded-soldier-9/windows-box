# Recommended tech stack for Windows Box

Goals: native Windows 11 look (Mica, Fluent, snap layouts, accessibility), tiny idle footprint (zero resident processes), deep Win32 access (SCM, registry, WMI, Task Scheduler, AppX, winget COM), signed portable distribution.

| Option | UX fidelity on Win11 | Resources | System access | Dev velocity | Verdict |
|-|-|-|-|-|-|
| **WinUI 3 / Windows App SDK + .NET 8/9 (C#)** | Best: native Fluent controls, Mica, NavigationView, accessibility & high-contrast for free | ~60–120 MB RAM while open, 0 when closed; NativeAOT/trim cuts startup | Full: P/Invoke/CsWin32, ServiceController, Microsoft.Win32.Registry, TaskScheduler, Microsoft.Management.Deployment (winget COM) | Good; XAML tooling improving; unpackaged/self-contained supported | **Recommended** |
| WPF + WPF-UI (lepoco) | Very good Fluent imitation, mature | Similar | Full | Highest maturity, huge ecosystem | Strong fallback if WinUI 3 pain bites |
| Avalonia 11 | Good, Fluent theme but not native Mica/feel | Similar | Full via .NET | Cross-platform (irrelevant here) | Not needed — Windows-only product |
| Tauri 2 (Rust + WebView2) | Web-styled; Fluent via CSS, never quite native | WebView2 ~100–200 MB while open | Rust windows-rs is excellent | Fast UI iteration | Good alt if team is web-first |
| Electron | Non-native | 200–400 MB | Node + native modules | Fast | Rejected (violates P2) |
| PowerShell + WPF (WinUtil model) | Dated | Low | Full but slow, AV-flagged `irm|iex` | Easy community PRs | Rejected for core; keep PS export for scripts |

## Recommendation
**C# / .NET 9 + WinUI 3 (Windows App SDK 1.6+), unpackaged self-contained, trimmed**, with:
- **Core engine as a separate .NET class library + CLI (`wbox.exe`)** — UI is a thin shell; CLI enables scripting, CI tests, and headless profile apply.
- **Elevation model:** UI runs as user; a short-lived elevated helper process (launched per apply batch via UAC, exits after) — no resident service.
- **Data:** declarative YAML/JSON catalogs (tweaks, services, apps) in repo, schema-validated, community-PR friendly; JSONL journal for undo under `%ProgramData%\WindowsBox`.
- **Packaging:** signed MSIX + portable ZIP + winget manifest; Azure Trusted Signing / SignPath (free for OSS) to avoid SmartScreen/AV hits.
- **Testing:** Hyper-V/Windows Sandbox VM matrix (Home/Pro, 23H2/24H2/25H2) running the CLI to verify apply → undo returns to identical state.
Tradeoff accepted: WinUI 3 has rougher tooling than WPF; mitigate with CommunityToolkit.Mvvm and the WinUI 3 Gallery samples.

Sources: https://learn.microsoft.com/windows/apps/windows-app-sdk/ · https://learn.microsoft.com/windows/package-manager/ · https://github.com/microsoft/CsWin32 · https://v2.tauri.app · https://avaloniaui.net · https://github.com/lepoco/wpfui
