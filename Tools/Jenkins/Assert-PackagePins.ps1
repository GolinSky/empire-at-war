[CmdletBinding()]
param([Parameter(Mandatory)][string]$ProjectPath)
$ErrorActionPreference = 'Stop'
$manifest = Get-Content -LiteralPath "$ProjectPath\Packages\manifest.json" -Raw | ConvertFrom-Json
$lock = Get-Content -LiteralPath "$ProjectPath\Packages\packages-lock.json" -Raw | ConvertFrom-Json
$failures = [Collections.Generic.List[string]]::new()
foreach ($dependency in $manifest.dependencies.PSObject.Properties) {
    $version = [string]$dependency.Value
    if ($version.StartsWith('file:')) {
        $failures.Add("External file package is not reproducible: $($dependency.Name)")
        continue
    }
    $entry = $lock.dependencies.PSObject.Properties[$dependency.Name]
    if ($null -eq $entry) {
        $failures.Add("Package missing from lock file: $($dependency.Name)")
        continue
    }
    if ($entry.Value.source -eq 'git' -or $version -match '^(https?://|git[+:]|ssh://)') {
        if ($version -notmatch '#([0-9a-fA-F]{40})$') {
            $failures.Add("Git package must pin a full commit: $($dependency.Name)")
            continue
        }
        if ($entry.Value.version -ne $version -or $entry.Value.hash -ne $Matches[1]) {
            $failures.Add("Git package lock differs from manifest: $($dependency.Name)")
        }
    }
}
if ($failures.Count) { throw ($failures -join [Environment]::NewLine) }
