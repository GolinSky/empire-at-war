$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../..')).Path
$node = (Get-Command node -CommandType Application | Select-Object -First 1).Source
$launcher = Join-Path $PSScriptRoot 'start.mjs'

# Start one shared backend at sign-in; Codex opens async HTTP connections to it.
$shortcutPath = Join-Path ([Environment]::GetFolderPath('Startup')) 'empire-at-war Vault.lnk'
$shell = New-Object -ComObject WScript.Shell
$shortcut = $shell.CreateShortcut($shortcutPath)
$shortcut.TargetPath = $node
$shortcut.Arguments = '"' + $launcher + '"'
$shortcut.WorkingDirectory = $projectRoot
$shortcut.WindowStyle = 7
$shortcut.Description = 'Shared empire-vault MCP backend on localhost:9123'
$shortcut.Save()
& $node $launcher
if ($LASTEXITCODE -ne 0) { throw 'empire-vault startup failed.' }
