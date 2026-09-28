[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$root = 'F:\Jenkins'
$java = 'F:\Jenkins\app\java21\jdk-21.0.12.1+1\bin\java.exe'
$war = Join-Path $root 'app\jenkins.war'
$existing = @(Get-CimInstance Win32_Process -Filter "Name='java.exe'" | Where-Object { $_.CommandLine -like "*$war*" })
if ($existing.Count -gt 1) { throw 'Multiple Jenkins instances require investigation.' }
if ($existing.Count -eq 1) { Write-Host "Jenkins already running: PID $($existing[0].ProcessId)"; return }
if (Get-NetTCPConnection -LocalPort 8080 -State Listen -ErrorAction SilentlyContinue) { throw 'Port 8080 is occupied.' }
$env:JENKINS_HOME = Join-Path $root 'data'
$process = Start-Process -FilePath $java -ArgumentList @('-Xms256m', '-Xmx1024m', '-Djenkins.install.runSetupWizard=false', '-jar', $war, '--httpListenAddress=127.0.0.1', '--httpPort=8080') -WorkingDirectory $root -WindowStyle Hidden -RedirectStandardOutput "$root\logs\jenkins.out.log" -RedirectStandardError "$root\logs\jenkins.err.log" -PassThru
$process.Id | Set-Content -LiteralPath "$root\jenkins.pid"
Write-Host "Started Jenkins: PID $($process.Id)"
