# Network safeguards

Windows Box is designed for **offline-first** use. A signed build must work with
every network feature turned off. Anything that contacts the network is **opt-in**,
**off by default**, and **clearly disclosed** in Settings.

## Features that can use the network

| Feature | Default | Where disclosed | What it contacts |
|-|-|-|-|
| Check for updates | **Off** | Settings → Updates | GitHub Releases API (`api.github.com`) only when enabled |
| Update preference | **Manual** | Settings → Updates | Automatic only checks on open when “Check for updates” is also on; never silent-installs |
| Crash report upload | **Off** | Settings → Privacy | Local dumps under `%ProgramData%\WindowsBox\crashes`; upload only if opted in |
| Optional telemetry | **Off** (when present) | Settings → Privacy | Nothing until enabled |
| Plugin marketplace fetch | **Off** / in-repo only (when present) | Settings → Plugins | Only allow-listed plugins merged into this repo |

## Offline contract

1. Fresh install / first run never phones home.
2. Settings file lives under `%ProgramData%\WindowsBox\config\settings.json` (or a test temp dir).
3. `CheckForUpdatesEnabled=false` and `UpdatePreference=Manual` are the shipped defaults.
4. CI and developer hosts never run apply against a real machine; update checks in unit tests use fakes (no live HTTP).

## Related docs

- `docs/quick-start.md` — getting started without network features
- `docs/trust-guide.md` — one-line installer inspection (when published)
- `SECURITY.md` — vulnerability reporting
