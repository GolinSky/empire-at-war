$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../..')).Path
$python = (Get-Content -LiteralPath (Join-Path $projectRoot 'graphify-out/.graphify_python') -Raw).Trim()
$pythonWindowless = Join-Path (Split-Path -Parent $python) 'pythonw.exe'
if (-not (Test-Path -LiteralPath $pythonWindowless)) { throw "Missing Graphify runtime: $pythonWindowless" }
& $python -c 'import importlib.metadata; assert importlib.metadata.version("graphifyy") == "0.9.54"'
if ($LASTEXITCODE -ne 0) { throw 'Graphify 0.9.54 is required.' }

$taskName = 'empire-at-war Graphify'
$userId = [Security.Principal.WindowsIdentity]::GetCurrent().Name
$arguments = '"' + (Join-Path $PSScriptRoot 'run.py') + '" watch --root "' + $projectRoot + '"'
$action = New-ScheduledTaskAction -Execute $pythonWindowless -Argument $arguments -WorkingDirectory $projectRoot
$triggers = @(
    (New-ScheduledTaskTrigger -AtLogOn -User $userId),
    (New-ScheduledTaskTrigger -Once -At (Get-Date).AddMinutes(1) -RepetitionInterval (New-TimeSpan -Minutes 1))
)
$settings = New-ScheduledTaskSettingsSet -MultipleInstances IgnoreNew -StartWhenAvailable `
    -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -ExecutionTimeLimit ([TimeSpan]::Zero) `
    -RestartCount 3 -RestartInterval (New-TimeSpan -Minutes 1)
$principal = New-ScheduledTaskPrincipal -UserId $userId -LogonType Interactive -RunLevel Limited
$task = New-ScheduledTask -Action $action -Trigger $triggers -Settings $settings -Principal $principal `
    -Description 'Keep the local Graphify code index current; recover at sign-in and after process exit.'
Register-ScheduledTask -TaskName $taskName -InputObject $task -Force | Out-Null
Start-ScheduledTask -TaskName $taskName
Get-ScheduledTask -TaskName $taskName | Select-Object TaskName, State
