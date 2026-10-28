param([Parameter(Mandatory)][string]$Faction, [switch]$Inspect)
$ErrorActionPreference = 'Stop'
$entryType = if ($Inspect) { 'InspectStationArmament' } else { 'BuildStationArmament' }
$sourcePaths = @("Tools/Blender/SpaceStations/$entryType.cs", 'Tools/Blender/SpaceStations/StationHardPointSurface.cs')
$sources = $sourcePaths | ForEach-Object { Get-Content -LiteralPath $_ -Raw }
$usingLines = $sources | ForEach-Object { [regex]::Matches($_, '(?m)^using [^\r\n]+;') | ForEach-Object Value } | Select-Object -Unique
$bodies = $sources | ForEach-Object { [regex]::Replace($_, '(?m)^using [^\r\n]+;\r?\n', '') }
New-Item -ItemType Directory -Force -Path 'Temp/StationArmament' | Out-Null
$combined = ($usingLines -join "`n") + "`n" + ($bodies -join "`n")
[System.IO.File]::WriteAllText((Join-Path (Get-Location) 'Temp/StationArmament/Build.cs'), $combined)
$scriptArgs = ConvertTo-Json -Compress -InputObject @($Faction)
$resultJson = unity command run_script --file Temp/StationArmament/Build.cs --entry "$entryType.Main" --args $scriptArgs --timeout_ms 120000 --json
$resultJson
if ($LASTEXITCODE -ne 0) { throw "Station armament update failed for $Faction." }
$result = $resultJson | ConvertFrom-Json
if (-not $result.success -or -not $result.data.result.success) { throw "Station armament update failed for $Faction." }
