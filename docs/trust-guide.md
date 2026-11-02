# Trust guide — one-line installer

Windows Box prefers distributing **signed binaries**. A convenience one-liner is
also published for enthusiasts who accept the risk of `irm | iex`.

```powershell
# Inspect first (recommended):
irm https://raw.githubusercontent.com/modded-soldier-9/windows-box/main/install.ps1 | more

# Or download and read locally, then:
iex (Get-Content .\install.ps1 -Raw)

# Only after you understand it:
irm https://raw.githubusercontent.com/modded-soldier-9/windows-box/main/install.ps1 | iex
```

Release asset form (same script, pinned to a tag):

```powershell
irm https://github.com/modded-soldier-9/windows-box/releases/latest/download/install.ps1 | iex
```

## What the script does (line-by-line intent)

1. **Parameters** — optional `-Version`, `-Channel stable|beta`, `-InstallDir`, `-DryRun`.
2. **Resolve release** — calls GitHub Releases API for this repository only.
3. **Select assets** — requires `WindowsBox-*-win-x64.zip` **and** matching `*.sha256`.
4. **Download those two files only** — no other URLs, no nested `irm` of scripts.
5. **SHA256 verify** — compares the ZIP hash to the published checksum; **aborts on mismatch**.
6. **Expand** — extracts the verified ZIP into the install directory.
7. **Require launchables** — aborts if `WindowsBox.App.exe`, `wbox.exe`, `WindowsBox.Elevator.exe`, or `catalog/services` are missing (packaging guard).
8. **Authenticode** — when present on extracted executables, requires `Valid`; unsigned builds warn after SHA256 success.
9. **Launch once** — starts `WindowsBox.App.exe` (pass `-NoLaunch` to skip). Does not install a background agent or tray process.
10. **Cleanup** — deletes the temp download folder.
11. **Exit** — network features stay off by default.

## What it deliberately does *not* do

- Does not download or execute any second-stage script.
- Does not disable Defender, UAC, or SmartScreen.
- Does not enable update checks, crash upload, or telemetry.
- Does not apply any Windows tweaks on install.

## How to inspect before running

```powershell
# Save without executing
Invoke-WebRequest https://raw.githubusercontent.com/modded-soldier-9/windows-box/main/install.ps1 -OutFile install.ps1
notepad .\install.ps1

# Dry-run (resolve URLs only)
powershell -File .\install.ps1 -DryRun
```

Compare against the copy in this repository (`install.ps1` at the repo root).
If the remote bytes differ from a tag you trust, **do not run it**.

## Offline / highest trust path

1. Open the GitHub Release in a browser.
2. Download `WindowsBox-*-win-x64.zip` and `*.sha256`.
3. `Get-FileHash -Algorithm SHA256` the ZIP; match the checksum file.
4. Expand manually; optionally check Authenticode on `WindowsBox.App.exe` / `wbox.exe`.
5. Double-click `WindowsBox.App.exe` (or run `wbox.exe` for the CLI).

See also `docs/network-safeguards.md` and `docs/signing.md`.
