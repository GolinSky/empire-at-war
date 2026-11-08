[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Tag,
    [Parameter(Mandatory)][string]$Commit,
    [Parameter(Mandatory)][string]$ZipPath,
    [Parameter(Mandatory)][string]$ZipSha256,
    [string]$Repository = 'F:\Private\empire-at-war',
    [string]$CacheRoot = 'F:\Jenkins\velopack-cache'
)
# Packs the archived player + launcher (built from the source commit) into Velopack release files.
# Output is cached per tag so a resumed publish uploads byte-identical assets.
# Returns the FileInfo objects to upload; the caller owns all GitHub mutations.
$ErrorActionPreference = 'Stop'
$repo = 'GolinSky/empire-at-war'
$packId = 'EmpireAtWar'
$vpk = 'F:\Jenkins\app\vpk\vpk.exe'
$version = $Tag.Substring(1)
$cache = Join-Path $CacheRoot $Tag
$releases = Join-Path $cache 'Releases'
$markerPath = Join-Path $cache 'pack-complete.json'

function Get-UploadFiles {
    $assets = Get-Content -LiteralPath "$releases\assets.win.json" -Raw | ConvertFrom-Json
    @(foreach ($asset in $assets) { Get-Item -LiteralPath (Join-Path $releases $asset.RelativeFileName) }) + @(Get-Item -LiteralPath "$releases\releases.win.json")
}

if (Test-Path -LiteralPath $markerPath -PathType Leaf) {
    $marker = Get-Content -LiteralPath $markerPath -Raw | ConvertFrom-Json
    if ($marker.Tag -ceq $Tag -and $marker.Commit -ieq $Commit -and $marker.ZipSha256 -ieq $ZipSha256) {
        Write-Host "Reusing cached Velopack release: $cache"
        return Get-UploadFiles
    }
}
if (-not (Test-Path -LiteralPath $vpk -PathType Leaf)) { throw "Install the pinned vpk CLI at $vpk (see Tools/Jenkins/README.md)." }
if (Test-Path -LiteralPath $cache) { Remove-Item -LiteralPath $cache -Recurse -Force }
$staging = Join-Path $cache 'staging'
$launcherSource = Join-Path $cache 'launcher-src'
New-Item -ItemType Directory -Path $staging, $launcherSource, $releases | Out-Null

& git -C $Repository cat-file -e "${Commit}:Tools/Launcher/EmpireAtWarLauncher.csproj"
if ($LASTEXITCODE -ne 0) { throw "Source commit $Commit has no Tools/Launcher; build and publish a commit that includes the launcher." }
& git -C $Repository archive --format=zip --output "$cache\launcher.zip" $Commit Tools/Launcher | Out-Host
if ($LASTEXITCODE -ne 0) { throw 'Cannot export the launcher source from the source commit.' }
Expand-Archive -LiteralPath "$cache\launcher.zip" -DestinationPath $launcherSource

Add-Type -AssemblyName System.IO.Compression.FileSystem
[IO.Compression.ZipFile]::ExtractToDirectory($ZipPath, $staging)
if (-not (Test-Path -LiteralPath "$staging\EmpireAtWar.exe" -PathType Leaf)) { throw 'Player ZIP has no EmpireAtWar.exe at its root.' }
if (Test-Path -LiteralPath "$staging\EmpireAtWarLauncher.exe") { throw 'Player ZIP already contains EmpireAtWarLauncher.exe.' }

& dotnet publish "$launcherSource\Tools\Launcher\EmpireAtWarLauncher.csproj" -c Release -o "$cache\launcher" --nologo | Out-Host
if ($LASTEXITCODE -ne 0) { throw 'Launcher publish failed.' }
Copy-Item -LiteralPath "$cache\launcher\EmpireAtWarLauncher.exe" -Destination $staging

# Previous published full package → delta. Drafts are never used: clients only see published releases.
# The repository is public; this anonymous read sees only published releases, matching what launchers see.
$headers = @{ Accept='application/vnd.github+json'; 'User-Agent'='EmpireAtWar-Jenkins' }
# Windows PowerShell emits a JSON array as one pipeline object; assign first so it enumerates.
$all = Invoke-RestMethod -Uri "https://api.github.com/repos/$repo/releases?per_page=100" -Headers $headers
$published = @($all | Where-Object { -not $_.draft -and $_.tag_name -cne $Tag } | Sort-Object { [DateTime]$_.published_at } -Descending)
$previous = $null
foreach ($release in $published) {
    $previous = @($release.assets | Where-Object { $_.name -clike "$packId-*-full.nupkg" })[0]
    if ($previous) { break }
}
if ($previous) {
    $previousPath = Join-Path $releases $previous.name
    Write-Host "Downloading delta base $($previous.name) ($([math]::Round($previous.size / 1GB, 2)) GiB)"
    Invoke-WebRequest -Uri $previous.browser_download_url -OutFile $previousPath -UseBasicParsing -Headers @{ 'User-Agent'='EmpireAtWar-Jenkins' }
    if ((Get-Item -LiteralPath $previousPath).Length -ne [long]$previous.size -or "sha256:$((Get-FileHash -LiteralPath $previousPath -Algorithm SHA256).Hash)" -ine $previous.digest) {
        throw "Downloaded delta base $($previous.name) does not match GitHub's size/SHA-256."
    }
} else {
    Write-Host 'No published Velopack release yet; this release has no delta.'
}

$packArguments = @('pack', '--packId', $packId, '--packVersion', $version, '--packDir', $staging, '--mainExe', 'EmpireAtWarLauncher.exe',
    '--packTitle', 'Empire at War', '--packAuthors', 'GolinSkyCompany', '--runtime', 'win-x64', '--outputDir', $releases)
# Commits before the launcher icon was added have no .ico; their Setup.exe keeps Velopack's default icon.
$icon = "$launcherSource\Tools\Launcher\EmpireAtWar.ico"
if (Test-Path -LiteralPath $icon -PathType Leaf) { $packArguments += @('--icon', $icon) }
& $vpk @packArguments | Out-Host
if ($LASTEXITCODE -ne 0) { throw 'vpk pack failed.' }

# GithubSource resolves every feed entry against the release that hosts the feed, so keep only this version's entries.
$feed = Get-Content -LiteralPath "$releases\releases.win.json" -Raw | ConvertFrom-Json
$feed.Assets = @($feed.Assets | Where-Object { $_.Version -ceq $version })
if (-not @($feed.Assets | Where-Object { $_.Type -eq 'Full' }).Count) { throw "Feed has no full package for $version." }
[IO.File]::WriteAllText("$releases\releases.win.json", ($feed | ConvertTo-Json -Depth 4 -Compress), [Text.UTF8Encoding]::new($false))
if ($previous) { Remove-Item -LiteralPath $previousPath }
Remove-Item -LiteralPath $staging, $launcherSource, "$cache\launcher", "$cache\launcher.zip" -Recurse -Force

@{ Tag=$Tag; Commit=$Commit; ZipSha256=$ZipSha256; DeltaBase=$(if ($previous) { $previous.name } else { $null }); PackedAtUtc=[DateTime]::UtcNow.ToString('o') } |
    ConvertTo-Json | Set-Content -LiteralPath $markerPath -Encoding UTF8
return Get-UploadFiles
