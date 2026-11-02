<#
.SYNOPSIS
  Windows Box uninstaller — removes \WindowsBox\Verify and known registry keys.

.DESCRIPTION
  Never phones home. Does not download or execute remote code.
  Prefer: wbox uninstall   (when the CLI is on PATH)
  This script is the portable fallback shipped with releases.

.PARAMETER RemoveProgramData
  Also delete %ProgramData%\WindowsBox (journals, backups, config, crashes).

.PARAMETER WhatIf
  Show planned actions without changing the system.
#>
[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [switch]$RemoveProgramData
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$taskPath = '\WindowsBox\Verify'
$registryKeys = @(
    'HKCU:\Software\WindowsBox',
    'HKLM:\Software\WindowsBox',
    'HKLM:\Software\Microsoft\Windows\CurrentVersion\Uninstall\WindowsBox'
)
$programData = Join-Path $env:ProgramData 'WindowsBox'

Write-Host 'Windows Box uninstaller'
Write-Host "  Task: $taskPath"
Write-Host "  ProgramData: $programData"
Write-Host ''

if ($PSCmdlet.ShouldProcess($taskPath, 'schtasks /Delete')) {
    & schtasks.exe /Delete /TN 'WindowsBox\Verify' /F 2>$null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Removed scheduled task $taskPath"
    } else {
        Write-Host "Scheduled task $taskPath not present or could not be removed (exit $LASTEXITCODE)."
    }
}

foreach ($key in $registryKeys) {
    if (Test-Path -LiteralPath $key) {
        if ($PSCmdlet.ShouldProcess($key, 'Remove-Item')) {
            Remove-Item -LiteralPath $key -Recurse -Force
            Write-Host "Deleted registry key $key"
        }
    } else {
        Write-Host "Registry key absent: $key"
    }
}

if ($RemoveProgramData) {
    if (Test-Path -LiteralPath $programData) {
        if ($PSCmdlet.ShouldProcess($programData, 'Remove-Item')) {
            Remove-Item -LiteralPath $programData -Recurse -Force
            Write-Host "Removed $programData"
        }
    } else {
        Write-Host "ProgramData folder absent: $programData"
    }
} else {
    Write-Host "Left $programData in place (pass -RemoveProgramData to delete journals/backups)."
}

Write-Host 'Done. No network calls were made.'
