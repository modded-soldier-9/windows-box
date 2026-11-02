<#
.SYNOPSIS
  CI benchmark gate: if WinUI cold start > ~1s or idle memory > ~120 MB,
  flag a WPF fallback review (does not fail the build by default).

.DESCRIPTION
  Reads a JSON metrics file produced by a Windows harness:
    { "coldStartMs": 850, "idleMemoryMb": 95, "host": "win11-vm" }
  Writes a GitHub Job Summary section recommending WPF + WPF-UI when thresholds trip.
#>
param(
    [string]$MetricsPath = "artifacts/perf/winui-metrics.json",
    [int]$ColdStartMsLimit = 1000,
    [int]$IdleMemoryMbLimit = 120,
    [switch]$FailOnBreach
)

$ErrorActionPreference = "Stop"

function Write-Summary([string]$text) {
    if ($env:GITHUB_STEP_SUMMARY) {
        Add-Content -Path $env:GITHUB_STEP_SUMMARY -Value $text
    }
    Write-Host $text
}

if (-not (Test-Path $MetricsPath)) {
    Write-Summary "### WPF fallback gate"
    Write-Summary "No metrics file at ``$MetricsPath`` — skipped (run the Windows perf harness to enable)."
    exit 0
}

$m = Get-Content $MetricsPath -Raw | ConvertFrom-Json
$cold = [int]$m.coldStartMs
$idle = [int]$m.idleMemoryMb
$breachCold = $cold -gt $ColdStartMsLimit
$breachIdle = $idle -gt $IdleMemoryMbLimit

Write-Summary "### WPF fallback gate"
Write-Summary "- Cold start: **${cold} ms** (limit ${ColdStartMsLimit} ms)"
Write-Summary "- Idle memory: **${idle} MB** (limit ${IdleMemoryMbLimit} MB)"

if ($breachCold -or $breachIdle) {
    Write-Summary ""
    Write-Summary "> **Fallback review triggered.** Prefer evaluating WPF + WPF-UI per MASTER-PLAN."
    Write-Summary "> WinUI remains default until Ron accepts the fallback."
    New-Item -ItemType Directory -Force -Path "artifacts/perf" | Out-Null
    "fallback-review-required" | Set-Content "artifacts/perf/WPF_FALLBACK_REVIEW"
    if ($FailOnBreach) {
        exit 2
    }
    exit 0
}

Write-Summary "Within budget — no WPF fallback review required."
exit 0
