[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$baseUrl = 'http://127.0.0.1:8080/'
$app = 'F:\Jenkins\app'
$installed = "$app\build-tools"
$sourceJob = 'EmpireAtWar-Windows-Local'
$publishJob = 'EmpireAtWar-Publish-GitHub'
$login = Get-Content -LiteralPath 'F:\Jenkins\admin-login.json' -Raw | ConvertFrom-Json
$headers = @{ Authorization='Basic ' + [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes("$($login.Username):$($login.Password)")) }
$session = New-Object Microsoft.PowerShell.Commands.WebRequestSession

function Invoke-JenkinsRequest {
    param([string]$Path, [string]$Method = 'GET', $Body, [string]$ContentType = 'application/x-www-form-urlencoded')
    $request = @{ Uri="$baseUrl$Path"; Method=$Method; Headers=$headers; WebSession=$session; UseBasicParsing=$true }
    if ($null -ne $Body) { $request.Body=$Body; $request.ContentType=$ContentType }
    try { return Invoke-WebRequest @request } catch {
        throw "Jenkins request failed: $Method $Path. Check that Jenkins is running and the local admin login is valid."
    }
}

function Read-JobXml {
    param([string]$Content)
    # Jenkins writes XML 1.1; .NET's parser supports XML 1.0. Job text uses no XML 1.1-only characters.
    return [xml]($Content -replace "<\?xml version=['`"]1\.1['`"]", '<?xml version="1.0"')
}

$crumb = (Invoke-JenkinsRequest 'crumbIssuer/api/json').Content | ConvertFrom-Json
$headers[$crumb.crumbRequestField] = $crumb.crumb
$computers = (Invoke-JenkinsRequest 'computer/api/json?tree=busyExecutors').Content | ConvertFrom-Json
$queue = (Invoke-JenkinsRequest 'queue/api/json?tree=items[id]').Content | ConvertFrom-Json
if ($null -eq $computers.busyExecutors -or $computers.busyExecutors -ne 0 -or @($queue.items).Count -ne 0) { throw 'Configure publishing only while Jenkins has no running or queued builds.' }
$plugins = ((Invoke-JenkinsRequest 'pluginManager/api/json?tree=plugins[shortName,version,active]').Content | ConvertFrom-Json).plugins
foreach ($name in @('copyartifact','pipeline-build-step','credentials-binding','plain-credentials')) {
    if (-not @($plugins | Where-Object { $_.shortName -eq $name -and $_.active }).Count) { throw "Required plugin is not active: $name" }
}
if (-not (Test-Path -LiteralPath "$app\gh\bin\gh.exe" -PathType Leaf)) { throw 'Install GitHub CLI before configuring publishing.' }

$buildPipeline = Get-Content -LiteralPath "$PSScriptRoot\Jenkinsfile" -Raw
$publishPipeline = Get-Content -LiteralPath "$PSScriptRoot\Jenkinsfile.Publish" -Raw
foreach ($pipeline in @($buildPipeline, $publishPipeline)) {
    $validation = (Invoke-JenkinsRequest 'pipeline-model-converter/validate' -Method POST -Body @{jenkinsfile=$pipeline}).Content
    if ($validation -notmatch 'Jenkinsfile successfully validated') { throw "Pipeline validation failed: $validation" }
    Write-Host $validation.Trim()
}
$sourceResponse = Invoke-JenkinsRequest "job/$sourceJob/config.xml"
$sourceXml = Read-JobXml $sourceResponse.Content
$sourceScript = $sourceXml.SelectSingleNode('/flow-definition/definition/script')
$normalize = { param($value) ($value -replace '\r\n', "`n").Trim() }
$withoutPermission = $buildPipeline -replace "(?m)^\s*copyArtifactPermission\('EmpireAtWar-Publish-GitHub'\)\r?\n", ''
if ((& $normalize $sourceScript.InnerText) -cne (& $normalize $withoutPermission) -and (& $normalize $sourceScript.InnerText) -cne (& $normalize $buildPipeline)) {
    throw 'Installed build Pipeline differs beyond the publishing permission. Reconcile it before updating.'
}
$jobs = ((Invoke-JenkinsRequest 'api/json?tree=jobs[name]').Content | ConvertFrom-Json).jobs
$publishExists = @($jobs | Where-Object { $_.name -eq $publishJob }).Count -eq 1
if (-not $publishExists) {
    $publishXml = [xml]'<flow-definition><actions/><description/><keepDependencies>false</keepDependencies><properties/><definition class="org.jenkinsci.plugins.workflow.cps.CpsFlowDefinition"><script/><sandbox>true</sandbox></definition><triggers/><disabled>false</disabled></flow-definition>'
} else {
    $publishResponse = Invoke-JenkinsRequest "job/$publishJob/config.xml"
    $publishXml = Read-JobXml $publishResponse.Content
    if ($publishXml.SelectSingleNode('/flow-definition/definition').GetAttribute('class') -ne 'org.jenkinsci.plugins.workflow.cps.CpsFlowDefinition') { throw 'Existing publishing job is not an inline Pipeline; refusing to replace it.' }
}
$backup = Join-Path "$app\release-config-backups" (Get-Date -Format 'yyyyMMdd-HHmmss-fff')
New-Item -ItemType Directory -Path $backup -Force | Out-Null
[IO.File]::WriteAllText("$backup\$sourceJob.xml", $sourceResponse.Content, [Text.UTF8Encoding]::new($false))
if ($publishExists) { [IO.File]::WriteAllText("$backup\$publishJob.xml", $publishResponse.Content, [Text.UTF8Encoding]::new($false)) }

$sourceScript.InnerText = $buildPipeline
$permission = $sourceXml.SelectSingleNode('/flow-definition/properties/hudson.plugins.copyartifact.CopyArtifactPermissionProperty')
if ($null -eq $permission) {
    $permission = $sourceXml.CreateElement('hudson.plugins.copyartifact.CopyArtifactPermissionProperty')
    $permission.InnerXml = '<projectNameList/>'
    [void]$sourceXml.SelectSingleNode('/flow-definition/properties').AppendChild($permission)
}
$allowed = $permission.SelectSingleNode('projectNameList')
if (@($allowed.SelectNodes('string') | Where-Object { $_.InnerText -eq $publishJob }).Count -eq 0) {
    $entry = $sourceXml.CreateElement('string'); $entry.InnerText = $publishJob
    [void]$allowed.AppendChild($entry)
}
$publishXml.SelectSingleNode('/flow-definition/definition/script').InnerText = $publishPipeline
$publishXml.SelectSingleNode('/flow-definition/definition/sandbox').InnerText = 'true'
$publishXml.SelectSingleNode('/flow-definition/description').InnerText = 'Uploads a selected successful EmpireAtWar-Windows-Local archive to a verified GitHub draft. Requires Secret Text credential github-releases. Never builds Unity or publishes the draft.'
$propertyXml = @'
<properties>
  <hudson.model.ParametersDefinitionProperty>
    <parameterDefinitions>
      <hudson.model.StringParameterDefinition><name>SOURCE_BUILD</name><description>Successful EmpireAtWar-Windows-Local build number with retained artifacts.</description><defaultValue/><trim>true</trim></hudson.model.StringParameterDefinition>
      <hudson.model.StringParameterDefinition><name>RELEASE_TAG</name><description>vMAJOR.MINOR.PATCH, optionally with a prerelease suffix. Always creates a draft.</description><defaultValue/><trim>true</trim></hudson.model.StringParameterDefinition>
      <hudson.model.BooleanParameterDefinition><name>PRERELEASE</name><description>Mark the draft as a prerelease.</description><defaultValue>true</defaultValue></hudson.model.BooleanParameterDefinition>
    </parameterDefinitions>
  </hudson.model.ParametersDefinitionProperty>
  <org.jenkinsci.plugins.workflow.job.properties.DisableConcurrentBuildsJobProperty><abortPrevious>false</abortPrevious></org.jenkinsci.plugins.workflow.job.properties.DisableConcurrentBuildsJobProperty>
  <jenkins.model.BuildDiscarderProperty><strategy class="hudson.tasks.LogRotator"><daysToKeep>-1</daysToKeep><numToKeep>20</numToKeep><artifactDaysToKeep>-1</artifactDaysToKeep><artifactNumToKeep>-1</artifactNumToKeep><removeLastBuild>false</removeLastBuild></strategy></jenkins.model.BuildDiscarderProperty>
</properties>
'@
$properties = [xml]$propertyXml
foreach ($property in $properties.DocumentElement.ChildNodes) {
    $parent = $publishXml.SelectSingleNode('/flow-definition/properties')
    $previous = $parent.SelectSingleNode($property.Name)
    if ($null -ne $previous) { [void]$parent.RemoveChild($previous) }
    [void]$parent.AppendChild($publishXml.ImportNode($property, $true))
}
$files = @('Jenkinsfile','Jenkinsfile.Publish','Publish-GitHubRelease.ps1','Configure-GitHubPublish.ps1')
foreach ($name in $files) { Copy-Item -LiteralPath "$PSScriptRoot\$name" -Destination "$installed\$name" -Force }
if (-not $publishExists) { $publishPath = "createItem?name=$publishJob" } else { $publishPath = "job/$publishJob/config.xml" }
[void](Invoke-JenkinsRequest $publishPath -Method POST -Body $publishXml.OuterXml -ContentType 'application/xml; charset=utf-8')
[void](Invoke-JenkinsRequest "job/$sourceJob/config.xml" -Method POST -Body $sourceXml.OuterXml -ContentType 'application/xml; charset=utf-8')

$sourceReadback = Read-JobXml (Invoke-JenkinsRequest "job/$sourceJob/config.xml").Content
$publishReadback = Read-JobXml (Invoke-JenkinsRequest "job/$publishJob/config.xml").Content
if ($sourceReadback.SelectSingleNode('/flow-definition/definition/script').InnerText -cne $buildPipeline -or $publishReadback.SelectSingleNode('/flow-definition/definition/script').InnerText -cne $publishPipeline) { throw 'Installed Pipeline readback does not match source.' }
if ($sourceReadback.SelectSingleNode("/flow-definition/properties/hudson.plugins.copyartifact.CopyArtifactPermissionProperty/projectNameList/string[text()='$publishJob']") -eq $null) { throw 'Artifact-copy permission was not saved.' }
if ($sourceReadback.SelectSingleNode('/flow-definition/properties/jenkins.model.BuildDiscarderProperty').OuterXml -cne $sourceXml.SelectSingleNode('/flow-definition/properties/jenkins.model.BuildDiscarderProperty').OuterXml) { throw 'Source build retention changed unexpectedly.' }
$parameterPath = '/flow-definition/properties/hudson.model.ParametersDefinitionProperty/parameterDefinitions'
$expectedParameters = $publishXml.SelectSingleNode($parameterPath).ChildNodes
$actualParameters = $publishReadback.SelectSingleNode($parameterPath).ChildNodes
if ($actualParameters.Count -ne $expectedParameters.Count) { throw 'Publishing parameter count mismatch.' }
for ($index = 0; $index -lt $expectedParameters.Count; $index++) {
    $expected = $expectedParameters[$index]; $actual = $actualParameters[$index]
    if ($actual.Name -cne $expected.Name) { throw 'Publishing parameter type mismatch.' }
    foreach ($field in @('name','description','defaultValue','trim')) {
        if ([string]$actual.$field -cne [string]$expected.$field) { throw "Publishing parameter readback mismatch: $($expected.name).$field" }
    }
}
if ($publishReadback.SelectSingleNode('/flow-definition/properties/org.jenkinsci.plugins.workflow.job.properties.DisableConcurrentBuildsJobProperty/abortPrevious').InnerText -ne 'false') { throw 'Concurrent publishing configuration was not saved.' }
$fileHashes = @(foreach ($name in $files) {
    $sourceHash = (Get-FileHash -LiteralPath "$PSScriptRoot\$name" -Algorithm SHA256).Hash
    $installedHash = (Get-FileHash -LiteralPath "$installed\$name" -Algorithm SHA256).Hash
    if ($sourceHash -ne $installedHash) { throw "Installed hash mismatch: $name" }
    @{File=$name; SHA256=$sourceHash}
})
$sourceCommit = (& git -C $PSScriptRoot rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0) { throw 'Could not record source revision.' }
$sourceChanges = @(& git -C $PSScriptRoot status --porcelain -- Jenkinsfile Jenkinsfile.Publish Publish-GitHubRelease.ps1 Configure-GitHubPublish.ps1)
if ($LASTEXITCODE -ne 0) { throw 'Could not record source working-tree state.' }
@{
    JobUrl="${baseUrl}job/$publishJob/"; InstalledAtUtc=[DateTime]::UtcNow.ToString('o'); SourceCommit=$sourceCommit
    SourceWorkingTreeChanges=$sourceChanges; Files=$fileHashes; PipelineValidation='Passed'; ConfigurationReadback='Passed'
    BackupDirectory=$backup; LiveUploadVerified=$false; Pending='User adds github-releases credential and selects source build/tag.'
} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath "$app\release-tools-installation.json" -Encoding UTF8
Write-Host "Configured and verified: ${baseUrl}job/$publishJob/"
Write-Host 'Next: add Secret Text credential github-releases (repository-scoped Contents read/write). No build or upload was started.'
