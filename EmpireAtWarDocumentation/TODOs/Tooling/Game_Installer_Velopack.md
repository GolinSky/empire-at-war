---
category: Tooling
status: in-progress
created: 2026-11-08
---
# Game Installer (Velopack + GitHub Releases)

## Goal
- `Setup.exe` installs the Windows player; a launcher checks GitHub Releases, downloads the update (delta when possible) and replaces game files.
- Saves, settings and telemetry survive every update and reinstall.
- Reuse the existing Jenkins publish job ([[TODOs/Tooling/Jenkins_GitHub_Releases|Jenkins GitHub Releases]]); no new hosting.

## Decision
- **Chosen:** Velopack (`vpk` CLI + `Velopack` NuGet) with `GithubSource` on `GolinSky/empire-at-war`.
  - Why: free/OSS, built-in GitHub Releases source, Setup.exe, zstd deltas, per-user install (no admin), handles locked files.
- **Chosen:** separate small .NET launcher `EmpireAtWarLauncher.exe` as `--mainExe`; the Unity player stays unchanged.
  - Why: Velopack runs mainExe with `--veloapp-install/updated/obsolete/uninstall` and kills it after 15–30 s; a Unity IL2CPP player cannot handle these hooks and would start the full game.
  - Avoid: Velopack SDK inside Unity (IL2CPP compatibility unverified, hook timing).
- **Avoid:** Inno Setup/NSIS + custom updater (we would build download/replace/rollback ourselves); Steam-style patchers (paid/hosting).

## Verified facts (2026-11-08)
- Repo `GolinSky/empire-at-war` is **public** → `GithubSource` reads releases without a token in the client.
- Install layout: `%LocalAppData%\EmpireAtWar\current\` (replaced on each update) + `Update.exe` + root exe stub. Anything inside `current` is deleted on update.
- Game data uses `Application.persistentDataPath` (`ProjectScope.cs:104-106` settings/saves, telemetry) → `%USERPROFILE%\AppData\LocalLow\GolinSkyCompany\<productName>`, outside the install root → survives updates and uninstall.
- Updates only see **published** releases; Jenkins creates drafts → manual GitHub publish remains the release gate.
- `GithubSource(prerelease: false)` ignores prereleases; Jenkins `PRERELEASE` defaults to `true` → must be resolved (see TODO).
- Build 37 ZIP: 1.07 GiB (`0.1.37`). Full `.nupkg` ≈ same size; GitHub asset limit 2 GiB; zstd delta limit 2 GB per file.
- Versions are SemVer2 (`0.1.37`, `0.2.37-beta.1`) → valid for `vpk pack -v` (tag without `v`).

## Implementation
### 1. Launcher (`Tools/Launcher/`, .NET 8, self-contained single file)
```csharp
VelopackApp.Build().Run();                 // must be first: handles hooks and exits
var mgr = new UpdateManager(new GithubSource(
    "https://github.com/GolinSky/empire-at-war", accessToken: null, prerelease: false));
var update = await mgr.CheckForUpdatesAsync();   // null → up to date
if (update != null) {
    await mgr.DownloadUpdatesAsync(update, p => ShowProgress(p));
    mgr.ApplyUpdatesAndRestart(update);           // restarts launcher on new version
}
Process.Start(Path.Combine(VelopackLocator.Current.AppContentDir, "EmpireAtWar.exe"));
```
- Offline / GitHub error → log and launch the installed game; never block play.
- UI: minimal window (version, progress bar, Play/Skip). Writes its log to persistentDataPath-equivalent, never into `current`.
- `IsInstalled == false` (run from ZIP/dev) → skip update check.

### 2. Jenkins publish job (`Jenkinsfile.Publish`, `Publish-GitHubRelease.ps1`)
1. After `-ValidateOnly`: extract archived `EmpireAtWar-Windows.zip` → `staging\`; copy launcher publish output into `staging\`.
2. `vpk download github --repoUrl … --token $GH_TOKEN -o Releases` (previous full package → deltas).
3. `vpk pack -u EmpireAtWar -v <tag without v> -p staging -e EmpireAtWarLauncher.exe --packTitle "Empire at War" -o Releases`.
4. Upload `Releases\` outputs (`EmpireAtWar-win-Setup.exe`, `*-full.nupkg`, `*-delta.nupkg`, `releases.win.json`, `RELEASES`, portable zip) through the existing verified-draft uploader (extend asset list + size/SHA-256 checks). Avoid `vpk upload github` so resume/verification logic stays single-owner.
5. Install pinned `vpk` + .NET 8 SDK on the build machine; record versions/SHA-256 in `F:\Jenkins\app\release-installation.json`.

### 3. Release flow (unchanged gate)
- Build → publish job draft → user verifies → **Publish** on GitHub → launchers pick it up.

## Edge Cases
- Game running during update → Velopack kills processes in `current`; launcher applies only before starting the game.
- Antivirus locks `current` → Velopack dialog / old version launches.
- Unsigned binaries → SmartScreen "Unknown publisher" on `Setup.exe`. Code signing (`vpk pack --signParams`) is optional, needs a certificate.
- First Velopack release has no delta (full download ~1.1 GB); later ones delta from previous.
- Any future code writing next to the exe (`Application.dataPath`) loses data on update → keep all writes in persistentDataPath.

## Progress (2026-11-08)
- Decisions: launcher uses `prerelease: true` (Jenkins `PRERELEASE` default unchanged); raw `EmpireAtWar-Windows.zip` dropped from release assets → Velopack Portable zip.
- Deviation: launcher targets `net10.0-windows` (installed SDK `10.0.103`, LTS) instead of .NET 8.
- Pinned: `Velopack` NuGet + `vpk` CLI `1.2.161`; `vpk` at `F:\Jenkins\app\vpk\vpk.exe`, recorded in `F:\Jenkins\app\release-installation.json`.
- Files: `Tools/Launcher/` (`Program.cs`, `LauncherForm.cs`, `LauncherLog.cs`, csproj, README); `Tools/Jenkins/New-VelopackRelease.ps1`; edits to `Publish-GitHubRelease.ps1`, `Jenkinsfile.Publish` (timeout 60 → 180 min), `Configure-GitHubPublish.ps1`, `README.md` (*Velopack installer and launcher*).
- Pack flow: launcher built from the **source commit** (`git archive`) → player ZIP + launcher → newest *published* `*-full.nupkg` as delta base (anonymous, size+SHA-256 checked) → `vpk pack --runtime win-x64` → `releases.win.json` filtered to this version (GithubSource resolves entries inside the hosting release). Cache `F:\Jenkins\velopack-cache\<tag>` keeps resumed uploads byte-identical; deleted after `VerifiedDraft`.
- Verified: launcher `dotnet build`/single-file publish (~50 MB, 0 warnings); `vpk pack` smoke test incl. delta from a lone previous full nupkg; dry run of `New-VelopackRelease.ps1` (scratch repo + dummy player) → 4 assets, cache reuse; all scripts parse in Windows PowerShell 5.1. No GitHub upload, Jenkins job install or install/update run yet.
- Edge: builds before the launcher commit (e.g. build 37) cannot be published — `New-VelopackRelease.ps1` rejects commits without `Tools/Launcher`.

## TODO
- [x] Decide prerelease policy → launcher accepts prereleases.
- [x] Decide raw ZIP → dropped; Velopack Portable zip replaces it.
- [x] Add Jenkins credential `github-releases` (user, verified present 2026-11-08).
- [x] Launcher project + Velopack NuGet `1.2.161`.
- [x] Jenkins pack/upload stages + tool installation record.
- [x] Commit `Tools/Launcher` + `Tools/Jenkins` (`b532d9bad`, 2026-11-08; root `.gitignore` ignores `*.csproj` → `Tools/Launcher/.gitignore` re-includes `EmpireAtWarLauncher.csproj`). `Configure-GitHubPublish.ps1` installed tools from `b532d9bad`; both Pipelines validated; installed files match the commit.
- [x] Push `b532d9bad` (pushed to `origin/main` 2026-11-08).
- Publish run #1 failed: parameter fields held `SOURCE_BUILD=38` (value only, e.g. `39`). Run #2 (build 39) failed: provenance `source.dirty` is always `true` (CI bundleVersion stamp + Unity reserialization) → check removed in `f116bba62`, job reinstalled; cleanliness stays enforced by `Prepare-Snapshot.ps1:55`. Build 38 predates the launcher; use build 39+.
- Run #3: pack OK (Setup.exe 1.18 GB, full nupkg 1.18 GB, Portable.zip 1.18 GB, `releases.win.json`); failed `Created draft could not be uniquely resolved` — GitHub release list lags after `gh release create` → retry lookup 12 × 5 s (`5217ec438`). Run #4 resumed the same draft → `VerifiedDraft` 2026-11-08 (`v0.1.39`, draft, prerelease). Remaining: user publishes draft + install/update acceptance.
- 2026-11-08 update 0.1.39 → 0.1.40 failed: `Could not find asset 'EmpireAtWar-0.1.40-full.nupkg' in GitHub Release 'v0.1.40'`. Cause: a second release per tag was created/published from the tag page (drafts live at `untagged-…`); `gh release upload <tag>` then sent `releases.win.json` + small files into it (run #5 failed `Uploaded asset not uniquely found`). Fix `8e60c8706`: `Assert-OnlyReleaseForTag` before each upload; README publish step warns. GitHub cleanup (user): delete public duplicates 438521452 (v0.1.39, empty) and 438529637 (v0.1.40, partial), keep tags; rerun publish build 40 to resume draft 438527115; publish drafts via Edit.
- 2026-11-08 resolved: duplicates deleted; drafts published by user; missing v0.1.40 small assets uploaded manually (SHA-256 match run #5 report). **Update 0.1.39 → 0.1.40 verified**: launcher log `Downloading 0.1.40` → `Applying 0.1.40` (~52 s) → `Up to date: 0.1.40`; `current/sq.version` = 0.1.40. Still open: saves kept (user check), delta update on next release, offline launch, uninstall.
- [ ] Build a new player from a commit containing the launcher, then run the publish job.
- [ ] Acceptance: fresh install via Setup.exe; update N → N+1 keeps saves/settings; delta used on N+1 → N+2; offline launch; uninstall keeps LocalLow data.
