[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$testDirectory = Join-Path ([IO.Path]::GetTempPath()) ('EmpireAtWar-TestGate-' + [guid]::NewGuid())
$reportPath = Join-Path $testDirectory 'results.xml'
$projectCase = '<test-case fullname="EmpireAtWar.Tests.SampleTests.Passes" result="Passed" />'
$packageCase = '<test-case fullname="Unity.Package.Tests.SampleTests.Passes" result="Passed" />'
$projectError = 'EditMode gate requires at least one passed project test case'
$aggregateError = 'EditMode gate requires a passing NUnit report'
$provenanceError = 'EditMode results do not belong to this snapshot'
$cases = @(
    @{ Name='package-only report'; Body=$packageCase; Error=$projectError }
    @{ Name='skipped project test'; Body=$packageCase + '<test-case fullname="EmpireAtWar.Tests.SampleTests.Skipped" result="Skipped" />'; Error=$projectError }
    @{ Name='inconclusive project test'; Body=$packageCase + '<test-case fullname="EmpireAtWar.Tests.SampleTests.Inconclusive" result="Inconclusive" />'; Error=$projectError }
    @{ Name='project suite without project cases'; Body='<test-suite fullname="EmpireAtWar.Tests.SampleTests" result="Passed">' + $packageCase + '</test-suite>'; Error=$projectError }
    @{ Name='similar namespace'; Body='<test-case fullname="EmpireAtWar.TestsOther.SampleTests.Passes" result="Passed" />'; Error=$projectError }
    @{ Name='missing case fullname'; Body='<test-case name="EmpireAtWar.Tests.SampleTests.Passes" result="Passed" />'; Error=$projectError }
    @{ Name='project-only report'; Body=$projectCase }
    @{ Name='mixed nested report'; Body='<test-suite type="Assembly"><test-suite type="TestFixture">' + $packageCase + $projectCase + '</test-suite></test-suite>' }
    @{ Name='failed report'; Body=$projectCase; Result='Failed'; Failed=1; Error=$aggregateError }
    @{ Name='zero passed tests'; Body=''; Passed=0; Error=$aggregateError }
    @{ Name='failed count despite passed status'; Body=$projectCase; Failed=1; Error=$aggregateError }
    @{ Name='missing test-run'; Xml='<test-results />'; Error=$aggregateError }
    @{ Name='matching provenance'; Body=$projectCase; Provenance=$true }
    @{ Name='wrong commit'; Body=$projectCase; Provenance=$true; Commit='other'; Error=$provenanceError }
    @{ Name='wrong build'; Body=$projectCase; Provenance=$true; Build='2'; Error=$provenanceError }
    @{ Name='changed report'; Body=$projectCase; Provenance=$true; Hash='incorrect'; Error=$provenanceError }
    @{ Name='incomplete provenance arguments'; Body=$projectCase; CommitOnly=$true; Error='Test provenance requires both commit and Jenkins build number.' }
)
New-Item -ItemType Directory -Path $testDirectory | Out-Null
try {
    foreach ($case in $cases) {
        $result = 'Passed'
        $passed = 1
        $failed = 0
        if ($case.ContainsKey('Result')) { $result = $case.Result }
        if ($case.ContainsKey('Passed')) { $passed = $case.Passed }
        if ($case.ContainsKey('Failed')) { $failed = $case.Failed }
        $xml = "<test-run result='$result' passed='$passed' failed='$failed'>$($case.Body)</test-run>"
        if ($case.ContainsKey('Xml')) { $xml = $case.Xml }
        Set-Content -LiteralPath $reportPath -Value $xml -Encoding UTF8
        $arguments = @{ Path=$reportPath }
        if ($case.Provenance) {
            $provenance = @{ Commit='snapshot'; Build='1'; ReportSHA256=(Get-FileHash -LiteralPath $reportPath -Algorithm SHA256).Hash }
            if ($case.ContainsKey('Commit')) { $provenance.Commit = $case.Commit }
            if ($case.ContainsKey('Build')) { $provenance.Build = $case.Build }
            if ($case.ContainsKey('Hash')) { $provenance.ReportSHA256 = $case.Hash }
            $provenance | ConvertTo-Json | Set-Content -LiteralPath "$reportPath.provenance.json" -Encoding UTF8
            $arguments.Commit = 'snapshot'
            $arguments.Build = '1'
        }
        if ($case.CommitOnly) { $arguments.Commit = 'snapshot' }
        $failure = $null
        try { & "$PSScriptRoot\Assert-TestResults.ps1" @arguments } catch { $failure = $_.Exception.Message }
        if ($case.Error) {
            if (-not $failure -or -not $failure.StartsWith($case.Error)) {
                throw "$($case.Name): expected '$($case.Error)', received '$failure'."
            }
        } elseif ($failure) {
            throw "$($case.Name): unexpected failure: $failure"
        }
        Write-Host "PASS: $($case.Name)"
    }
    Write-Host "$($cases.Count) CI test gate checks passed."
} finally {
    Remove-Item -LiteralPath $reportPath -Force
    if (Test-Path -LiteralPath "$reportPath.provenance.json") { Remove-Item -LiteralPath "$reportPath.provenance.json" -Force }
    Remove-Item -LiteralPath $testDirectory
}
