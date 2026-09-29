[CmdletBinding()]
param([Parameter(Mandatory)][ValidateSet('Prepare','Build','Package')][string]$Phase)
$ErrorActionPreference = 'Stop'
try {
    if ($Phase -eq 'Prepare') { & "$PSScriptRoot\Prepare-Snapshot.ps1"; exit 0 }
    . "$PSScriptRoot\BuildResources.ps1"
    $ci = 'F:\UnityCI\empire-at-war'
    $player = "$env:WORKSPACE\player"
    $artifacts = "$env:WORKSPACE\artifacts"
    if ($Phase -eq 'Build') {
        Write-ResourceSample -Preflight -Stage 'build'
        Assert-CiIdle
        $sha = (Get-Content -LiteralPath "$artifacts\commit.txt").Trim()
        $head = (& git -C $ci rev-parse HEAD).Trim()
        if ($LASTEXITCODE -ne 0 -or $head -ne $sha) { throw 'Snapshot changed after preparation.' }
        $version = (Select-String -LiteralPath "$ci\ProjectSettings\ProjectVersion.txt" -Pattern '^m_EditorVersion: (.+)$').Matches[0].Groups[1].Value
        $editor = "F:\Program Files\Unity\Hub\Editor\$version\Editor\Unity.exe"
        if (-not (Test-Path -LiteralPath $editor)) { throw "Required Editor missing: $version" }
        if (Test-Path -LiteralPath "$player\EmpireAtWar.exe") { throw 'Old player output rejected.' }
        @{Commit=$sha;Build=$env:BUILD_NUMBER;State='BuildStarted'} | ConvertTo-Json | Set-Content -LiteralPath "$env:WORKSPACE\archive-pending.json"
        & unity build $ci --target StandaloneWindows64 --editor-path $editor --output-path "$player\EmpireAtWar.exe" --log-file "$artifacts\Unity.log" --provenance-path "$artifacts\build-provenance.json" --args '-job-worker-count 4' --timeout $policy.BuildTimeoutSeconds --non-interactive
        if ($LASTEXITCODE -ne 0) { throw "Unity build failed: exit $LASTEXITCODE" }
        foreach ($required in @('EmpireAtWar.exe','UnityPlayer.dll','EmpireAtWar_Data\globalgamemanagers','EmpireAtWar_Data\Managed\Assembly-CSharp.dll','EmpireAtWar_Data\StreamingAssets\aa\settings.json')) {
            if (-not (Test-Path -LiteralPath "$player\$required" -PathType Leaf)) { throw "Incomplete Mono player: $required" }
        }
        $bundles = @(Get-ChildItem -LiteralPath "$player\EmpireAtWar_Data\StreamingAssets\aa" -Recurse -Filter '*.bundle')
        if (-not $bundles.Count) { throw 'Addressables bundles are missing from player.' }
        if (-not (Test-Path -LiteralPath "$artifacts\build-provenance.json")) { throw 'CLI provenance is missing.' }
        @{LibraryGiB=(Get-DirectoryGiB "$ci\Library");PlayerGiB=(Get-DirectoryGiB $player);AddressablesBundles=$bundles.Count} | ConvertTo-Json | Set-Content -LiteralPath "$artifacts\output-sizes.json"
        @{Commit=$sha;Build=$env:BUILD_NUMBER;State='PlayerReady'} | ConvertTo-Json | Set-Content -LiteralPath "$env:WORKSPACE\archive-pending.json"
    } else {
        Write-ResourceSample -Stage 'package'
        & tar.exe -a -c -f "$artifacts\EmpireAtWar-Windows.zip" -C $player .
        if ($LASTEXITCODE -ne 0) { throw 'ZIP packaging failed.' }
        & tar.exe -t -f "$artifacts\EmpireAtWar-Windows.zip" > "$artifacts\zip-contents.txt"
        if ($LASTEXITCODE -ne 0) { throw 'ZIP inventory failed.' }
        Get-FileHash -LiteralPath "$artifacts\EmpireAtWar-Windows.zip" -Algorithm SHA256 | Select-Object Hash | ConvertTo-Json | Set-Content -LiteralPath "$artifacts\zip-sha256.json"
    }
    exit 0
} catch {
    Write-Error -Message $_ -ErrorAction Continue
    exit 1
}
