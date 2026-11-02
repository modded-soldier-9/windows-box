<#
.SYNOPSIS
  Offline Authenticode signing for Windows Box artifacts.

.DESCRIPTION
  Signs binaries with a local PFX. Never uploads, never phones home.
  If -CertPath is missing, exits 0 after printing UNSIGNED guidance
  (so CI can still produce reviewable artifacts).
#>
param(
    [string]$CertPath = $env:WINDOWSBOX_CERT_PFX_PATH,
    [string]$CertPassword = $env:WINDOWSBOX_CERT_PASSWORD,
    [string]$TimestampUrl = $(if ($env:WINDOWSBOX_TIMESTAMP_URL) { $env:WINDOWSBOX_TIMESTAMP_URL } else { "" }),
    [string[]]$Paths = @(
        "artifacts/wbox/wbox.exe",
        "artifacts/elevator/WindowsBox.Elevator.exe"
    )
)

$ErrorActionPreference = "Stop"

if (-not $CertPath -or -not (Test-Path $CertPath)) {
    Write-Host "UNSIGNED — supply WINDOWSBOX_CERT_PFX_PATH (and optional WINDOWSBOX_CERT_PASSWORD)."
    Write-Host "See docs/signing.md for what Ron must provide (SignPath or Azure Trusted Signing later)."
    exit 0
}

$signtool = Get-Command signtool.exe -ErrorAction SilentlyContinue
if (-not $signtool) {
    $kits = "${env:ProgramFiles(x86)}\Windows Kits\10\bin"
    $signtool = Get-ChildItem -Path $kits -Recurse -Filter signtool.exe -ErrorAction SilentlyContinue |
        Sort-Object FullName -Descending |
        Select-Object -First 1
    if (-not $signtool) {
        throw "signtool.exe not found. Install Windows SDK signing tools."
    }
    $signtoolPath = $signtool.FullName
} else {
    $signtoolPath = $signtool.Source
}

foreach ($path in $Paths) {
    if (-not (Test-Path $path)) {
        Write-Warning "Skip missing artifact: $path"
        continue
    }

    $args = @("sign", "/fd", "SHA256", "/f", $CertPath)
    if ($CertPassword) {
        $args += @("/p", $CertPassword)
    }
    if ($TimestampUrl) {
        $args += @("/tr", $TimestampUrl, "/td", "SHA256")
    }
    $args += $path

    Write-Host "Signing $path"
    & $signtoolPath @args
    if ($LASTEXITCODE -ne 0) {
        throw "signtool failed for $path with exit $LASTEXITCODE"
    }
}

Write-Host "Signing complete (offline)."
