[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$root = 'F:\Jenkins'
$homePath = "$root\data"
$installed = "$root\app\build-tools"
if (Test-Path -LiteralPath "$homePath\config.xml") { throw 'Existing Jenkins configuration requires an explicit update instead of bootstrap.' }
New-Item -ItemType Directory -Path $installed,"$homePath\init.groovy.d" -Force | Out-Null
Get-ChildItem -LiteralPath $PSScriptRoot -File | Copy-Item -Destination $installed
Copy-Item -LiteralPath "$PSScriptRoot\Start-Jenkins.ps1" -Destination "$root\Start-Jenkins.ps1"
Copy-Item -LiteralPath "$PSScriptRoot\Stop-Jenkins.ps1" -Destination "$root\Stop-Jenkins.ps1"
$credentialFile = "$root\admin-login.json"
if (-not (Test-Path -LiteralPath $credentialFile)) {
    $bytes = New-Object byte[] 32
    $random = [Security.Cryptography.RandomNumberGenerator]::Create()
    try { $random.GetBytes($bytes) } finally { $random.Dispose() }
    @{Username='admin';Password=[Convert]::ToBase64String($bytes);Url='http://localhost:8080/'} | ConvertTo-Json | Set-Content -LiteralPath $credentialFile -Encoding UTF8
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent().Name
    & icacls.exe $credentialFile /inheritance:r /grant:r "${identity}:(F)" 'SYSTEM:(F)' | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Could not restrict admin credential file.' }
}
$hook = @'
import jenkins.model.Jenkins
import jenkins.model.JenkinsLocationConfiguration
import jenkins.install.InstallState
import hudson.security.HudsonPrivateSecurityRealm
import hudson.security.FullControlOnceLoggedInAuthorizationStrategy
import groovy.json.JsonSlurper
import org.jenkinsci.plugins.workflow.job.WorkflowJob
import org.jenkinsci.plugins.workflow.cps.CpsFlowDefinition

def jenkins = Jenkins.get()
def marker = new File(jenkins.rootDir, 'local-bootstrap-completed')
if (marker.exists()) { return }
def credentials = new JsonSlurper().parse(new File('F:/Jenkins/admin-login.json'))
def realm = new HudsonPrivateSecurityRealm(false)
realm.createAccount(credentials.Username, credentials.Password)
jenkins.setSecurityRealm(realm)
def authorization = new FullControlOnceLoggedInAuthorizationStrategy()
authorization.setAllowAnonymousRead(false)
jenkins.setAuthorizationStrategy(authorization)
jenkins.setNumExecutors(1)
jenkins.setLabelString('windows-local-unity')
jenkins.setSlaveAgentPort(-1)
def location = JenkinsLocationConfiguration.get()
location.setUrl('http://localhost:8080/')
location.save()
def job = jenkins.createProject(WorkflowJob, 'EmpireAtWar-Windows-Local')
job.setDefinition(new CpsFlowDefinition(new File('F:/Jenkins/app/build-tools/Jenkinsfile').getText('UTF-8'), true))
job.setDescription('Builds the committed local main branch in F:/UnityCI/empire-at-war. Resource gates apply. No automated tests.')
job.save()
jenkins.setInstallState(InstallState.INITIAL_SETUP_COMPLETED)
jenkins.save()
marker.text = 'Initial local configuration completed.'
'@
[IO.File]::WriteAllText("$homePath\init.groovy.d\01-local-bootstrap.groovy", $hook, [Text.UTF8Encoding]::new($false))
Write-Host "Bootstrap prepared. Admin credentials: $credentialFile (restricted to this Windows user and SYSTEM)."
