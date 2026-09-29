$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\BuildResources.ps1"
$source = 'F:\Private\empire-at-war'
$ci = 'F:\UnityCI\empire-at-war'

function Invoke-Git {
    $output = & git @args
    if ($LASTEXITCODE -ne 0) { throw "Git failed: $args" }
    return $output
}

function Assert-CiWorktree {
    Assert-PlainPath $ci
    if (-not (Test-Path -LiteralPath "$ci\.git" -PathType Leaf)) { throw 'CI must be a linked Git worktree.' }
    if ([IO.Path]::GetFullPath((Invoke-Git -C $ci rev-parse --show-toplevel)) -ne $ci) { throw 'CI root mismatch.' }
    $common = Invoke-Git -C $ci rev-parse --path-format=absolute --git-common-dir
    $expected = Invoke-Git -C $source rev-parse --path-format=absolute --git-common-dir
    if ([IO.Path]::GetFullPath($common) -ne [IO.Path]::GetFullPath($expected)) { throw 'Shared Git directory mismatch.' }
    $registration = (Invoke-Git -C $source worktree list --porcelain) -join "`n"
    if ($registration -notmatch '(?m)^worktree F:/UnityCI/empire-at-war\r?$') { throw 'CI worktree is not registered.' }
    & git -C $ci symbolic-ref -q HEAD
    if ($LASTEXITCODE -ne 1) { throw 'CI HEAD must be detached.' }
    Assert-CiIdle
    $reparse = Get-ChildItem -LiteralPath $ci -Recurse -Force -Attributes ReparsePoint
    if ($reparse) { throw 'CI contains reparse points; cleanup rejected.' }
}

Assert-PlainPath $source
Assert-PlainPath $ci
Assert-CiIdle
Write-ResourceSample -Preflight -Stage 'prepare'
$librarySize = Get-DirectoryGiB "$ci\Library"
if ($librarySize -gt $policy.LibraryBudgetGiB) { throw "CI Library $librarySize GiB exceeds budget. Review maintenance while idle." }
$sha = (Invoke-Git -C $source rev-parse --verify 'refs/heads/main^{commit}').Trim()
$sha | Set-Content -LiteralPath "$env:WORKSPACE\artifacts\commit.txt" -Encoding ASCII
if ((Invoke-Git -C $source ls-tree -r $sha) -match '^160000 ') { throw 'Submodules require an independent clone.' }
if (Invoke-Git -C $source ls-tree --name-only $sha -- .gitmodules) { throw 'Submodule configuration rejected.' }
$env:GIT_LFS_SKIP_SMUDGE = '1'
try {
    if (-not (Test-Path -LiteralPath $ci)) { Invoke-Git -C $source worktree add --detach $ci $sha }
    Assert-CiWorktree
    Invoke-Git -C $ci checkout --detach --force $sha
    Invoke-Git -C $ci clean -fdx -e /Library/
} finally { Remove-Item Env:GIT_LFS_SKIP_SMUDGE -ErrorAction SilentlyContinue }
Invoke-Git -C $ci lfs checkout
Invoke-Git -C $ci lfs pull
Invoke-Git -C $ci lfs fsck
# LFS hydration can leave pointer-sized index metadata on Windows.
# Reapply clean filters, then reject any canonical content change against the pinned commit.
Invoke-Git -C $ci add --renormalize -u
Invoke-Git -C $ci diff --cached --exit-code $sha --
if (Invoke-Git -C $ci status --porcelain) { throw 'Hydrated snapshot is dirty.' }
if ((Get-Content -LiteralPath "$ci\Packages\manifest.json" -Raw) -match '"file:') { throw 'External file packages are not reproducible.' }
if ((Get-Content -LiteralPath "$ci\Assets\AddressableAssetsData\AddressableAssetSettings.asset" -Raw) -notmatch 'm_BuildAddressablesWithPlayerBuild: 1') { throw 'Commit explicit Addressables BuildWithPlayer before building.' }
Copy-Item -LiteralPath "$PSScriptRoot\ResourcePolicy.json" -Destination "$env:WORKSPACE\artifacts\resource-policy.json"
Write-Host "Pinned main at $sha; Library $([math]::Round($librarySize,2)) GiB."
