<#
.SYNOPSIS
  Local Windows preview: publish App + CLI + Elevator, optional sign, pack ZIP.

.DESCRIPTION
  Prefer this over GitHub release tags while iterating. Produces artifacts under
  artifacts/app|wbox|elevator, stages into artifacts/portable, and zips to
  artifacts/dist. Fails hard if WindowsBox.App.exe is missing after publish or pack.

.EXAMPLE
  ./build/local-preview.ps1
  ./build/local-preview.ps1 -Launch
  ./build/local-preview.ps1 -Tag local-preview -Launch
#>
param(
    [string]$Tag = 'local-preview',
    [string]$Configuration = 'Release',
    [string]$Runtime = 'win-x64',
    [string]$Platform = 'x64',
    [switch]$Launch,
    [switch]$SkipSign
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

if (-not $IsWindows) {
    throw 'build/local-preview.ps1 must run on Windows (WinUI App publish requires Windows).'
}

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..')
Set-Location -LiteralPath $repoRoot

$appOut = Join-Path $repoRoot 'artifacts/app'
$wboxOut = Join-Path $repoRoot 'artifacts/wbox'
$elevatorOut = Join-Path $repoRoot 'artifacts/elevator'
$stageDir = Join-Path $repoRoot 'artifacts/portable'
$distDir = Join-Path $repoRoot 'artifacts/dist'

function Invoke-DotnetPublish {
    param(
        [Parameter(Mandatory)] [string]$Project,
        [Parameter(Mandatory)] [string]$Output,
        [switch]$WinUiPlatform
    )

    if (-not (Test-Path -LiteralPath $Project)) {
        throw "Missing project: $Project"
    }

    $publishArgs = @(
        $Project,
        '-c', $Configuration,
        '-r', $Runtime,
        '--self-contained', 'true',
        '-o', $Output
    )
    # WinUI XAML compile requires an explicit non-AnyCPU Platform (see WMC9999).
    if ($WinUiPlatform) {
        $publishArgs += @('-p:Platform=' + $Platform)
    }

    Write-Host "==> publish $Project -> $Output"
    & dotnet publish @publishArgs
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish failed for $Project (exit $LASTEXITCODE)"
    }
}

function Assert-File([string]$Path, [string]$Hint) {
    if (-not (Test-Path -LiteralPath $Path)) {
        throw "Missing required file: $Path. $Hint"
    }
}

Write-Host "Windows Box local preview (repo: $repoRoot)"
Write-Host "Tag=$Tag Configuration=$Configuration Runtime=$Runtime Platform=$Platform"

Invoke-DotnetPublish `
    -Project (Join-Path $repoRoot 'src/WindowsBox.App/WindowsBox.App.csproj') `
    -Output $appOut `
    -WinUiPlatform

$appExePublish = Join-Path $appOut 'WindowsBox.App.exe'
Assert-File $appExePublish 'App publish did not produce WindowsBox.App.exe — fix WinUI build before packing.'

Invoke-DotnetPublish `
    -Project (Join-Path $repoRoot 'src/WindowsBox.Cli/WindowsBox.Cli.csproj') `
    -Output $wboxOut

Invoke-DotnetPublish `
    -Project (Join-Path $repoRoot 'src/WindowsBox.Elevator/WindowsBox.Elevator.csproj') `
    -Output $elevatorOut

Assert-File (Join-Path $wboxOut 'wbox.exe') 'CLI publish missing wbox.exe.'
Assert-File (Join-Path $elevatorOut 'WindowsBox.Elevator.exe') 'Elevator publish missing WindowsBox.Elevator.exe.'

if (-not $SkipSign) {
    Write-Host '==> sign (skips gracefully when no cert)'
    & (Join-Path $repoRoot 'build/sign.ps1')
    if ($LASTEXITCODE -ne 0) {
        throw "build/sign.ps1 failed (exit $LASTEXITCODE)"
    }
} else {
    Write-Host '==> sign skipped (-SkipSign)'
}

Write-Host '==> pack portable ZIP'
& (Join-Path $repoRoot 'build/pack-portable.ps1') `
    -Tag $Tag `
    -OutDir $distDir `
    -StageDir $stageDir `
    -AppDir $appOut `
    -WboxDir $wboxOut `
    -ElevatorDir $elevatorOut `
    -CatalogDir (Join-Path $repoRoot 'catalog') `
    -PluginsDir (Join-Path $repoRoot 'plugins')
if ($LASTEXITCODE -ne 0) {
    throw "build/pack-portable.ps1 failed (exit $LASTEXITCODE)"
}

$appExeStage = Join-Path $stageDir 'WindowsBox.App.exe'
$zipPath = Join-Path $distDir "WindowsBox-$Tag-win-x64.zip"
Assert-File $appExeStage 'Pack stage is missing WindowsBox.App.exe.'
Assert-File $zipPath 'Pack did not write the portable ZIP.'
Assert-File "$zipPath.sha256" 'Pack did not write the SHA256 sidecar.'

Write-Host ''
Write-Host 'Local preview ready (test this on Windows before any GitHub release):'
Write-Host "  App (publish):  $appExePublish"
Write-Host "  App (staged):   $appExeStage"
Write-Host "  ZIP:            $zipPath"
Write-Host "  SHA256:         $zipPath.sha256"
Write-Host "  Stage dir:      $stageDir"
Write-Host ''

if ($Launch) {
    Write-Host "==> launching $appExeStage"
    Start-Process -FilePath $appExeStage -WorkingDirectory $stageDir
}
