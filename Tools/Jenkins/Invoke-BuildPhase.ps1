[CmdletBinding()]
param([Parameter(Mandatory)][ValidateSet('Prepare','Test','Build','Package')][string]$Phase)
$ErrorActionPreference = 'Stop'
try {
    if ($Phase -eq 'Prepare') { & "$PSScriptRoot\Prepare-Snapshot.ps1"; exit 0 }
    . "$PSScriptRoot\BuildResources.ps1"
    $ci = 'F:\UnityCI\empire-at-war'
    $player = "$env:WORKSPACE\player"
    $artifacts = "$env:WORKSPACE\artifacts"
    if ($Phase -in @('Test','Build')) {
        Write-ResourceSample -Preflight -Stage $Phase
        Assert-CiIdle
        $sha = (Get-Content -LiteralPath "$artifacts\commit.txt").Trim()
        $head = (& git -C $ci rev-parse HEAD).Trim()
        if ($LASTEXITCODE -ne 0 -or $head -ne $sha) { throw 'Snapshot changed after preparation.' }
        $version = (Select-String -LiteralPath "$ci\ProjectSettings\ProjectVersion.txt" -Pattern '^m_EditorVersion: (.+)$').Matches[0].Groups[1].Value
        $editor = "F:\Program Files\Unity\Hub\Editor\$version\Editor\Unity.exe"
        if (-not (Test-Path -LiteralPath $editor)) { throw "Required Editor missing: $version" }
        if ($Phase -eq 'Test') {
            if (-not $env:BUILD_NUMBER) { throw 'Jenkins build number is required for test provenance.' }
            foreach ($previous in @("$artifacts\EditMode-results.xml", "$artifacts\EditMode-results.xml.provenance.json")) {
                if (Test-Path -LiteralPath $previous) { Remove-Item -LiteralPath $previous -Force }
            }
            # This launches a separate batch Editor on the clean, idle CI snapshot.
            # It never runs tests in the developer's open Editor or scenes.
            & unity test $ci --mode EditMode --editor-path $editor --output "$artifacts\EditMode-results.xml" --timeout $policy.BuildTimeoutSeconds --non-interactive
            if ($LASTEXITCODE -ne 0) { throw "EditMode tests failed: exit $LASTEXITCODE" }
            & "$PSScriptRoot\Assert-TestResults.ps1" -Path "$artifacts\EditMode-results.xml"
            @{
                Commit=$sha; Build=$env:BUILD_NUMBER
                ReportSHA256=(Get-FileHash -LiteralPath "$artifacts\EditMode-results.xml" -Algorithm SHA256).Hash
            } | ConvertTo-Json | Set-Content -LiteralPath "$artifacts\EditMode-results.xml.provenance.json" -Encoding UTF8
            exit 0
        }
        & "$PSScriptRoot\Assert-TestResults.ps1" -Path "$artifacts\EditMode-results.xml" -Commit $sha -Build $env:BUILD_NUMBER
        if (Test-Path -LiteralPath "$player\EmpireAtWar.exe") { throw 'Old player output rejected.' }
        # IL2CPP debug data from the previous run; kept beside, never inside, the shipped player.
        $symbols = "$env:WORKSPACE\player-symbols"
        Assert-PlainPath $symbols
        if (Test-Path -LiteralPath $symbols) { Remove-Item -LiteralPath $symbols -Recurse -Force }
        @{Commit=$sha;Build=$env:BUILD_NUMBER;State='BuildStarted'} | ConvertTo-Json | Set-Content -LiteralPath "$env:WORKSPACE\archive-pending.json"
        # BuildInfoWriter stamps this and BUILD_NUMBER into the player's build-info.json.
        $env:EAW_BUILD_COMMIT = $sha
        & unity build $ci --target StandaloneWindows64 --editor-path $editor --output-path "$player\EmpireAtWar.exe" --log-file "$artifacts\Unity.log" --provenance-path "$artifacts\build-provenance.json" --args '-job-worker-count 4' --timeout $policy.BuildTimeoutSeconds --non-interactive
        if ($LASTEXITCODE -ne 0) { throw "Unity build failed: exit $LASTEXITCODE" }
        foreach ($required in @('EmpireAtWar.exe','UnityPlayer.dll','GameAssembly.dll','EmpireAtWar_Data\globalgamemanagers','EmpireAtWar_Data\il2cpp_data\Metadata\global-metadata.dat','EmpireAtWar_Data\StreamingAssets\aa\settings.json','EmpireAtWar_Data\StreamingAssets\build-info.json')) {
            if (-not (Test-Path -LiteralPath "$player\$required" -PathType Leaf)) { throw "Incomplete IL2CPP player: $required" }
        }
        # A Managed folder means a Mono player; IL2CPP never compiles editor or test assemblies into the player.
        if (Test-Path -LiteralPath "$player\EmpireAtWar_Data\Managed") { throw 'Mono player output rejected; IL2CPP backend expected.' }
        $backup = "$player\EmpireAtWar_BackUpThisFolder_ButDontShipItWithYourGame"
        if (-not (Test-Path -LiteralPath $backup -PathType Container)) { throw 'IL2CPP debug folder is missing.' }
        Move-Item -LiteralPath $backup -Destination $symbols
        $bundles = @(Get-ChildItem -LiteralPath "$player\EmpireAtWar_Data\StreamingAssets\aa" -Recurse -Filter '*.bundle')
        if (-not $bundles.Count) { throw 'Addressables bundles are missing from player.' }
        if (-not (Test-Path -LiteralPath "$artifacts\build-provenance.json")) { throw 'CLI provenance is missing.' }
        $buildInfo = Get-Content -LiteralPath "$player\EmpireAtWar_Data\StreamingAssets\build-info.json" -Raw | ConvertFrom-Json
        if ($buildInfo.commit -ne $sha -or $buildInfo.build -ne $env:BUILD_NUMBER) { throw 'Player build-info.json does not match this commit and build number.' }
        Copy-Item -LiteralPath "$player\EmpireAtWar_Data\StreamingAssets\build-info.json" -Destination "$artifacts\build-info.json"
        @{LibraryGiB=(Get-DirectoryGiB "$ci\Library");PlayerGiB=(Get-DirectoryGiB $player);AddressablesBundles=$bundles.Count} | ConvertTo-Json | Set-Content -LiteralPath "$artifacts\output-sizes.json"
        @{Commit=$sha;Build=$env:BUILD_NUMBER;State='PlayerReady'} | ConvertTo-Json | Set-Content -LiteralPath "$env:WORKSPACE\archive-pending.json"
    } else {
        Write-ResourceSample -Stage 'package'
        Add-Type -AssemblyName System.IO.Compression.FileSystem
        $zipPath = "$artifacts\EmpireAtWar-Windows.zip"
        [IO.Compression.ZipFile]::CreateFromDirectory($player, $zipPath, [IO.Compression.CompressionLevel]::Optimal, $false)
        $zip = [IO.Compression.ZipFile]::OpenRead($zipPath)
        try {
            $zip.Entries.FullName | Set-Content -LiteralPath "$artifacts\zip-contents.txt"
            if (-not $zip.GetEntry('EmpireAtWar.exe')) { throw 'ZIP executable is not at the archive root.' }
        } finally { $zip.Dispose() }
        $shell = New-Object -ComObject Shell.Application
        $zipFolder = $shell.NameSpace($zipPath)
        if ($null -eq $zipFolder -or $null -eq $zipFolder.ParseName('EmpireAtWar.exe')) { throw 'Windows ZIP handler cannot read the packaged player.' }
        Get-FileHash -LiteralPath "$artifacts\EmpireAtWar-Windows.zip" -Algorithm SHA256 | Select-Object Hash | ConvertTo-Json | Set-Content -LiteralPath "$artifacts\zip-sha256.json"
    }
    exit 0
} catch {
    Write-Error -Message $_ -ErrorAction Continue
    exit 1
}
