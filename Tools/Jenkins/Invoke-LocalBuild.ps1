[CmdletBinding()]
param([Parameter(Mandatory)][ValidateSet('Prepare','Build','Package','ArchiveMonitor','ArchiveComplete')][string]$Phase)
$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\BuildResources.ps1"
$workspace = 'F:\Jenkins\data\workspace\EmpireAtWar-Windows-Local'
if ([IO.Path]::GetFullPath($env:WORKSPACE) -ne $workspace) { throw 'Unexpected workspace; refusing build or cleanup.' }
Assert-PlainPath $workspace
if ($Phase -eq 'ArchiveMonitor') {
    while (-not (Test-Path -LiteralPath "$workspace\archive-finished")) {
        Write-ResourceSample -Stage 'archive'
        Start-Sleep -Seconds $policy.PollSeconds
    }
    return
}
if ($Phase -eq 'ArchiveComplete') {
    Assert-CiIdle
    if (Test-Path -LiteralPath "$workspace\archive-pending.json") {
        $pending = Get-Content -LiteralPath "$workspace\archive-pending.json" -Raw | ConvertFrom-Json
        if ($pending.State -eq 'PlayerReady' -and -not (Test-Path -LiteralPath "$workspace\artifacts\EmpireAtWar-Windows.zip")) {
            throw 'Player packaging failed. Preserve/recover the player before clearing archive-pending.json.'
        }
    }
    foreach ($path in @("$workspace\player","$workspace\artifacts\EmpireAtWar-Windows.zip")) {
        Assert-PlainPath $path
        if (Test-Path -LiteralPath $path) {
            if ((Get-Item -LiteralPath $path).PSIsContainer -and (Get-ChildItem -LiteralPath $path -Recurse -Force -Attributes ReparsePoint)) { throw 'Cleanup contains a reparse point.' }
            Remove-Item -LiteralPath $path -Recurse -Force
        }
    }
    if (Test-Path -LiteralPath "$workspace\archive-pending.json") { Remove-Item -LiteralPath "$workspace\archive-pending.json" }
    return
}
if ($Phase -eq 'Prepare') {
    Assert-CiIdle
    if (Test-Path -LiteralPath "$workspace\archive-pending.json") { throw 'Previous output has not been archived. Recover it before another run.' }
    if (Test-Path -LiteralPath "$workspace\archive-finished") { Remove-Item -LiteralPath "$workspace\archive-finished" }
    foreach ($folder in @('player','artifacts')) {
        $path = Join-Path $workspace $folder
        Assert-PlainPath $path
        if (Test-Path -LiteralPath $path) {
            if (Get-ChildItem -LiteralPath $path -Recurse -Force -Attributes ReparsePoint) { throw 'Workspace reparse point rejected.' }
            Remove-Item -LiteralPath $path -Recurse -Force
        }
        New-Item -ItemType Directory -Path $path | Out-Null
    }
    Copy-Item -LiteralPath "$PSScriptRoot\ResourcePolicy.json" -Destination "$workspace\artifacts\resource-policy.json"
    Copy-Item -LiteralPath 'F:\Jenkins\app\build-tools-commit.txt' -Destination "$workspace\artifacts\tools-commit.txt"
    try { Write-ResourceSample -Preflight -Stage 'prepare' }
    catch { $_.ToString() | Set-Content -LiteralPath "$workspace\artifacts\failure.txt"; throw }
}
Add-Type -Path "$PSScriptRoot\CiProcessGroup.cs"
$executable = "$env:SystemRoot\System32\WindowsPowerShell\v1.0\powershell.exe"
$arguments = '-NoProfile -NonInteractive -ExecutionPolicy Bypass -File "' + "$PSScriptRoot\Invoke-BuildPhase.ps1" + '" -Phase ' + $Phase
$group = [CiProcessGroup]::new($executable, $arguments, $workspace, "$workspace\artifacts\$Phase-process.log")
$timer = [Diagnostics.Stopwatch]::StartNew()
$peakWorkingSet = 0L
$peakPrivate = 0L
$maximumProcesses = 0
try {
    do {
        Write-ResourceSample -Stage $Phase
        $ids = $group.GetProcessIds()
        $processes = @()
        if ($ids.Count) { $processes = @(Get-Process -Id $ids -ErrorAction SilentlyContinue) }
        $workingSet = ($processes | Measure-Object WorkingSet64 -Sum).Sum
        $private = ($processes | Measure-Object PrivateMemorySize64 -Sum).Sum
        $peakWorkingSet = [math]::Max($peakWorkingSet,$workingSet)
        $peakPrivate = [math]::Max($peakPrivate,$private)
        $maximumProcesses = [math]::Max($maximumProcesses,$ids.Count)
        @{Utc=[DateTime]::UtcNow.ToString('o');Phase=$Phase;JobProcessIds=$ids;WorkingSetBytes=$workingSet;PrivateBytes=$private} | ConvertTo-Json -Compress | Add-Content -LiteralPath "$workspace\artifacts\processes.jsonl"
        if ($timer.Elapsed.TotalSeconds -gt $policy.BuildTimeoutSeconds) { throw 'CI process group exceeded its time limit.' }
        if ($group.HasExited) { break }
        Start-Sleep -Seconds $policy.PollSeconds
    } while ($true)
    if ($group.ExitCode -ne 0) { throw "$Phase failed: exit $($group.ExitCode). Read $Phase-process.log." }
} catch {
    $_.ToString() | Set-Content -LiteralPath "$workspace\artifacts\failure.txt"
    throw
} finally {
    $group.Dispose()
    @{Phase=$Phase;DurationSeconds=$timer.Elapsed.TotalSeconds;PeakWorkingSetGiB=$peakWorkingSet/1GB;PeakPrivateGiB=$peakPrivate/1GB;MaximumJobProcesses=$maximumProcesses} | ConvertTo-Json | Set-Content -LiteralPath "$workspace\artifacts\$Phase-measurements.json"
    Assert-CiIdle
}
