# Local Windows builds

- Jenkins: `http://localhost:8080/job/EmpireAtWar-Windows-Local/`.
- Runs as the signed-in Windows user, bound only to `127.0.0.1:8080`.
- Admin credentials: `F:\Jenkins\admin-login.json`, restricted to the current user and SYSTEM. Never commit this file.
- Start: `powershell.exe -NoProfile -ExecutionPolicy Bypass -File F:\Jenkins\Start-Jenkins.ps1`.
- Stop: `powershell.exe -NoProfile -ExecutionPolicy Bypass -File F:\Jenkins\Stop-Jenkins.ps1`. Use while builds are idle; this terminates the Jenkins Java process, verifies its exit, and removes the PID file.
- Runtime: `F:\Jenkins\app`; home: `F:\Jenkins\data`; startup logs: `F:\Jenkins\logs`.
- Installation versions and SHA256 hashes: `F:\Jenkins\app\installation.json`.

## Snapshot and build

- Each run pins `refs/heads/main` once; development edits stay outside that snapshot.
- CI uses the dedicated detached worktree `F:\UnityCI\empire-at-war` and its own persistent `Library`.
- The worktree must share the source Git directory, remain detached, and contain no reparse points. Cleanup rejects other roots and active CI Editors.
- Submodules and external `file:` packages are rejected. Git LFS hydration and integrity checks precede the build.
- Unity CLI reads the committed Editor version; builds Windows x64 using the shared scene list and existing Mono backend.
- Addressables must use committed `BuildWithPlayer`. The installed 2.9.1 player processor throws on content-build errors.
- No automated test stage. Acceptance requires cold/warm builds and a manual extracted-player check.

## Resource policy

- `ResourcePolicy.json`: disk reserve 20 GiB per used volume. Available RAM is recorded; RAM start/stop restrictions are disabled by user request.
- Additional peak estimates: F: 50 GiB; C: 15 GiB. Baseline: source assets 2.21 GiB, development Library 8.01 GiB, user Unity cache 6.87 GiB.
- These are conservative initial estimates, not measured CI peaks. Refine after successful cold and warm runs.
- CI Library budget: 24 GiB; over-budget preparation fails. Review/rebuild only that cache while idle; automatic cache deletion is disabled.
- Preparation/build/package processes run in a Windows Job Object, assigned while suspended, with BelowNormal priority and kill-on-close; child processes inherit membership.
- Unity job workers: 4. Resource sampling: every 5 seconds. Build limit: 6,600 seconds; job limit: 120 minutes.
- Sampled group memory and process IDs are archived. Polling leaves a margin; it does not enforce a hard RAM/storage quota.
- No hard CPU percentage or committed-memory limit until successful measurements justify one.

## Artifacts and recovery

- Archives: entire `EmpireAtWar-Windows.zip`, Unity/CLI logs, commit SHA, CLI provenance, ZIP inventory/checksum, resource policy and measurements.
- Packaging uses .NET `ZipFile` with player files at the archive root; Windows' ZIP handler must recognize `EmpireAtWar.exe` before archiving. `tar -C player .` produced ZIPs that Windows Explorer could not extract.
- Retention: 10 runs; artifacts from 3 runs. Retention limits counts, not total bytes.
- Player output and workspace ZIP are deleted only after Jenkins confirms archiving; archived files remain downloadable.
- A complete player without its ZIP is preserved if packaging fails. Archival monitors disk reserve and records RAM without blocking failure-log recovery on low memory.
- `archive-pending.json` blocks a new snapshot when the only output copy still needs recovery. Recover/archive that output before removing the marker.
- Cancellation closes the monitor's Job Object and kills only its CI group. Preparation refuses to reuse a worktree with an active CI Editor or held Unity lock.
- Sign-in startup remains disabled until cold/warm builds, player navigation, cancellation, queued builds and resource rejection pass acceptance.

## Updating the installed job

1. Review and commit changes to this folder on `main`.
2. While Jenkins is idle, copy the committed tools to `F:\Jenkins\app\build-tools` and `Start-Jenkins.ps1` / `Stop-Jenkins.ps1` to `F:\Jenkins`.
3. Update the job's Pipeline script to the exact committed `Jenkinsfile` through its authenticated configuration UI/API.
4. Record the tools SHA in `F:\Jenkins\app\build-tools-commit.txt`. Restarting Jenkins does not automatically update the job.

`Configure-Jenkins.ps1` is a first-install bootstrap and refuses an existing configuration. It provisions authenticated access and the job through Jenkins' supported Groovy init hook instead of requiring manual setup-wizard clicks.

## GitHub draft releases

- Publish job: [EmpireAtWar-Publish-GitHub](http://localhost:8080/job/EmpireAtWar-Publish-GitHub/).
- Destination: `GolinSky/empire-at-war`. Always a draft; review and publish manually on GitHub.
- Copies the selected successful build's archived ZIP and metadata. Never rebuilds Unity or pushes local commits.
- Workspace: Jenkins assigns the separate `EmpireAtWar-Publish-GitHub` workspace. Copied files are removed after the run; original build archives are unchanged.
- Installed dependencies: GitHub CLI `2.102.0` at `F:\Jenkins\app\gh\bin\gh.exe`; Copy Artifact `805.v9d1393350780` in Production mode. Versions, official download URLs, and SHA-256 values: `F:\Jenkins\app\release-installation.json`.
- Build retention remains 10 run records / artifacts from 3 runs. Mark a release candidate **Keep this build forever** before its artifacts expire.

### Add the credential once

1. Create a fine-grained GitHub token restricted to `GolinSky/empire-at-war`, with repository **Contents: Read and write** permission.
2. Open Jenkins → Manage Jenkins → Credentials → System → Global credentials → Add Credentials.
3. Choose **Secret text**, set ID to `github-releases`, and enter the token in the Secret field. Do not put it in source, parameters, build files, or chat.

`GH_TOKEN` is bound only during GitHub operations. Missing/expired credentials fail the job; no interactive CLI login is used.

### Create a draft

1. Download and manually verify a retained successful build from `EmpireAtWar-Windows-Local`.
2. Open the publish job → **Build with Parameters**.
3. Enter `SOURCE_BUILD` (a positive build number, never `latest`) and `RELEASE_TAG` (for example `v0.1.0` or `v0.1.0-beta.1`). `PRERELEASE` defaults to `true`.
4. Run the job. Follow the release URL in its description, console, or archived `release-url.txt`.
5. Confirm `release-result.json` reports `VerifiedDraft`, inspect the draft on GitHub, then publish manually when ready.

Before changing GitHub, the job requires SUCCESS, all four archived inputs, matching clean Windows build provenance/commit, the ZIP's archived SHA-256, and nonempty assets below 2 GiB. The archived commit must already exist in GitHub; push the intended commit separately if needed.

- Uploaded assets: `EmpireAtWar-Windows.zip`, `commit.txt`, `zip-sha256.json`. Unity provenance is used for validation and remains in Jenkins.
- The tag resolves to the archived commit. An existing tag pointing elsewhere is rejected, including annotated tags after dereferencing.
- Draft notes identify the source build, commit, and ZIP hash. Keep the embedded provenance marker when editing notes if you intend to resume the job.
- Uploaded asset names, sizes, states, and SHA-256 digests must match before the job succeeds.
- The release tag does not change the already-built player's internal version or ZIP filename.

### Resume and troubleshoot

- Retry with the same build, tag, and prerelease setting. A matching draft is reused; verified assets are skipped and missing assets are uploaded.
- Published releases, conflicting source markers/tags, and mismatched/incomplete assets are never overwritten or deleted. Inspect the draft and resolve conflicts manually before retrying.
- Expired artifacts fail explicitly; the job does not substitute a newer build. Preserve the source build while publishing or recovering an upload.
- A failed upload leaves its tag/draft on GitHub. The archived report includes the release URL once resolved and the failure reason; failures before credential binding can leave the earlier `ArtifactsValidated` report.
- Job timeout: 60 minutes; concurrent publish runs queue. The existing single Windows executor serializes copying/uploading with Unity builds.

### Update the publishing job

1. Review changes in `Tools/Jenkins/Jenkinsfile`, `Jenkinsfile.Publish`, `Publish-GitHubRelease.ps1`, and `Configure-GitHubPublish.ps1`.
2. With Jenkins running and no running or queued builds, run from the source checkout:

   ```powershell
   powershell.exe -NoProfile -ExecutionPolicy Bypass -File F:\Private\empire-at-war\Tools\Jenkins\Configure-GitHubPublish.ps1
   ```

3. The script validates both Pipelines, backs up the affected job XML under `F:\Jenkins\app\release-config-backups`, installs the four reviewed files, and updates only the two jobs through the authenticated API.
4. Verify `F:\Jenkins\app\release-tools-installation.json`: source commit/working-tree changes, exact file hashes, Pipeline validation, and configuration readback. This supplemental record does not replace the base build-tools commit marker.

Configuration does not create a GitHub token or start any build/upload. Initial verification covers PowerShell parsing and Jenkins configuration; live upload and recovery acceptance remain pending until a credential and source build/tag are supplied. No automated tests are run by setup.
