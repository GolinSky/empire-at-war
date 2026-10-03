$ErrorActionPreference = 'Stop'
$converter = Join-Path $env:LOCALAPPDATA 'AI-Tools\Blender\blender-3.6.23-windows-x64\blender.exe'
if (-not (Test-Path -LiteralPath $converter)) {
    throw "Blender converter is missing: $converter"
}
if (Get-NetTCPConnection -State Listen -LocalPort 9876 -ErrorAction SilentlyContinue) {
    Write-Output 'Port 9876 already has a listener. Verify its Blender version with MCP.'
    return
}
# The enabled MCP add-on starts automatically; keep this helper launch hidden.
Start-Process -FilePath $converter -WindowStyle Hidden
