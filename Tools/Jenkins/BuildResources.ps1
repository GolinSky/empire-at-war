$ErrorActionPreference = 'Stop'
$policy = Get-Content -LiteralPath "$PSScriptRoot\ResourcePolicy.json" -Raw | ConvertFrom-Json

function Assert-PlainPath([string]$Path) {
    $item = [IO.Path]::GetFullPath($Path)
    while ($item) {
        if ((Test-Path -LiteralPath $item) -and ((Get-Item -LiteralPath $item -Force).Attributes -band [IO.FileAttributes]::ReparsePoint)) {
            throw "Reparse point rejected: $item"
        }
        $item = [IO.Path]::GetDirectoryName($item)
    }
}

function Get-DirectoryGiB([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path)) { return 0 }
    $size = Get-ChildItem -LiteralPath $Path -File -Recurse -Force | Measure-Object Length -Sum
    return $size.Sum / 1GB
}

function Assert-CiIdle {
    $active = @(Get-CimInstance Win32_Process | Where-Object {
        $_.Name -eq 'Unity.exe' -and $_.CommandLine -like '*F:\UnityCI\empire-at-war*'
    })
    if ($active.Count) { throw "CI Editor still active: $($active.ProcessId -join ',')" }
    $lock = 'F:\UnityCI\empire-at-war\Temp\UnityLockfile'
    if (Test-Path -LiteralPath $lock) {
        $handle = [IO.File]::Open($lock, 'Open', 'ReadWrite', 'None')
        $handle.Dispose()
    }
}

function Write-ResourceSample([switch]$Preflight, [string]$Stage = 'build') {
    $ram = (Get-CimInstance Win32_OperatingSystem).FreePhysicalMemory / 1MB
    $roots = @('F:\', 'C:\', [IO.Path]::GetPathRoot($env:TEMP), [IO.Path]::GetPathRoot($env:TMP), [IO.Path]::GetPathRoot($env:LOCALAPPDATA)) | Select-Object -Unique
    $volumes = [ordered]@{}
    $failures = @()
    foreach ($root in $roots) {
        $free = ([IO.DriveInfo]::new($root)).AvailableFreeSpace / 1GB
        $volumes[$root] = [math]::Round($free, 3)
        $required = $policy.DiskReserveGiB
        if ($Preflight) {
            $growth = $policy.AdditionalPeakGiB.PSObject.Properties[$root]
            if ($null -eq $growth) { throw "No measured/estimated disk budget configured for $root" }
            $required += $growth.Value
        }
        if ($free -lt $required) { $failures += "Disk $root has $([math]::Round($free,2)) GiB; requires $required GiB." }
    }
    $ramLimit = $policy.StopAvailableRamGiB
    if ($Preflight) { $ramLimit = $policy.StartAvailableRamGiB }
    if ($Stage -ne 'archive' -and $ram -lt $ramLimit) { $failures += "Available RAM $([math]::Round($ram,2)) GiB; requires $ramLimit GiB." }
    $sample = [ordered]@{Utc=[DateTime]::UtcNow.ToString('o');Stage=$Stage;AvailableRamGiB=[math]::Round($ram,3);FreeDiskGiB=$volumes;Failures=$failures}
    $sample | ConvertTo-Json -Compress | Add-Content -LiteralPath "$env:WORKSPACE\artifacts\resources.jsonl" -Encoding UTF8
    if ($failures.Count) { throw ($failures -join ' ') }
}
