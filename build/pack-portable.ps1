<#
.SYNOPSIS
  Stage and zip the portable Windows Box release layout.

.DESCRIPTION
  Merges published App + CLI + Elevator outputs (excluding PDBs), copies the
  service catalog and in-repo plugins, then writes WindowsBox-<tag>-win-x64.zip
  plus a sibling SHA256 checksum. The WinUI shell (WindowsBox.App.exe) is
  required — without it a downloaded release has nothing users can launch.
#>
param(
    [string]$Tag = 'portable',
    [string]$OutDir = 'artifacts/dist',
    [string]$StageDir = 'artifacts/portable',
    [string]$AppDir = 'artifacts/app',
    [string]$WboxDir = 'artifacts/wbox',
    [string]$ElevatorDir = 'artifacts/elevator',
    [string]$CatalogDir = 'catalog',
    [string]$PluginsDir = 'plugins'
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Copy-PublishTree([string]$Source, [string]$Destination) {
    if (-not (Test-Path -LiteralPath $Source)) {
        throw "Missing publish output: $Source"
    }
    New-Item -ItemType Directory -Force -Path $Destination | Out-Null
    Get-ChildItem -LiteralPath $Source -Force | ForEach-Object {
        if ($_.Extension -ieq '.pdb') {
            return
        }
        $target = Join-Path $Destination $_.Name
        if ($_.PSIsContainer) {
            Copy-Item -LiteralPath $_.FullName -Destination $target -Recurse -Force
            Get-ChildItem -LiteralPath $target -Recurse -Filter '*.pdb' -ErrorAction SilentlyContinue |
                Remove-Item -Force -ErrorAction SilentlyContinue
        } else {
            Copy-Item -LiteralPath $_.FullName -Destination $target -Force
        }
    }
}

if (Test-Path -LiteralPath $StageDir) {
    Remove-Item -LiteralPath $StageDir -Recurse -Force
}
New-Item -ItemType Directory -Force -Path $StageDir | Out-Null
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null

Copy-PublishTree -Source $AppDir -Destination $StageDir
Copy-PublishTree -Source $WboxDir -Destination $StageDir
Copy-PublishTree -Source $ElevatorDir -Destination $StageDir

$appExe = Join-Path $StageDir 'WindowsBox.App.exe'
$wboxExe = Join-Path $StageDir 'wbox.exe'
$elevatorExe = Join-Path $StageDir 'WindowsBox.Elevator.exe'
foreach ($required in @($appExe, $wboxExe, $elevatorExe)) {
    if (-not (Test-Path -LiteralPath $required)) {
        throw "Portable stage missing required binary: $required"
    }
}

if (-not (Test-Path -LiteralPath $CatalogDir)) {
    throw "Missing catalog directory: $CatalogDir"
}
Copy-Item -LiteralPath $CatalogDir -Destination (Join-Path $StageDir 'catalog') -Recurse -Force

if (Test-Path -LiteralPath $PluginsDir) {
    Copy-Item -LiteralPath $PluginsDir -Destination (Join-Path $StageDir 'plugins') -Recurse -Force
}

$servicesDir = Join-Path (Join-Path $StageDir 'catalog') 'services'
if (-not (Test-Path -LiteralPath $servicesDir)) {
    throw "Portable stage missing catalog/services under $StageDir"
}

$zipName = "WindowsBox-$Tag-win-x64.zip"
$zipPath = Join-Path $OutDir $zipName
if (Test-Path -LiteralPath $zipPath) {
    Remove-Item -LiteralPath $zipPath -Force
}

# Compress the *contents* of the stage so exes land at the ZIP root.
Compress-Archive -Path (Join-Path $StageDir '*') -DestinationPath $zipPath -Force

$hash = (Get-FileHash -Algorithm SHA256 -Path $zipPath).Hash.ToLowerInvariant()
Set-Content -Path "$zipPath.sha256" -Value "$hash  $zipName" -NoNewline
Write-Host "Packed $zipName sha256=$hash (includes WindowsBox.App.exe for launch)"
