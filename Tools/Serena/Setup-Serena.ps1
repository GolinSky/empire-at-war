$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../..')).Path
$runtimePath = Join-Path $projectRoot '.serena/runtime'
$runtimePython = Join-Path $runtimePath 'Scripts/python.exe'
$uv = (Get-Command uv -ErrorAction Stop).Source
if (-not (Test-Path -LiteralPath $runtimePython)) {
    & $uv venv --python 3.13 $runtimePath
    if ($LASTEXITCODE -ne 0) { throw 'Creating the Serena environment failed.' }
}
& $uv pip install --python $runtimePython --link-mode copy -r (Join-Path $PSScriptRoot 'requirements.lock')
if ($LASTEXITCODE -ne 0) { throw 'Installing the pinned Serena environment failed.' }
& $runtimePython (Join-Path $PSScriptRoot 'patch_csharp_adapter.py')
if ($LASTEXITCODE -ne 0) { throw 'Applying the reviewed C# adapter patch failed.' }

# This user-owned shortcut starts this one checkout's service after Windows sign-in.
$startupDirectory = [Environment]::GetFolderPath('Startup')
$shortcutPath = Join-Path $startupDirectory 'empire-at-war Serena.lnk'
$shell = New-Object -ComObject WScript.Shell
$shortcut = $shell.CreateShortcut($shortcutPath)
$shortcut.TargetPath = Join-Path $env:SystemRoot 'System32/WindowsPowerShell/v1.0/powershell.exe'
$shortcut.Arguments = '-NoProfile -ExecutionPolicy RemoteSigned -WindowStyle Hidden -File "' + (Join-Path $PSScriptRoot 'Start-Serena.ps1') + '"'
$shortcut.WorkingDirectory = $projectRoot
$shortcut.WindowStyle = 7
$shortcut.Description = 'Shared Serena C# MCP backend for empire-at-war on localhost:9122'
$shortcut.Save()
Write-Output "Installed sign-in launcher: $shortcutPath"
& (Join-Path $PSScriptRoot 'Start-Serena.ps1')
