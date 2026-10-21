[CmdletBinding()]
param(
    [string]$Revision = 'refs/heads/main',
    [string]$Repository = 'F:\Private\empire-at-war',
    [string]$AppPath = 'F:\Jenkins\app'
)
$ErrorActionPreference = 'Stop'
$commit = & git -C $Repository rev-parse --verify "$Revision^{commit}"
if ($LASTEXITCODE -ne 0) { throw 'Cannot resolve the build-tools commit.' }
$commit = $commit.Trim()
$staging = Join-Path ([IO.Path]::GetTempPath()) ('jenkins-tools-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $staging | Out-Null
try {
    & git -C $Repository archive --format=zip --output "$staging\tools.zip" $commit Tools/Jenkins
    if ($LASTEXITCODE -ne 0) { throw 'Cannot export committed build tools.' }
    Expand-Archive -LiteralPath "$staging\tools.zip" -DestinationPath "$staging\export"
    $source = "$staging\export\Tools\Jenkins"
    $installed = Join-Path $AppPath 'build-tools'
    New-Item -ItemType Directory -Path $installed -Force | Out-Null
    $hashes = @(foreach ($file in Get-ChildItem -LiteralPath $source -File) {
        $target = Join-Path $installed $file.Name
        Copy-Item -LiteralPath $file.FullName -Destination $target -Force
        $expected = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash
        if ((Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash -ne $expected) { throw "Installed hash mismatch: $($file.Name)" }
        @{File=$file.Name; SHA256=$expected}
    })
    @{Commit=$commit; Files=$hashes} | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath "$AppPath\build-tools-installation.json" -Encoding UTF8
    $commit | Set-Content -LiteralPath "$AppPath\build-tools-commit.txt" -Encoding ASCII
    Write-Host "Installed committed build tools: $commit"
} finally {
    $resolved = [IO.Path]::GetFullPath($staging)
    $tempRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd('\') + '\'
    if (-not $resolved.StartsWith($tempRoot, [StringComparison]::OrdinalIgnoreCase)) { throw 'Unexpected staging path.' }
    Remove-Item -LiteralPath $resolved -Recurse -Force
}
