[CmdletBinding()]
param([Parameter(Mandatory)][string]$ProjectPath)
$ErrorActionPreference = 'Stop'
$manifest = Get-Content -LiteralPath "$ProjectPath\Packages\manifest.json" -Raw | ConvertFrom-Json
$lock = Get-Content -LiteralPath "$ProjectPath\Packages\packages-lock.json" -Raw | ConvertFrom-Json
foreach ($dependency in $manifest.dependencies.PSObject.Properties) {
    $version = [string]$dependency.Value
    if ($version.StartsWith('file:')) { throw "External file package is not reproducible: $($dependency.Name)" }
    $entry = $lock.dependencies.PSObject.Properties[$dependency.Name]
    if ($null -eq $entry) { throw "Package missing from lock file: $($dependency.Name)" }
    if ($entry.Value.source -eq 'git' -or $version -match '^(https?://|git[+:]|ssh://)') {
        if ($version -notmatch '#([0-9a-fA-F]{40})$') { throw "Git package must pin a full commit: $($dependency.Name)" }
        if ($entry.Value.version -ne $version -or $entry.Value.hash -ne $Matches[1]) { throw "Git package lock differs from manifest: $($dependency.Name)" }
    }
}
