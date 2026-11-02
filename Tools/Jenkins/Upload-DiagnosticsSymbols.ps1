[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$project = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$symbols = Join-Path $env:WORKSPACE 'player-symbols'
$artifacts = Join-Path $env:WORKSPACE 'artifacts'
if ([string]::IsNullOrWhiteSpace($env:USYM_UPLOAD_AUTH_TOKEN)) {
    throw 'Bind the unity-diagnostics-upload-token Jenkins secret text credential before uploading.'
}
$settings = Get-Content -LiteralPath "$project\ProjectSettings\ProjectSettings.asset" -Raw
$projectId = [regex]::Match($settings, '(?m)^\s*cloudProjectId:\s*([a-fA-F0-9-]{36})\s*$').Groups[1].Value
if (-not $projectId) { throw 'Link the committed project to Unity Cloud before uploading symbols.' }
$version = (Select-String -LiteralPath "$project\ProjectSettings\ProjectVersion.txt" -Pattern '^m_EditorVersion: (.+)$').Matches[0].Groups[1].Value
$tool = "F:\Program Files\Unity\Hub\Editor\$version\Editor\Data\Tools\usymtool.exe"
foreach ($file in @($tool, "$symbols\GameAssembly.pdb", "$artifacts\build-info.json", "$artifacts\commit.txt")) {
    if (-not (Test-Path -LiteralPath $file -PathType Leaf)) { throw "Required symbol upload input missing: $file" }
}
$il2cpp = Join-Path $symbols 'il2cppOutput'
if (-not (Test-Path -LiteralPath $il2cpp -PathType Container)) { throw 'IL2CPP generated source is missing.' }
$build = Get-Content -LiteralPath "$artifacts\build-info.json" -Raw | ConvertFrom-Json
$commit = (Get-Content -LiteralPath "$artifacts\commit.txt").Trim()
if ($build.commit -ne $commit -or $build.build -ne $env:BUILD_NUMBER) {
    throw 'Symbols must belong to the current Jenkins build.'
}
# The token already identifies the project. -projectId selects a different upload mode.
$previousUrl = $env:USYM_UPLOAD_URL_SOURCE
$env:USYM_UPLOAD_URL_SOURCE = 'https://perf-events.cloud.unity3d.com/url'
try {
    Push-Location -LiteralPath $project
    try {
        & $tool -symbolPath "$symbols\GameAssembly.pdb" -il2cppOutputPath $il2cpp -il2cppFileRoot '.' -log "$env:WORKSPACE\symbol-upload.log"
        if ($LASTEXITCODE -ne 0) { throw "Diagnostics symbol upload failed: exit $LASTEXITCODE. See symbol-upload.log." }
    } finally { Pop-Location }
    @{
        State = 'UploadCommandSucceeded'; ProjectId = $projectId
        Build = $build.build; Commit = $commit
        PdbSHA256 = (Get-FileHash -LiteralPath "$symbols\GameAssembly.pdb" -Algorithm SHA256).Hash
        Verification = 'Native/C# stack resolution still requires a matching standalone crash report.'
    } | ConvertTo-Json | Set-Content -LiteralPath "$artifacts\symbol-upload.json" -Encoding UTF8
} finally { $env:USYM_UPLOAD_URL_SOURCE = $previousUrl }
