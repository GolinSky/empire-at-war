[CmdletBinding()]
param(
    [switch]$Check,
    [switch]$RuntimesOnly
)

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'

function TestRuntime([string]$Command, [switch]$Node) {
    if (-not (Get-Command $Command -CommandType Application -ErrorAction SilentlyContinue)) { return $false }
    try {
        $version = & $Command --version 2>&1
        if ($LASTEXITCODE -ne 0) { return $false }
        if ($Node) { return "$version" -match '^v(\d+)\.' -and [int]$Matches[1] -ge 20 }
        return $true
    } catch { return $false } # A missing or unusable runtime is repaired below.
}

function AddRuntimePath([string]$Directory) {
    # Preserve unexpanded %VAR% entries and the registry value's original kind.
    $key = [Microsoft.Win32.Registry]::CurrentUser.CreateSubKey('Environment')
    try {
        $current = $key.GetValue('Path', $null, [Microsoft.Win32.RegistryValueOptions]::DoNotExpandEnvironmentNames)
        $kind = [Microsoft.Win32.RegistryValueKind]::ExpandString
        if ($null -ne $current) {
            if ($current -isnot [string]) { throw 'User PATH is not a string; cannot add the runtime directory.' }
            $kind = $key.GetValueKind('Path')
        }
        $entries = @($current -split ';' | Where-Object { $_ -and [Environment]::ExpandEnvironmentVariables($_).TrimEnd('\') -ine $Directory.TrimEnd('\') })
        $updated = (@($Directory) + $entries) -join ';'
        if ($updated -cne $current) {
            $key.SetValue('Path', $updated, $kind)
            if (-not ('EmpireMcp.EnvironmentNotification' -as [type])) {
                Add-Type -Namespace EmpireMcp -Name EnvironmentNotification -MemberDefinition @'
[System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode, SetLastError = true)]
public static extern System.IntPtr SendMessageTimeout(System.IntPtr window, uint message, System.UIntPtr parameter, string value, uint flags, uint timeout, out System.UIntPtr result);
'@
            }
            $notificationResult = [UIntPtr]::Zero
            $sent = [EmpireMcp.EnvironmentNotification]::SendMessageTimeout([IntPtr]0xffff, 0x001a, [UIntPtr]::Zero, 'Environment', 2, 2000, [ref]$notificationResult)
            if ($sent -eq [IntPtr]::Zero) { Write-Warning 'PATH was saved, but Windows could not notify every app. Sign out and back in if new apps cannot find the installed runtimes.' }
        }
    } finally { $key.Dispose() }
    # Keep custom paths inherited from the caller, including tool-managed paths.
    $env:PATH = $Directory + ';' + $env:PATH
}

function InstallScript([string]$Url, [string]$Path) {
    Invoke-WebRequest -Uri $Url -OutFile $Path -UseBasicParsing
    & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $Path
    if ($LASTEXITCODE -ne 0) { throw "Installer $Url failed (exit $LASTEXITCODE)." }
}

try {
    if ($env:OS -ne 'Windows_NT') { throw 'Use bash Tools/setup-mcp.sh on macOS.' }
    if (-not [Environment]::Is64BitProcess) { throw 'Use 64-bit PowerShell; Unity CLI requires a 64-bit system.' }

    $nodeReady = (TestRuntime 'node.exe' -Node) -and (TestRuntime 'npm.cmd')
    $uvReady = (TestRuntime 'uv.exe') -and (TestRuntime 'uvx.exe')
    $unityReady = TestRuntime 'unity.exe'
    if ($Check) {
        if (-not ($nodeReady -and $uvReady -and $unityReady)) {
            throw "Missing/unusable prerequisites: Node 20+/npm=$nodeReady; uv/uvx=$uvReady; Unity CLI=$unityReady. Run without -Check to install."
        }
    } elseif (-not ($nodeReady -and $uvReady -and $unityReady)) {
        [Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor [Net.SecurityProtocolType]::Tls12
        $runtimeRoot = Join-Path $env:LOCALAPPDATA 'EmpireAtWar\mcp-runtimes'
        $tempRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
        $staging = Join-Path $tempRoot ('empire-mcp-' + [Guid]::NewGuid().ToString('N'))
        New-Item -ItemType Directory -Path $staging -Force | Out-Null
        try {
            if (-not $nodeReady) {
                Write-Host 'Installing Node.js 24 LTS and npm for the current user...'
                $arch = if ($env:PROCESSOR_ARCHITECTURE -eq 'ARM64' -or $env:PROCESSOR_ARCHITEW6432 -eq 'ARM64') { 'arm64' } else { 'x64' }
                $baseUrl = 'https://nodejs.org/dist/latest-v24.x'
                $sums = (Invoke-WebRequest -Uri "$baseUrl/SHASUMS256.txt" -UseBasicParsing).Content
                $pattern = '^([a-fA-F0-9]{64})\s+(node-v24\.\d+\.\d+-win-' + $arch + '\.zip)\s*$'
                $archiveMatch = [regex]::Match($sums, $pattern, [Text.RegularExpressions.RegexOptions]::Multiline)
                if (-not $archiveMatch.Success) { throw "Node.js checksum manifest has no Windows $arch archive." }
                $archiveName = $archiveMatch.Groups[2].Value
                $archive = Join-Path $staging $archiveName
                $nodeVersion = ($archiveName -split '-')[1]
                Invoke-WebRequest -Uri "https://nodejs.org/dist/$nodeVersion/$archiveName" -OutFile $archive -UseBasicParsing
                if ((Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash -ine $archiveMatch.Groups[1].Value) {
                    throw 'Node.js archive SHA-256 verification failed.'
                }
                New-Item -ItemType Directory -Path $runtimeRoot -Force | Out-Null
                Expand-Archive -LiteralPath $archive -DestinationPath $runtimeRoot -Force
                AddRuntimePath (Join-Path $runtimeRoot ([IO.Path]::GetFileNameWithoutExtension($archiveName)))
                if (-not ((TestRuntime 'node.exe' -Node) -and (TestRuntime 'npm.cmd'))) { throw 'Installed Node.js/npm failed verification.' }
            }
            if (-not $uvReady) {
                Write-Host 'Installing uv and uvx for the current user...'
                $uvDirectory = Join-Path $runtimeRoot 'uv'
                $previousUvDirectory = $env:UV_INSTALL_DIR
                $previousUvNoPath = $env:UV_NO_MODIFY_PATH
                try {
                    $env:UV_INSTALL_DIR = $uvDirectory
                    $env:UV_NO_MODIFY_PATH = '1'
                    InstallScript 'https://astral.sh/uv/install.ps1' (Join-Path $staging 'uv-install.ps1')
                } finally {
                    $env:UV_INSTALL_DIR = $previousUvDirectory
                    $env:UV_NO_MODIFY_PATH = $previousUvNoPath
                }
                AddRuntimePath $uvDirectory
                if (-not ((TestRuntime 'uv.exe') -and (TestRuntime 'uvx.exe'))) { throw 'Installed uv/uvx failed verification.' }
            }
            if (-not $unityReady) {
                Write-Host 'Installing the official Unity CLI for the current user...'
                InstallScript 'https://unity.com/install.ps1' (Join-Path $staging 'unity-install.ps1')
                $unityDirectory = if ($env:UNITY_CLI_HOME) { Join-Path $env:UNITY_CLI_HOME 'bin' } else { Join-Path $env:LOCALAPPDATA 'Unity\bin' }
                AddRuntimePath $unityDirectory
                if (-not (TestRuntime 'unity.exe')) { throw 'Installed Unity CLI failed verification.' }
            }
        } finally {
            $resolvedStaging = [IO.Path]::GetFullPath($staging)
            if (-not $resolvedStaging.StartsWith($tempRoot.TrimEnd('\') + '\', [StringComparison]::OrdinalIgnoreCase)) {
                throw 'Temporary cleanup path escaped the system temporary directory.'
            }
            Remove-Item -LiteralPath $resolvedStaging -Recurse -Force
        }
        Write-Host 'Runtimes installed. Restart Codex and other terminals to load the updated user PATH.'
    }

    if (-not $RuntimesOnly) {
        $setupArguments = @(Join-Path $PSScriptRoot 'setup-mcp.mjs')
        if ($Check) { $setupArguments += '--check' }
        $previousBootstrap = $env:EAW_MCP_BOOTSTRAPPED
        try {
            $env:EAW_MCP_BOOTSTRAPPED = '1'
            & node.exe @setupArguments
            if ($LASTEXITCODE -ne 0) { throw "MCP dependency setup failed (exit $LASTEXITCODE)." }
        } finally { $env:EAW_MCP_BOOTSTRAPPED = $previousBootstrap }
    }
} catch {
    Write-Error "MCP setup failed: $_" -ErrorAction Continue
    exit 1
}
