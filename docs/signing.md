# Code signing (offline, no phone-home)

Windows Box ships **signed** portable ZIP / MSIX builds. The app itself never
phones home; the signing pipeline must also avoid network calls at runtime
(signing happens only on the release builder).

## What Ron must supply

| Secret / input | Where it is used | Notes |
|-|-|-|
| `WINDOWSBOX_CERT_PFX` | GitHub Actions secret (base64 PFX) **or** local file path | Code-signing certificate with private key. Placeholder in CI is empty — builds produce **unsigned** artifacts until set. |
| `WINDOWSBOX_CERT_PASSWORD` | GitHub Actions secret | PFX password. Leave empty for passwordless test certs. |
| Timestamp URL (optional) | `WINDOWSBOX_TIMESTAMP_URL` | Default `http://timestamp.digicert.com`. Omit for fully offline signing (no timestamp). |
| Publisher identity | `WINDOWSBOX_PUBLISHER` | e.g. `CN=Windows Box`. Must match the cert subject for MSIX. |

### SignPath OSS or Azure Trusted Signing

Ron still chooses the long-term provider (open question in `docs/MASTER-PLAN.md`):

- **SignPath** (free for OSS): replace the local `signtool` step with SignPath’s GitHub Action; keep the same artifact layout.
- **Azure Trusted Signing**: replace PFX secrets with Azure credentials; still no runtime telemetry from the app.

Until then, the placeholder pipeline signs with `signtool` when secrets exist and clearly labels unsigned builds when they do not.

## Local offline sign

```bash
# Produce artifacts (no network required after restore)
dotnet publish src/WindowsBox.Cli/WindowsBox.Cli.csproj -c Release -r win-x64 -o artifacts/wbox
dotnet publish src/WindowsBox.Elevator/WindowsBox.Elevator.csproj -c Release -r win-x64 -o artifacts/elevator

# Sign when Ron’s PFX is present (example)
pwsh ./build/sign.ps1 -CertPath $env:WINDOWSBOX_CERT_PFX_PATH -CertPassword $env:WINDOWSBOX_CERT_PASSWORD
```

`build/sign.ps1` refuses to upload anywhere; it only invokes `signtool` locally.

## Verification

```bash
signtool verify /pa artifacts/wbox/wbox.exe
```

Unsigned CI artifacts print `UNSIGNED — supply WINDOWSBOX_CERT_PFX to enable signing` in the workflow summary.
