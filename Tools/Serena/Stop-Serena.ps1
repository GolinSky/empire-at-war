$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../..')).Path
$statePath = Join-Path $projectRoot '.serena/service/process.json'
if (-not (Test-Path -LiteralPath $statePath)) { throw 'No managed Serena process record exists.' }
$state = Get-Content -LiteralPath $statePath -Raw | ConvertFrom-Json
$server = Get-CimInstance Win32_Process -Filter "ProcessId = $([int]$state.ProcessId)"
if (-not $server) { Write-Output 'The managed Serena service is already stopped.'; return }
$expectedExe = Join-Path $projectRoot '.serena/runtime/Scripts/serena.exe'
if ($server.ExecutablePath -ine $expectedExe -or
    $server.CreationDate.ToUniversalTime() -ne ([datetime]$state.CreatedUtc).ToUniversalTime()) {
    throw 'The recorded PID no longer identifies this Serena service. No process was stopped.'
}

# Capture only descendants of the verified managed service, then stop deepest first.
$processes = @(Get-CimInstance Win32_Process)
$owned = @($server)
for ($index = 0; $index -lt $owned.Count; $index++) {
    $parent = $owned[$index]
    $owned += @($processes | Where-Object {
        $_.ParentProcessId -eq $parent.ProcessId -and $_.CreationDate -ge $parent.CreationDate
    })
}
for ($index = $owned.Count - 1; $index -ge 0; $index--) {
    $captured = $owned[$index]
    $live = Get-CimInstance Win32_Process -Filter "ProcessId = $($captured.ProcessId)"
    if ($live -and $live.CreationDate -eq $captured.CreationDate) {
        Stop-Process -Id $live.ProcessId -Force
    }
}
Write-Output 'Stopped the managed Serena service and its captured descendants.'
