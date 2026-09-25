$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../..')).Path
$serverExe = Join-Path $projectRoot '.serena/runtime/Scripts/serena.exe'
$serviceDirectory = Join-Path $projectRoot '.serena/service'
$statePath = Join-Path $serviceDirectory 'process.json'
$endpoint = 'http://127.0.0.1:9122/mcp'
$mutex = [Threading.Mutex]::new($false, 'Local\empire-at-war-Serena-9122')
$locked = $false

try {
    $locked = $mutex.WaitOne(10000)
    if (-not $locked) { throw 'Another Serena launcher is still starting the service.' }
    if (-not (Test-Path -LiteralPath $serverExe)) { throw 'Run Tools/Serena/Setup-Serena.ps1 first.' }
    New-Item -ItemType Directory -Path $serviceDirectory -Force | Out-Null

    if (Test-Path -LiteralPath $statePath) {
        $state = Get-Content -LiteralPath $statePath -Raw | ConvertFrom-Json
        $existing = Get-CimInstance Win32_Process -Filter "ProcessId = $([int]$state.ProcessId)"
        if ($existing -and $existing.ExecutablePath -ieq $serverExe -and
            $existing.CreationDate.ToUniversalTime() -eq ([datetime]$state.CreatedUtc).ToUniversalTime()) {
            [pscustomobject]@{ Status = 'AlreadyRunning'; ProcessId = $existing.ProcessId; Endpoint = $endpoint }
            return
        }
    }

    $listener = Get-NetTCPConnection -State Listen -LocalPort 9122 -ErrorAction SilentlyContinue
    if ($listener) { throw "Port 9122 is already owned by process $($listener.OwningProcess). No second server was started." }

    $previousHome = $env:SERENA_HOME
    $previousUtf8 = $env:PYTHONUTF8
    try {
        $env:SERENA_HOME = Join-Path $projectRoot '.serena/home'
        $env:PYTHONUTF8 = '1'
        $server = Start-Process -FilePath $serverExe -WorkingDirectory $projectRoot -WindowStyle Hidden -PassThru `
            -ArgumentList @('start-mcp-server', '--context', 'codex', '--project', ('"' + $projectRoot + '"'),
                '--transport', 'streamable-http', '--host', '127.0.0.1', '--port', '9122', '--enable-web-dashboard', 'false') `
            -RedirectStandardOutput (Join-Path $serviceDirectory 'stdout.log') `
            -RedirectStandardError (Join-Path $serviceDirectory 'stderr.log')
        $started = Get-CimInstance Win32_Process -Filter "ProcessId = $($server.Id)"
        if (-not $started) { throw 'Serena exited during startup. Inspect .serena/service/stderr.log.' }
        [ordered]@{
            ProcessId = $started.ProcessId
            CreatedUtc = $started.CreationDate.ToUniversalTime().ToString('o')
            ExecutablePath = $serverExe
            ProjectRoot = $projectRoot
            Endpoint = $endpoint
        } | ConvertTo-Json | Set-Content -LiteralPath $statePath -Encoding UTF8
        [pscustomobject]@{ Status = 'Starting'; ProcessId = $started.ProcessId; Endpoint = $endpoint }
    } finally {
        $env:SERENA_HOME = $previousHome
        $env:PYTHONUTF8 = $previousUtf8
    }
} finally {
    if ($locked) { $mutex.ReleaseMutex() }
    $mutex.Dispose()
}
