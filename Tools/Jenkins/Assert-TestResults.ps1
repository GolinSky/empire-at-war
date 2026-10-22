[CmdletBinding()]
param([Parameter(Mandatory)][string]$Path, [string]$Commit, [string]$Build)
$ErrorActionPreference = 'Stop'
[xml]$report = Get-Content -LiteralPath $Path -Raw
$run = $report.SelectSingleNode('/test-run')
if ($null -eq $run -or $run.result -ne 'Passed' -or [int]$run.passed -lt 1 -or [int]$run.failed -ne 0) {
    throw 'EditMode gate requires a passing NUnit report with at least one passed test and no failures.'
}
if ($null -eq $run.SelectSingleNode(".//test-case[starts-with(@fullname, 'EmpireAtWar.Tests.') and @result='Passed']")) {
    throw 'EditMode gate requires at least one passed project test case in EmpireAtWar.Tests.; package tests alone do not satisfy the gate.'
}
if ($PSBoundParameters.ContainsKey('Commit') -or $PSBoundParameters.ContainsKey('Build')) {
    if (-not $Commit -or -not $Build) { throw 'Test provenance requires both commit and Jenkins build number.' }
    $provenance = Get-Content -LiteralPath "$Path.provenance.json" -Raw | ConvertFrom-Json
    if ($provenance.Commit -ne $Commit -or $provenance.Build -ne $Build -or
        $provenance.ReportSHA256 -ne (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash) {
        throw 'EditMode results do not belong to this snapshot and Jenkins build, or the report changed.'
    }
}
