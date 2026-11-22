# Downloads the pinned MIT PlantUML jar into Tools/PlantUml/bin and verifies its SHA-256.
# Usage: pwsh Tools/PlantUml/Install-PlantUml.ps1 [-Force]
[CmdletBinding()]
param(
    [switch]$Force
)

$ErrorActionPreference = 'Stop'

$VERSION = '1.2026.10'
$JAR_NAME = "plantuml-mit-$VERSION.jar"
$SHA256 = '30a66b81a16127fd3ec560992f31964fe9ae3f1233c614ef4558a4558a21489e'
$URL = "https://github.com/plantuml/plantuml/releases/download/v$VERSION/$JAR_NAME"

$binDirectory = Join-Path $PSScriptRoot 'bin'
$target = Join-Path $binDirectory 'plantuml.jar'

function Get-JarHash([string]$path) {
    (Get-FileHash -Path $path -Algorithm SHA256).Hash.ToLowerInvariant()
}

if ((Test-Path $target) -and -not $Force) {
    if ((Get-JarHash $target) -eq $SHA256) {
        Write-Host "PlantUML $VERSION already installed: $target"
        exit 0
    }
    Write-Host "Installed jar does not match $VERSION; reinstalling."
}

if (-not (Get-Command java -ErrorAction SilentlyContinue)) {
    throw 'Java is not on PATH. Install a JDK/JRE 11+ before rendering diagrams.'
}

New-Item -ItemType Directory -Force -Path $binDirectory | Out-Null
$download = Join-Path $binDirectory "$JAR_NAME.download"

Write-Host "Downloading $URL"
Invoke-WebRequest -Uri $URL -OutFile $download -UseBasicParsing

$actual = Get-JarHash $download
if ($actual -ne $SHA256) {
    Remove-Item $download -Force
    throw "SHA-256 mismatch for $JAR_NAME. Expected $SHA256, got $actual."
}

Move-Item -Path $download -Destination $target -Force
Write-Host "Installed PlantUML $VERSION (MIT): $target"
