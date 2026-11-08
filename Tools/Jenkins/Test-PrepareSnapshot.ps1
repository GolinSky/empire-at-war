[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$testDirectory = Join-Path ([IO.Path]::GetTempPath()) ('EmpireAtWar-PrepareSnapshot-' + [guid]::NewGuid())
$relativeCsv = 'Docs/Audits/Fixture/frames.csv'
$csvPath = Join-Path $testDirectory $relativeCsv
$tokens = $null
$parseErrors = $null
$ast = [Management.Automation.Language.Parser]::ParseFile("$PSScriptRoot\Prepare-Snapshot.ps1", [ref]$tokens, [ref]$parseErrors)
if ($parseErrors.Count) { throw 'Prepare-Snapshot.ps1 has parse errors.' }
$gitFunction = $ast.Find({ param($node) $node -is [Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -eq 'Invoke-Git' }, $false)
if ($null -eq $gitFunction) { throw 'Prepare-Snapshot.ps1 has no Invoke-Git function.' }
# Load only the real Git wrapper; never execute CI preparation against the real worktree.
. ([scriptblock]::Create($gitFunction.Extent.Text))
New-Item -ItemType Directory -Path (Split-Path $csvPath) | Out-Null
try {
    Invoke-Git init --quiet $testDirectory
    Invoke-Git -C $testDirectory config core.autocrlf false
    $actualRoot = Invoke-Git -C $testDirectory rev-parse --show-toplevel
    if ([IO.Path]::GetFullPath($actualRoot) -ne [IO.Path]::GetFullPath($testDirectory)) {
        throw 'Invoke-Git did not preserve successful command output.'
    }
    Write-Host 'PASS: successful Git output is returned'

    [IO.File]::WriteAllText($csvPath, "Frame,Milliseconds`r`n1,16`r`n", [Text.UTF8Encoding]::new($false))
    Invoke-Git -C $testDirectory add -- $relativeCsv
    $originalTree = Invoke-Git -C $testDirectory write-tree
    Invoke-Git -C $testDirectory config core.autocrlf true
    Invoke-Git -C $testDirectory add --renormalize -u
    $log = [Collections.Generic.List[string]]::new()
    $failure = $null
    try {
        Invoke-Git -C $testDirectory diff --cached --exit-code $originalTree -- 6>&1 |
            ForEach-Object { $log.Add([string]$_) }
    } catch { $failure = $_.Exception.Message }
    if (-not $failure -or -not $failure.StartsWith('Git failed:')) {
        throw 'The original CRLF snapshot drift did not fail the integrity check.'
    }
    $loggedOutput = $log -join [Environment]::NewLine
    foreach ($expected in @("diff --git a/$relativeCsv b/$relativeCsv", '@@', '-Frame,Milliseconds', '+Frame,Milliseconds')) {
        if (-not $loggedOutput.Contains($expected)) { throw "Failed Git diff was not logged: $expected" }
    }
    Write-Host 'PASS: CRLF snapshot drift fails and the actual diff is logged'

    Copy-Item -LiteralPath "$PSScriptRoot\..\..\.gitattributes" -Destination "$testDirectory\.gitattributes"
    [IO.File]::WriteAllText($csvPath, "Frame,Milliseconds`n1,16`n", [Text.UTF8Encoding]::new($false))
    Invoke-Git -C $testDirectory add -- .gitattributes $relativeCsv
    $normalizedTree = Invoke-Git -C $testDirectory write-tree
    Invoke-Git -C $testDirectory add --renormalize -u
    Invoke-Git -C $testDirectory diff --cached --exit-code $normalizedTree --
    $attributes = Invoke-Git -C $testDirectory check-attr --all $relativeCsv
    if ($attributes -notcontains "${relativeCsv}: text: set" -or $attributes -notcontains "${relativeCsv}: eol: lf") {
        throw "Audit CSVs are not explicitly normalized to LF: $($attributes -join '; ')"
    }
    Write-Host 'PASS: repository CSV attributes keep snapshot normalization stable'
    Write-Host '3 snapshot preparation checks passed.'
} finally {
    $resolved = [IO.Path]::GetFullPath($testDirectory)
    $tempRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd('\') + '\'
    if (-not $resolved.StartsWith($tempRoot, [StringComparison]::OrdinalIgnoreCase) -or
        [IO.Path]::GetFileName($resolved) -notlike 'EmpireAtWar-PrepareSnapshot-*') { throw 'Unexpected fixture path.' }
    Remove-Item -LiteralPath $resolved -Recurse -Force
}
