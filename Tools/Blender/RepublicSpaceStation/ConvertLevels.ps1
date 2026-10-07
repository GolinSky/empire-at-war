param([int[]]$Levels = @(1, 2, 3, 4, 5))
$ErrorActionPreference = 'Stop'
foreach ($level in $Levels) {
    $name = "RepublicSpaceStationLevel$level"
    uvx --from mcp-for-blender==2.1.3 python Tools/Blender/RepublicSpaceStation/Call.py "Temp/RepublicStationImport/${name}_Requests.json" 2> "Temp/RepublicStationImport/${name}.mcp.log"
    if ($LASTEXITCODE -ne 0) { throw "Republic conversion failed: $name. Inspect its log." }
    Write-Output "Converted $name"
}
