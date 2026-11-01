# VM test plan — apply / undo round-trips

Goal: prove that for each supported Windows 11 build, **apply → undo** restores the machine to the measured pre-state (not to assumed catalog defaults), and harvest live service defaults for the catalog.

## Matrix

| Edition | 23H2 (22631) | 24H2 (26100) | 25H2 (when available) |
|---------|--------------|--------------|------------------------|
| Home    | required     | required     | required               |
| Pro     | required     | required     | required               |
| Ent     | optional     | required     | required               |

Use clean installs (or differencing disks from a golden image). Hyper-V or Windows Sandbox for smoke; full matrix on Hyper-V Gen2 VMs with checkpoints.

## Host prerequisites

- Windows 11 Pro/Ent host with Hyper-V (or nested virt)
- `wbox` CLI build from the PR under test
- Checkpoints / snapshots enabled before each run
- No third-party “optimizer” preinstalled

## Per-build procedure

### 0. Checkpoint

```powershell
Checkpoint-VM -Name $vm -SnapshotName "pre-wbox"
```

### 1. Harvest defaults (feeds catalog `defaults:`)

```powershell
# Inside the guest, elevated
wbox catalog list --catalog C:\path\to\catalog\services
Get-Service | Select-Object Name, StartType, Status |
  Export-Csv C:\wbox-out\services-defaults.csv -NoTypeInformation
sc.exe query type= service state= all > C:\wbox-out\sc-query.txt
# For each catalogued name:
sc.exe qc <Name> >> C:\wbox-out\sc-qc.txt
```

Attach the CSV/`sc qc` dump to the PR or `docs/research/harness/` when verifying a new build. **Do not ship** unverified defaults.

### 2. Dry run

```powershell
wbox dry-run --profile balanced
wbox dry-run --profile privacy
wbox dry-run --profile gamer
```

Expect a preview diff only; no SCM changes. Confirm protected / `touch_policy: never|leave|policy_only` items are skipped.

### 3. Apply (journaled)

```powershell
wbox apply --profile privacy
# Record journal id from output
$jid = "<journal-id>"
```

Verify:

- Journal file exists under `%ProgramData%\WindowsBox\journals\<id>.json`
- Each entry has `priorStartType` matching the pre-apply `sc qc` value
- `registryExport` is non-empty for each changed service
- Target start types match the preview
- No Windows Box process remains after the Elevator exits (`Get-Process wbox,WindowsBox*`)

### 4. Health check

```powershell
wbox health   # M1+; until then use:
Get-Service RpcSs,BFE,mpssvc,EventLog,PlugPlay,Power
Test-NetConnection example.com -Port 443
# Audio endpoint present, Start menu opens, Settings → Windows Update page loads
```

Fail the run if networking, audio, or Update UI is broken.

### 5. Undo round-trip

```powershell
wbox rollback $jid
```

For every journalled service:

```powershell
$before = Import-Csv C:\wbox-out\pre-apply.csv
$after  = Get-Service | Select Name, StartType
# priorStartType in journal must equal live StartType after rollback
```

**Pass criteria:** live start type, delayed autostart flag, and trigger presence match the journal priors bit-for-bit. Catalog defaults are irrelevant here.

### 6. Offline recovery kit (M1+)

Confirm `%ProgramData%\WindowsBox\backups\<id>\restore-services.reg` (and `.cmd`) recreate priors from WinRE/Safe Mode. Spot-check one service manually:

```cmd
reg import restore-services.reg
```

### 7. Drift simulation

```powershell
# Re-apply, then manually revert one service as Windows Update might:
sc.exe config DiagTrack start= auto
wbox preview --profile privacy
# Expect drift callout for DiagTrack (M4); until then, preview should show it as a change again
```

### 8. Restore checkpoint

```powershell
Restore-VMCheckpoint -VMName $vm -Name "pre-wbox"
```

## Profiles to exercise

| Profile   | Intent                                      | Watch-outs                          |
|-----------|---------------------------------------------|-------------------------------------|
| Safe      | risk ≤ 1 only                               | Near-empty diff is OK               |
| Balanced  | default recommendations                     | Manual preferred                    |
| Privacy   | DiagTrack/WER etc.                          | MDM guests must gate disables       |
| Gamer     | Xbox leave / SysMain honesty label          | Teredo / GameInput                  |
| Minimal   | low-RAM                                     | Search/Outlook breakage warnings    |
| Laptop    | battery-aware                               | BT, sensors, WLAN never-touch       |
| Developer | Hyper-V/WSL leave                           | Don't disable vmcompute/WSL         |
| Office    | printers, search                            | Spooler gates                       |

## Hardware / role gate fixtures

Maintain secondary VMs or toggle fixtures:

- No printers vs USB printer installed
- BT radio on/off (or airplane mode)
- Domain-joined / Intune-enrolled (advisory read-only expectations)
- Xbox app / Game Pass present
- WSL2 feature enabled

## CI harness (direction)

M0 ships unit tests with fakes (Linux + Windows agents) and catalog validation. The VM matrix above is **manual / scheduled** until an automated Hyper-V job exists. When automating:

1. Boot golden VHDX per cell
2. Copy `wbox` + catalog in
3. Run steps 1–5; upload journals + `sc qc` artifacts
4. Fail on rollback mismatch or health failure
5. Destroy VM

## Reporting template

```
Build: 26100.xxxx  Edition: Pro
Profile: privacy
Changes applied: N
Journal: <id>
Rollback match: PASS/FAIL
Health: PASS/FAIL
Defaults dump: attached / path
Notes:
```
