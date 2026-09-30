[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$root = 'F:\Jenkins'
$war = Join-Path $root 'app\jenkins.war'
$existing = @(Get-CimInstance Win32_Process -Filter "Name='java.exe'" | Where-Object { $_.CommandLine -like "*$war*" })
if ($existing.Count -gt 1) { throw 'Multiple Jenkins instances require investigation.' }
if ($existing.Count -eq 1) {
    $process = Get-Process -Id $existing[0].ProcessId
    Stop-Process -Id $process.Id
    if (-not $process.WaitForExit(30000)) { throw 'Jenkins did not stop within 30 seconds.' }
}
if (Get-CimInstance Win32_Process -Filter "Name='java.exe'" | Where-Object { $_.CommandLine -like "*$war*" }) { throw 'Jenkins is still running.' }
if (Test-Path -LiteralPath "$root\jenkins.pid") { Remove-Item -LiteralPath "$root\jenkins.pid" }
Write-Host 'Jenkins stopped.'
