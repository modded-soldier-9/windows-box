# Quick start

Windows Box is offline-first. You do not need to enable updates, crash upload,
telemetry, or plugins to use it.

## Build and test (any OS)

```bash
dotnet test tests/WindowsBox.Tests/WindowsBox.Tests.csproj
dotnet run --project src/WindowsBox.Cli -- catalog validate --catalog catalog/services
dotnet run --project src/WindowsBox.Cli -- version
```

## CLI on Windows (safe first steps)

Never apply against a machine you care about without a dry-run and journal.

```powershell
wbox preview --profile balanced
wbox dry-run --profile privacy
wbox verify --quiet
wbox uninstall --plan
```

Apply (elevates once per batch via Elevator, then exits):

```powershell
wbox apply --profile balanced --confirm
wbox journals
wbox rollback <journal-id>
```

## App (WinUI)

1. Open Windows Box.
2. Use **Services** → Preview diff before applying.
3. Open **Settings** — leave network toggles **off** unless you intentionally opt in.
4. **History** shows journaled batches you can roll back.

## Network features (all optional)

| Setting | Default | Doc |
|-|-|-|
| Check for updates | Off / Manual | [network-safeguards.md](network-safeguards.md) |
| Crash dump upload | Off | [crash-reporting.md](crash-reporting.md) |
| Telemetry | Off | [network-safeguards.md](network-safeguards.md) |

A signed offline build works with every row above left at the default.

## Uninstall

```powershell
wbox uninstall --plan
# or elevated:
.\build\uninstall.ps1
.\build\uninstall.ps1 -RemoveProgramData
```

Removes `\WindowsBox\Verify` and known registry keys. See the script header for details.

## Next reading

- [ARCHITECTURE.md](../ARCHITECTURE.md) — engine layers
- [MASTER-PLAN.md](MASTER-PLAN.md) — roadmap source of truth
- [trust-guide.md](trust-guide.md) — one-line installer inspection (`install.ps1`)
