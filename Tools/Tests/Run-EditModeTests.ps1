<#
.SYNOPSIS
    Runs Unity EditMode tests through the Unity CLI without hanging.

.DESCRIPTION
    The one supported way for agents to run tests in this checkout.
    1. Waits until the Editor is ready: not compiling, no domain reload, Play Mode stopped, no test run active.
    2. Refuses dirty or untitled scenes (they trigger save popups).
    3. Starts each pass with --async-tests true and polls the parsed `status` field.
       PIPELINE_UNREACHABLE or non-JSON output means a domain reload is in progress: keep waiting.
    4. Stops after -TimeoutMinutes: cancels the run once and leaves Play Mode.
    5. Prints failed test names and messages, and leaves the Editor out of Play Mode.

    With no filter, runs two passes:
      - standard EditMode run (explicit tests excluded);
      - EditorPlayMode category (explicit tests that enter Play Mode and reload the domain).

.EXAMPLE
    pwsh -File Tools/Tests/Run-EditModeTests.ps1
.EXAMPLE
    pwsh -File Tools/Tests/Run-EditModeTests.ps1 -Category Ships,Health
.EXAMPLE
    pwsh -File Tools/Tests/Run-EditModeTests.ps1 -TestName HealthOverlayAnchorTests

.NOTES
    Exit codes: 0 all passed, 1 test failures, 2 Editor/infrastructure problem (nothing trustworthy ran).
#>
[CmdletBinding()]
param(
    # Exact categories; one pass each.
    [string[]]$Category = @(),
    # Test name filter (case-insensitive partial match); one pass.
    [string]$TestName = '',
    # Skip the EditorPlayMode pass of an unfiltered run.
    [switch]$SkipPlayModePass,
    # Per-pass limit; AGENTS.md hung-run limit is ~20 minutes.
    [int]$TimeoutMinutes = 20,
    [int]$PollSeconds = 5,
    [int]$ReadyTimeoutMinutes = 5
)

$ErrorActionPreference = 'Stop'
$PLAY_MODE_CATEGORY = 'EditorPlayMode'
$TERMINAL_STATES = @('completed', 'error', 'cancelled', 'canceled', 'failed', 'aborted')

function Invoke-Unity {
    param([string[]]$Arguments)
    $output = & unity @Arguments '--json' 2>&1 | Out-String
    $json = $null
    $firstLine = ($output -split "`r?`n" | Where-Object { $_.Trim() } | Select-Object -First 1)
    foreach ($candidate in @($output, $firstLine)) {
        if ($null -eq $candidate) { continue }
        try { $json = $candidate | ConvertFrom-Json; break } catch { }
    }
    [pscustomobject]@{ ExitCode = $LASTEXITCODE; Text = $output.Trim(); Json = $json }
}

function Test-Unreachable {
    param($Result)
    # A Pipeline error envelope (PIPELINE_UNREACHABLE, connection closed) or unparsable text.
    return ($null -eq $Result.Json) -or ($null -ne $Result.Json.PSObject.Properties['errors'] -and $Result.Json.errors)
}

function Get-EditorStatus {
    $result = Invoke-Unity @('command', 'editor', 'status')
    if (Test-Unreachable $result) { return $null }
    return $result.Json
}

function Wait-EditorReady {
    $deadline = (Get-Date).AddMinutes($ReadyTimeoutMinutes)
    while ((Get-Date) -lt $deadline) {
        $status = Get-EditorStatus
        if ($null -ne $status -and $status.status -eq 'ready' -and -not $status.compiling -and
            -not $status.domainReloadInProgress) {
            if ($status.playMode -ne 'stopped') {
                throw "The Editor is in Play Mode ($($status.playMode)). Stop it before running tests; this script never stops a session it did not start."
            }
            return
        }
        Start-Sleep -Seconds $PollSeconds
    }
    throw "The Editor did not become ready (not compiling, no domain reload) within $ReadyTimeoutMinutes min."
}

function Get-TestStatus {
    param([string]$Detail = 'summary')
    $result = Invoke-Unity @('command', 'test', 'status', '--detail', $Detail)
    if (Test-Unreachable $result) { return $null }
    return $result.Json
}

function Assert-ScenesClean {
    $result = Invoke-Unity @('command', 'scene', 'list_open')
    # One JSON object per open scene, one per line.
    try {
        $scenes = @($result.Text -split "`r?`n" | Where-Object { $_.Trim() } | ForEach-Object { $_ | ConvertFrom-Json })
    }
    catch { throw "Could not read open scenes: $($result.Text)" }
    if ($result.ExitCode -ne 0 -or $scenes.Count -eq 0 -or (Test-Unreachable ([pscustomobject]@{ Json = $scenes[0] }))) {
        throw "Could not read open scenes: $($result.Text)"
    }
    $blocked = @($scenes | Where-Object { $_.isDirty -or [string]::IsNullOrEmpty($_.path) })
    if ($blocked.Count -gt 0) {
        $names = ($blocked | ForEach-Object { if ($_.path) { $_.path } else { "<untitled> $($_.name)" } }) -join ', '
        throw "Dirty or untitled scenes would open a save popup: $names. Save or discard them, then rerun."
    }
}

function Stop-TestPlayMode {
    $status = Get-EditorStatus
    if ($null -ne $status -and $status.playMode -ne 'stopped') {
        Write-Warning 'Tests left the Editor in Play Mode; stopping it.'
        Invoke-Unity @('command', 'editor', 'playmode', 'stop') | Out-Null
    }
}

function Invoke-TestPass {
    param([string]$Label, [string[]]$FilterArguments, [bool]$IncludeExplicit)

    Wait-EditorReady
    $active = Get-TestStatus
    if ($null -ne $active -and $active.status -notin $TERMINAL_STATES) {
        throw "A test run is already active (status '$($active.status)'). Wait for it or run 'unity command test cancel'."
    }
    Assert-ScenesClean

    # Snapshot the previous run so a stale 'completed' status is not mistaken for this run's result.
    $previous = if ($null -ne $active) { $active | ConvertTo-Json -Compress -Depth 5 } else { '' }

    Write-Host "[$Label] starting (explicit: $IncludeExplicit)"
    $arguments = @('command', 'test', 'run', '--mode', 'editor', '--async-tests', 'true',
        '--include-explicit', $IncludeExplicit.ToString().ToLowerInvariant()) + $FilterArguments
    $start = Invoke-Unity $arguments
    if ($start.ExitCode -ne 0) { throw "[$Label] test run did not start (exit $($start.ExitCode)): $($start.Text)" }

    $timer = [Diagnostics.Stopwatch]::StartNew()
    $deadline = [TimeSpan]::FromMinutes($TimeoutMinutes)
    $lastState = ''
    while ($true) {
        Start-Sleep -Seconds $PollSeconds
        $status = Get-TestStatus
        if ($null -eq $status) {
            $state = 'unreachable (domain reload)'
        }
        else {
            $state = [string]$status.status
            $current = $status | ConvertTo-Json -Compress -Depth 5
            $isStale = $current -eq $previous -and $timer.Elapsed.TotalSeconds -lt 60
            if ($state -in $TERMINAL_STATES -and -not $isStale) { break }
        }
        if ($state -ne $lastState) {
            Write-Host ("[{0}] {1:mm\:ss} {2}" -f $Label, $timer.Elapsed, $state)
            $lastState = $state
        }
        if ($timer.Elapsed -gt $deadline) {
            Write-Warning "[$Label] exceeded $TimeoutMinutes min; cancelling once."
            Invoke-Unity @('command', 'test', 'cancel') | Out-Null
            Stop-TestPlayMode
            throw "[$Label] timed out. Inspect 'unity command console get' and Editor.log before another run."
        }
    }

    $final = Get-TestStatus -Detail 'failures'
    Stop-TestPlayMode
    if ($null -eq $final) { $final = $status }
    $summary = $final.summary
    Write-Host ("[{0}] {1} in {2:N0}s: total {3}, passed {4}, failed {5}, skipped {6}" -f
        $Label, $final.status, $final.duration, $summary.total, $summary.passed, $summary.failed, $summary.skipped)
    if ($final.status -ne 'completed') {
        throw "[$Label] ended with status '$($final.status)': $($final.message)"
    }
    foreach ($test in @($final.results | Where-Object { $_.status -eq 'Failed' })) {
        Write-Host "  FAILED $($test.fullName)" -ForegroundColor Red
        if ($test.message) { Write-Host ("    " + ($test.message.Trim() -replace "`r?`n", "`n    ")) }
    }
    return [int]$summary.failed
}

$passes = @()
if ($TestName) {
    $passes += , @("test:$TestName", @('--filter-type', 'TestName', '--filter', $TestName), $false)
}
foreach ($name in $Category) {
    # Exact category filter: explicit tests in that category run too (AGENTS.md Test Selection).
    $includeExplicit = $name -eq $PLAY_MODE_CATEGORY
    $passes += , @("category:$name", @('--filter-type', 'Category', '--filter', $name), $includeExplicit)
}
if ($passes.Count -eq 0) {
    $passes += , @('editmode', @(), $false)
    if (-not $SkipPlayModePass) {
        $passes += , @("category:$PLAY_MODE_CATEGORY", @('--filter-type', 'Category', '--filter', $PLAY_MODE_CATEGORY), $true)
    }
}

$failed = 0
try {
    foreach ($pass in $passes) {
        $failed += Invoke-TestPass -Label $pass[0] -FilterArguments $pass[1] -IncludeExplicit $pass[2]
    }
}
catch {
    Write-Host "BLOCKED: $($_.Exception.Message)" -ForegroundColor Yellow
    exit 2
}

if ($failed -gt 0) {
    Write-Host "$failed test(s) failed." -ForegroundColor Red
    exit 1
}
Write-Host 'All tests passed.' -ForegroundColor Green
exit 0
