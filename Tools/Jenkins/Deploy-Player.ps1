[CmdletBinding()]
param(
    # Jenkins build number; defaults to the job's last successful build.
    [int]$Build,
    [string]$Destination = 'C:\Users\golin\Desktop\EmpireAtWar',
    [switch]$Launch
)
$ErrorActionPreference = 'Stop'
$job = 'F:\Jenkins\data\jobs\EmpireAtWar-Windows-Local\builds'
if (-not $Build) {
    $line = Get-Content -LiteralPath "$job\permalinks" | Where-Object { $_ -match '^lastSuccessfulBuild (\d+)$' }
    if (-not $line) { throw 'Jenkins has no successful EmpireAtWar-Windows-Local build.' }
    $Build = [int]($line -replace '^lastSuccessfulBuild ', '')
}
$artifacts = "$job\$Build\archive\artifacts"
$zip = "$artifacts\EmpireAtWar-Windows.zip"
if (-not (Test-Path -LiteralPath $zip -PathType Leaf)) { throw "Build $Build has no archived player ZIP." }
$buildInfo = Get-Content -LiteralPath "$artifacts\build-info.json" -Raw | ConvertFrom-Json
if ($buildInfo.build -ne [string]$Build) { throw "Archived build-info.json belongs to build $($buildInfo.build), not $Build." }
if (Test-Path -LiteralPath "$artifacts\zip-sha256.json") {
    $expected = (Get-Content -LiteralPath "$artifacts\zip-sha256.json" -Raw | ConvertFrom-Json).Hash
    if ((Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash -ne $expected) { throw "Build $Build ZIP does not match its recorded SHA-256." }
}

# The game goes into its own folder; the destination may hold unrelated files.
$game = Join-Path $Destination 'Game'
$marker = 'eaw-deployment.json'
$running = @(Get-Process -Name EmpireAtWar -ErrorAction SilentlyContinue | Where-Object { $_.Path -and $_.Path.StartsWith($game, [StringComparison]::OrdinalIgnoreCase) })
if ($running.Count) { throw "Close the running game before deploying (PID $($running[0].Id))." }
if ((Test-Path -LiteralPath $game) -and -not (Test-Path -LiteralPath "$game\$marker" -PathType Leaf)) {
    throw "$game exists but was not created by this script; move it away first."
}

New-Item -ItemType Directory -Path $Destination -Force | Out-Null
$staging = Join-Path $Destination ".Game-staging-$Build"
if (Test-Path -LiteralPath $staging) { Remove-Item -LiteralPath $staging -Recurse -Force }
Write-Host "Extracting build $Build ($([math]::Round((Get-Item -LiteralPath $zip).Length / 1GB, 2)) GiB)..."
Add-Type -AssemblyName System.IO.Compression.FileSystem
[IO.Compression.ZipFile]::ExtractToDirectory($zip, $staging)
if (-not (Test-Path -LiteralPath "$staging\EmpireAtWar.exe" -PathType Leaf)) { throw 'Extracted player has no EmpireAtWar.exe.' }
@{ Build = $buildInfo.build; Version = $buildInfo.version; Commit = $buildInfo.commit; DeployedAtUtc = [DateTime]::UtcNow.ToString('o') } |
    ConvertTo-Json | Set-Content -LiteralPath "$staging\$marker" -Encoding UTF8

if (Test-Path -LiteralPath $game) { Remove-Item -LiteralPath $game -Recurse -Force }
Move-Item -LiteralPath $staging -Destination $game
Write-Host "Deployed v$($buildInfo.version) build $($buildInfo.build) ($($buildInfo.commit.Substring(0, 7))) to $game"
if ($Launch) { Start-Process -FilePath "$game\EmpireAtWar.exe" -WorkingDirectory $game }
