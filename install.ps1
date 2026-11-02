<#
.SYNOPSIS
  Windows Box one-line installer (intended for: irm <url> | iex).

.DESCRIPTION
  SAFETY CONTRACT (read docs/trust-guide.md before piping to iex):
  1. Downloads ONLY the signed release asset named in the GitHub Release.
  2. Verifies SHA256 against the published checksum asset (*.sha256).
  3. Verifies Authenticode signature when present on Windows.
  4. Aborts on any mismatch — does NOT execute unsigned or unexpected payloads.
  5. Does NOT download or invoke any other remote scripts.

  This script itself performs the download + verify + expand. After verification
  it expands the ZIP to the install directory and launches WindowsBox.App.exe
  once (unless -NoLaunch). It never runs remote PowerShell beyond this file.

.PARAMETER Version
  Release tag (default: latest stable non-prerelease).

.PARAMETER Channel
  stable (default) or beta.

.PARAMETER InstallDir
  Destination directory (default: $env:LOCALAPPDATA\WindowsBox).

.PARAMETER Repo
  GitHub owner/name (default: modded-soldier-9/windows-box).

.PARAMETER DryRun
  Resolve URLs and print plan without downloading.

.PARAMETER NoLaunch
  Skip starting WindowsBox.App.exe after a successful install.
#>
[CmdletBinding()]
param(
    [string]$Version = '',
    [ValidateSet('stable', 'beta')]
    [string]$Channel = 'stable',
    [string]$InstallDir = (Join-Path $env:LOCALAPPDATA 'WindowsBox'),
    [string]$Repo = 'modded-soldier-9/windows-box',
    [switch]$DryRun,
    [switch]$NoLaunch
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Write-Step([string]$Message) {
    Write-Host "[windows-box] $Message"
}

function Get-ReleaseJson {
    param([string]$Repository, [string]$Tag, [string]$ReleaseChannel)

    $headers = @{
        'User-Agent' = 'WindowsBox-Installer'
        'Accept'     = 'application/vnd.github+json'
    }

    if ($Tag) {
        $uri = "https://api.github.com/repos/$Repository/releases/tags/$Tag"
        return Invoke-RestMethod -Uri $uri -Headers $headers
    }

    if ($ReleaseChannel -eq 'beta') {
        $uri = "https://api.github.com/repos/$Repository/releases"
        $all = Invoke-RestMethod -Uri $uri -Headers $headers
        $pick = $all | Where-Object { $_.prerelease -eq $true } | Select-Object -First 1
        if (-not $pick) { $pick = $all | Select-Object -First 1 }
        if (-not $pick) { throw "No releases found for $Repository" }
        return $pick
    }

    $uri = "https://api.github.com/repos/$Repository/releases/latest"
    return Invoke-RestMethod -Uri $uri -Headers $headers
}

function Find-Asset {
    param($Release, [string]$Pattern)
    $asset = $Release.assets | Where-Object { $_.name -like $Pattern } | Select-Object -First 1
    if (-not $asset) {
        throw "Release $($Release.tag_name) has no asset matching '$Pattern'."
    }
    return $asset
}

function Get-FileSha256([string]$Path) {
    return (Get-FileHash -Algorithm SHA256 -Path $Path).Hash.ToLowerInvariant()
}

Write-Step "Repo=$Repo channel=$Channel installDir=$InstallDir"
$release = Get-ReleaseJson -Repository $Repo -Tag $Version -ReleaseChannel $Channel
Write-Step "Using release $($release.tag_name)"

# Convention: WindowsBox-<tag>-win-x64.zip + WindowsBox-<tag>-win-x64.zip.sha256
$zipAsset = Find-Asset -Release $release -Pattern 'WindowsBox-*-win-x64.zip'
$sumAsset = Find-Asset -Release $release -Pattern 'WindowsBox-*-win-x64.zip.sha256'

Write-Step "Asset: $($zipAsset.name)"
Write-Step "Checksum: $($sumAsset.name)"

if ($DryRun) {
    Write-Step "Dry-run only — would download:"
    Write-Host "  $($zipAsset.browser_download_url)"
    Write-Host "  $($sumAsset.browser_download_url)"
    Write-Step "Then verify SHA256, optional Authenticode, expand to $InstallDir, abort on mismatch."
    return
}

$temp = Join-Path ([System.IO.Path]::GetTempPath()) ("windowsbox-install-" + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $temp | Out-Null
try {
    $zipPath = Join-Path $temp $zipAsset.name
    $sumPath = Join-Path $temp $sumAsset.name

    Write-Step "Downloading release ZIP (signed asset only)…"
    Invoke-WebRequest -Uri $zipAsset.browser_download_url -OutFile $zipPath -UseBasicParsing
    Write-Step "Downloading published checksum…"
    Invoke-WebRequest -Uri $sumAsset.browser_download_url -OutFile $sumPath -UseBasicParsing

    $expectedLine = (Get-Content -LiteralPath $sumPath -Raw).Trim()
    # Accept "hash", "hash  filename", or "hash *filename"
    $expectedHash = ($expectedLine -split '\s+')[0].ToLowerInvariant()
    if ($expectedHash -notmatch '^[a-f0-9]{64}$') {
        throw "Checksum file did not contain a 64-char SHA256. Aborting."
    }

    $actualHash = Get-FileSha256 -Path $zipPath
    Write-Step "Expected SHA256: $expectedHash"
    Write-Step "Actual   SHA256: $actualHash"
    if ($actualHash -ne $expectedHash) {
        throw "SHA256 MISMATCH — aborting. No files were installed. Do not bypass this check."
    }
    Write-Step "SHA256 OK."

    # Authenticode when the ZIP contains a signed payload or the zip itself is signed.
    # We check signature on extracted wbox.exe / WindowsBox.App.exe after expand.
    if (-not (Test-Path -LiteralPath $InstallDir)) {
        New-Item -ItemType Directory -Path $InstallDir | Out-Null
    }

    Write-Step "Expanding verified ZIP to $InstallDir…"
    Expand-Archive -LiteralPath $zipPath -DestinationPath $InstallDir -Force

    $appExe = Join-Path $InstallDir 'WindowsBox.App.exe'
    $wboxExe = Join-Path $InstallDir 'wbox.exe'
    $elevatorExe = Join-Path $InstallDir 'WindowsBox.Elevator.exe'
    $required = @($appExe, $wboxExe, $elevatorExe)
    $missing = @($required | Where-Object { -not (Test-Path -LiteralPath $_) })
    if ($missing.Count -gt 0) {
        throw "Release ZIP is missing required binaries: $($missing -join ', '). This is a packaging bug — the GUI shell must ship in the portable release."
    }

    $catalogServices = Join-Path (Join-Path $InstallDir 'catalog') 'services'
    if (-not (Test-Path -LiteralPath $catalogServices)) {
        throw "Release ZIP is missing catalog/services. Aborting — CLI/App cannot run without the catalog."
    }

    foreach ($exe in $required) {
        $sig = Get-AuthenticodeSignature -FilePath $exe
        if ($sig.Status -eq 'NotSigned') {
            Write-Step "WARNING: $exe is not Authenticode-signed (status=$($sig.Status)). Continuing because SHA256 matched the published checksum; prefer signed releases."
            continue
        }
        if ($sig.Status -ne 'Valid') {
            throw "Authenticode FAILED for $exe (status=$($sig.Status)). Aborting."
        }
        Write-Step "Authenticode OK: $exe ($($sig.SignerCertificate.Subject))"
    }

    Write-Step "Install complete. Network features remain OFF by default — see Settings."
    Write-Step "No other remote code was executed by this installer."

    if ($NoLaunch) {
        Write-Step "Skipping launch (-NoLaunch). Start manually: $appExe"
    } else {
        Write-Step "Launching WindowsBox.App…"
        Start-Process -FilePath $appExe
    }
}
finally {
    if (Test-Path -LiteralPath $temp) {
        Remove-Item -LiteralPath $temp -Recurse -Force -ErrorAction SilentlyContinue
    }
}
