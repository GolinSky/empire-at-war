[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$testDirectory = Join-Path ([IO.Path]::GetTempPath()) ('EmpireAtWar-PackagePins-' + [guid]::NewGuid())
$packages = Join-Path $testDirectory 'Packages'
$commit = 'a' * 40
$url = "https://example.invalid/package.git#$commit"
$package = 'com.example.dependency'
$secondPackage = 'com.example.second'
$pinError = "Git package must pin a full commit: $package"
$lockError = "Git package lock differs from manifest: $package"
$cases = @(
    @{ Name='matching full commit' }
    @{ Name='matching package subdirectory'; Version="https://example.invalid/package.git?path=/Packages/Example#$commit" }
    @{ Name='registry dependency'; Version='1.2.3'; Source='registry' }
    @{ Name='tag rejected'; Version='https://example.invalid/package.git#v1.2.3'; Errors=@($pinError) }
    @{ Name='branch rejected'; Version='https://example.invalid/package.git#main'; Errors=@($pinError) }
    @{ Name='short commit rejected'; Version='https://example.invalid/package.git#aaaaaaa'; Errors=@($pinError) }
    @{ Name='missing revision rejected'; Version='https://example.invalid/package.git'; Errors=@($pinError) }
    @{ Name='different lock URL'; LockVersion="https://example.invalid/other.git#$commit"; Errors=@($lockError) }
    @{ Name='different lock hash'; Hash=('b' * 40); Errors=@($lockError) }
    @{ Name='missing lock entry'; MissingLock=$true; Errors=@("Package missing from lock file: $package") }
    @{ Name='external file dependency'; Version='file:../External'; Source='local'; Errors=@("External file package is not reproducible: $package") }
    @{ Name='all invalid packages reported'; Version='https://example.invalid/package.git#v1.2.3'; SecondVersion='https://example.invalid/second.git#main'; Errors=@($pinError, "Git package must pin a full commit: $secondPackage") }
)
New-Item -ItemType Directory -Path $packages | Out-Null
try {
    foreach ($case in $cases) {
        $version = $url
        $source = 'git'
        $hash = $commit
        if ($case.ContainsKey('Version')) { $version = $case.Version }
        if ($case.ContainsKey('Source')) { $source = $case.Source }
        if ($case.ContainsKey('Hash')) { $hash = $case.Hash }
        $lockVersion = $version
        if ($case.ContainsKey('LockVersion')) { $lockVersion = $case.LockVersion }
        $manifest = @{ dependencies=@{ $package=$version } }
        $lock = @{ dependencies=@{} }
        if (-not $case.MissingLock) {
            $lock.dependencies[$package] = @{ version=$lockVersion; source=$source; hash=$hash }
        }
        if ($case.ContainsKey('SecondVersion')) {
            $manifest.dependencies[$secondPackage] = $case.SecondVersion
            $lock.dependencies[$secondPackage] = @{ version=$case.SecondVersion; source='git'; hash=$commit }
        }
        $manifest | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath "$packages\manifest.json" -Encoding UTF8
        $lock | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath "$packages\packages-lock.json" -Encoding UTF8
        $failure = $null
        try { & "$PSScriptRoot\Assert-PackagePins.ps1" -ProjectPath $testDirectory } catch { $failure = $_.Exception.Message }
        if ($case.Errors) {
            foreach ($expected in $case.Errors) {
                if (-not $failure -or -not $failure.Contains($expected)) {
                    throw "$($case.Name): expected '$expected', received '$failure'."
                }
            }
        } elseif ($failure) {
            throw "$($case.Name): unexpected failure: $failure"
        }
        Write-Host "PASS: $($case.Name)"
    }
    & "$PSScriptRoot\Assert-PackagePins.ps1" -ProjectPath (Resolve-Path "$PSScriptRoot\..\..").Path
    Write-Host 'PASS: repository package pins'
    Write-Host "$($cases.Count + 1) package pin checks passed."
} finally {
    foreach ($name in @('manifest.json','packages-lock.json')) {
        $path = Join-Path $packages $name
        if (Test-Path -LiteralPath $path) { Remove-Item -LiteralPath $path -Force }
    }
    Remove-Item -LiteralPath $packages
    Remove-Item -LiteralPath $testDirectory
}
