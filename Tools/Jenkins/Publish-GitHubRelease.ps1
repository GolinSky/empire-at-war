[CmdletBinding()]
param([switch]$ValidateOnly)
$ErrorActionPreference = 'Stop'
$repo = 'GolinSky/empire-at-war'
$gh = 'F:\Jenkins\app\gh\bin\gh.exe'
$artifacts = Join-Path $env:WORKSPACE 'artifacts'
$reportPath = Join-Path $env:WORKSPACE 'release-result.json'
$tag = $env:RELEASE_TAG
$sourceBuild = $env:SOURCE_BUILD
$report = [ordered]@{ Repository=$repo; SourceJob='EmpireAtWar-Windows-Local'; SourceBuild=$sourceBuild; Tag=$tag; Status='Validating'; Draft=$true }

function Invoke-GitHubApi {
    param([string]$Resource, [string]$Method = 'GET', [hashtable]$Body, [switch]$AllowMissing)
    $request = @{
        Uri="https://api.github.com/repos/$repo/$Resource"; Method=$Method; UseBasicParsing=$true
        Headers=@{ Authorization="Bearer $env:GH_TOKEN"; Accept='application/vnd.github+json'; 'X-GitHub-Api-Version'='2026-03-10'; 'User-Agent'='EmpireAtWar-Jenkins' }
    }
    if ($Body) { $request.Body = $Body | ConvertTo-Json -Compress; $request.ContentType = 'application/json' }
    try {
        $response = Invoke-WebRequest @request
        return @{ Status=[int]$response.StatusCode; Data=($response.Content | ConvertFrom-Json) }
    } catch {
        if ($null -eq $_.Exception.Response) { throw "GitHub request failed before an HTTP response: $Method $Resource" }
        $status = [int]$_.Exception.Response.StatusCode
        if ($AllowMissing -and $status -eq 404) { return @{ Status=404; Data=$null } }
        throw "GitHub returned HTTP $status for $Method $Resource. Check repository access, token permissions, and repository rules."
    }
}

function Get-GitHubCollection {
    param([string]$Resource)
    $page = 1
    do {
        $items = @((Invoke-GitHubApi -Resource "${Resource}?per_page=100&page=$page").Data)
        $items
        $page++
    } while ($items.Count -eq 100)
}

function Assert-MatchingDraft {
    param($Release)
    if (-not $Release.draft) { throw "Release $tag is already published; it will not be changed." }
    if ($Release.tag_name -cne $tag -or -not ([string]$Release.body).Contains($marker) -or $Release.prerelease -ne $prerelease) {
        throw 'Existing draft does not match this source build, ZIP checksum, tag, and prerelease setting.'
    }
}

function Assert-MatchingAsset {
    param($Asset, $File)
    if ($Asset.state -ne 'uploaded' -or $Asset.name -cne $File.Name -or [long]$Asset.size -ne $File.Size -or $Asset.digest -ine "sha256:$($File.Sha256)") {
        throw "Asset $($File.Name) has a missing/mismatched digest, size, or upload state. Inspect the draft and resolve the conflict manually; no asset was overwritten."
    }
}

try {
    if ($sourceBuild -cnotmatch '^[1-9][0-9]*$') { throw 'SOURCE_BUILD must be a positive build number.' }
    if ($tag -cnotmatch '^v(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)(-[0-9A-Za-z-]+(\.[0-9A-Za-z-]+)*)?$') { throw 'Invalid RELEASE_TAG.' }
    if ($env:PRERELEASE -notin @('true','false')) { throw 'PRERELEASE must be true or false.' }
    $prerelease = $env:PRERELEASE -eq 'true'
    foreach ($name in @('EmpireAtWar-Windows.zip','commit.txt','zip-sha256.json','build-provenance.json')) {
        if (-not (Test-Path -LiteralPath (Join-Path $artifacts $name) -PathType Leaf)) { throw "Required archived artifact is missing: $name. It may have expired; choose a retained successful build." }
    }
    $sha = (Get-Content -LiteralPath "$artifacts\commit.txt" -Raw).Trim()
    if ($sha -notmatch '^[0-9a-fA-F]{40}$') { throw 'Archived commit.txt must contain a full Git commit SHA.' }
    $sha = $sha.ToLowerInvariant()
    $provenance = Get-Content -LiteralPath "$artifacts\build-provenance.json" -Raw | ConvertFrom-Json
    if ($provenance.outcome -ne 'success' -or $provenance.exitCode -ne 0 -or $provenance.source.revision -ine $sha -or $provenance.source.dirty -ne $false -or $provenance.build.target -ne 'StandaloneWindows64') {
        throw 'Archived provenance must describe a successful, clean Windows x64 build at the archived commit.'
    }
    $files = @(foreach ($name in @('EmpireAtWar-Windows.zip','commit.txt','zip-sha256.json')) {
        $file = Get-Item -LiteralPath (Join-Path $artifacts $name)
        if ($file.Length -le 0 -or $file.Length -ge 2GB) { throw "Release asset must be nonempty and smaller than 2 GiB: $name" }
        [pscustomobject]@{ Name=$name; Path=$file.FullName; Size=$file.Length; Sha256=(Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash.ToLowerInvariant() }
    })
    $zipHash = $files[0].Sha256
    $expectedHash = (Get-Content -LiteralPath "$artifacts\zip-sha256.json" -Raw | ConvertFrom-Json).Hash
    if ($expectedHash -notmatch '^[0-9a-fA-F]{64}$' -or $zipHash -ine $expectedHash) { throw 'Archived ZIP SHA-256 does not match zip-sha256.json.' }
    $report.Commit = $sha
    $report.Prerelease = $prerelease
    $report.Assets = @($files | Select-Object Name,Size,Sha256)
    $report.Status = 'ArtifactsValidated'
    $report | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $reportPath -Encoding UTF8
    if ($ValidateOnly) { Write-Host "Validated source build #$sourceBuild at $sha; ZIP SHA-256 $zipHash."; exit 0 }
    if ([string]::IsNullOrWhiteSpace($env:GH_TOKEN)) { throw 'Add Jenkins Secret Text credential github-releases before uploading.' }
    if (-not (Test-Path -LiteralPath $gh -PathType Leaf)) { throw "Install the pinned GitHub CLI at $gh." }
    $env:GH_HOST = 'github.com'
    $env:GH_PROMPT_DISABLED = '1'
    Remove-Item Env:GH_DEBUG -ErrorAction SilentlyContinue
    $commit = Invoke-GitHubApi -Resource "commits/$sha" -AllowMissing
    if ($commit.Status -eq 404) { throw "Commit $sha is unavailable in $repo. Check token access and push the intended source commit before retrying; Jenkins will not push local commits." }
    if ($commit.Data.sha -ine $sha) { throw 'GitHub returned a different source commit.' }
    $marker = "<!-- empire-at-war:source=EmpireAtWar-Windows-Local#$sourceBuild;commit=$sha;zip-sha256=$zipHash -->"
    $releases = @(Get-GitHubCollection -Resource 'releases' | Where-Object { $_.tag_name -ceq $tag })
    if ($releases.Count -gt 1) { throw 'Multiple releases use this tag; resolve the ambiguity manually.' }
    if ($releases.Count -eq 1) { Assert-MatchingDraft $releases[0] }
    $ref = Invoke-GitHubApi -Resource "git/ref/tags/$tag" -AllowMissing
    if ($ref.Status -eq 404) {
        if ($releases.Count -gt 0) { throw 'Existing draft has no tag; inspect it before retrying.' }
        $ref = Invoke-GitHubApi -Resource 'git/refs' -Method POST -Body @{ ref="refs/tags/$tag"; sha=$sha }
    }
    $target = $ref.Data.object
    while ($target.type -eq 'tag') { $target = (Invoke-GitHubApi -Resource "git/tags/$($target.sha)").Data.object }
    if ($target.type -ne 'commit' -or $target.sha -ine $sha) { throw 'Existing tag resolves to a different commit; it will not be moved.' }
    if ($releases.Count -eq 0) {
        $notes = "Source: EmpireAtWar-Windows-Local build #$sourceBuild`nCommit: $sha`nZIP SHA-256: $zipHash`n`n$marker"
        $notesPath = Join-Path $env:WORKSPACE 'release-notes.txt'
        [IO.File]::WriteAllText($notesPath, $notes, [Text.UTF8Encoding]::new($false))
        $arguments = @('release','create',$tag,'--repo',$repo,'--verify-tag','--draft','--title',"EmpireAtWar $tag",'--notes-file',$notesPath)
        if ($prerelease) { $arguments += '--prerelease' }
        & $gh @arguments
        if ($LASTEXITCODE -ne 0) { throw 'Draft creation failed. Inspect GitHub state and rerun with the same build and tag to resume.' }
        $releases = @(Get-GitHubCollection -Resource 'releases' | Where-Object { $_.tag_name -ceq $tag })
        if ($releases.Count -ne 1) { throw 'Created draft could not be uniquely resolved.' }
    }
    $release = $releases[0]
    Assert-MatchingDraft $release
    $report.ReleaseUrl = $release.html_url
    $report.ReleaseId = $release.id
    $report.Status = 'Uploading'
    $report | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $reportPath -Encoding UTF8
    $release.html_url | Set-Content -LiteralPath (Join-Path $env:WORKSPACE 'release-url.txt') -Encoding UTF8
    $assets = @(Get-GitHubCollection -Resource "releases/$($release.id)/assets")
    foreach ($file in $files) {
        $existing = @($assets | Where-Object { $_.name -ceq $file.Name })
        if ($existing.Count -gt 1) { throw "Duplicate asset name: $($file.Name)" }
        if ($existing.Count -eq 1) { Assert-MatchingAsset $existing[0] $file }
    }
    foreach ($file in $files) {
        if (@($assets | Where-Object { $_.name -ceq $file.Name }).Count -eq 1) { Write-Host "Already verified: $($file.Name)"; continue }
        Assert-MatchingDraft (Invoke-GitHubApi -Resource "releases/$($release.id)").Data
        & $gh release upload $tag $file.Path --repo $repo
        if ($LASTEXITCODE -ne 0) { throw "Upload failed for $($file.Name). The draft is preserved; rerun with the same build and tag to resume, or inspect incomplete assets." }
    }
    Assert-MatchingDraft (Invoke-GitHubApi -Resource "releases/$($release.id)").Data
    $assets = @(Get-GitHubCollection -Resource "releases/$($release.id)/assets")
    foreach ($file in $files) {
        $uploaded = @($assets | Where-Object { $_.name -ceq $file.Name })
        if ($uploaded.Count -ne 1) { throw "Uploaded asset not uniquely found: $($file.Name)" }
        Assert-MatchingAsset $uploaded[0] $file
    }
    $report.Status = 'VerifiedDraft'
    $report | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $reportPath -Encoding UTF8
    Write-Host "Verified draft: $($release.html_url). Review and publish manually on GitHub."
    exit 0
} catch {
    $report.Status = 'Failed'
    $report.Error = $_.Exception.Message
    $report | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $reportPath -Encoding UTF8
    Write-Error -Message $_.Exception.Message -ErrorAction Continue
    exit 1
}
