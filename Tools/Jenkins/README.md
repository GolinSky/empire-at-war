# Local Windows builds

- Jenkins: `http://localhost:8080/job/EmpireAtWar-Windows-Local/`.
- Runs as the signed-in Windows user, bound only to `127.0.0.1:8080`.
- Admin credentials: `F:\Jenkins\admin-login.json`, restricted to the current user and SYSTEM. Never commit this file.
- Start: `powershell.exe -NoProfile -ExecutionPolicy Bypass -File F:\Jenkins\Start-Jenkins.ps1`.
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

- `ResourcePolicy.json`: provisional start RAM 10 GiB, stop RAM 2 GiB; disk reserve 20 GiB per used volume.
- Additional peak estimates: F: 50 GiB; C: 15 GiB. Baseline: source assets 2.21 GiB, development Library 8.01 GiB, user Unity cache 6.87 GiB.
- These are conservative initial estimates, not measured CI peaks. Refine after successful cold and warm runs.
- CI Library budget: 24 GiB; over-budget preparation fails. Review/rebuild only that cache while idle; automatic cache deletion is disabled.
- Preparation/build/package processes run in a Windows Job Object, assigned while suspended, with BelowNormal priority and kill-on-close; child processes inherit membership.
- Unity job workers: 4. Resource sampling: every 5 seconds. Build limit: 6,600 seconds; job limit: 120 minutes.
- Sampled group memory and process IDs are archived. Polling leaves a margin; it does not enforce a hard RAM/storage quota.
- No hard CPU percentage or committed-memory limit until successful measurements justify one.

## Artifacts and recovery

- Archives: entire `EmpireAtWar-Windows.zip`, Unity/CLI logs, commit SHA, CLI provenance, ZIP inventory/checksum, resource policy and measurements.
- Retention: 10 runs; artifacts from 3 runs. Retention limits counts, not total bytes.
- Player output and workspace ZIP are deleted only after Jenkins confirms archiving; archived files remain downloadable.
- `archive-pending.json` blocks a new snapshot when the only output copy still needs recovery. Recover/archive that output before removing the marker.
- Cancellation closes the monitor's Job Object and kills only its CI group. Preparation refuses to reuse a worktree with an active CI Editor or held Unity lock.
- Sign-in startup remains disabled until cold/warm builds, player navigation, cancellation, queued builds and resource rejection pass acceptance.

## Updating the installed job

1. Review and commit changes to this folder on `main`.
2. While Jenkins is idle, copy the committed tools to `F:\Jenkins\app\build-tools` and `Start-Jenkins.ps1` to `F:\Jenkins`.
3. Update the job's Pipeline script to the exact committed `Jenkinsfile` through its authenticated configuration UI/API.
4. Record the tools SHA in `F:\Jenkins\app\build-tools-commit.txt`. Restarting Jenkins does not automatically update the job.

`Configure-Jenkins.ps1` is a first-install bootstrap and refuses an existing configuration. It provisions authenticated access and the job through Jenkins' supported Groovy init hook instead of requiring manual setup-wizard clicks.
