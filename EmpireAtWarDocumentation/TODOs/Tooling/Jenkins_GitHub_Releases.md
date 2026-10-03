---
category: Tooling
status: in-progress
---
# Jenkins GitHub Releases

## Goal

- Upload an explicitly selected successful Jenkins Windows build to a draft release in `GolinSky/empire-at-war`.
- Build once → verify archived bytes → upload the same ZIP; manual publication on GitHub.

## Decision

- Job: `EmpireAtWar-Publish-GitHub`; source: `EmpireAtWar-Windows-Local`.
- Parameters: required numeric `SOURCE_BUILD`, required `RELEASE_TAG` (`vMAJOR.MINOR.PATCH` with optional prerelease suffix), `PRERELEASE=true` by default.
- Always draft. No installer, version injection, signing, auto-update, or automatic Git push.
- User supplies Jenkins Secret Text credential `github-releases`; repository-scoped Contents read/write. Never put the token in notes or source.

## Implementation

- Install pinned official GitHub CLI and Copy Artifact plugin; record versions/checksums.
- Require source run SUCCESS; copy ZIP, commit, checksum, and Unity provenance using Copy Artifact.
- Validate source SHA/provenance/checksum and asset size <2 GiB before GitHub changes.
- Tag the archived commit; reject mismatches and published releases. Resume only matching drafts and verified assets.
- Upload ZIP, commit.txt, zip-sha256.json; verify remote sizes/digests and record release URL.
- Preserve build retention; mark release candidates Keep forever before artifacts expire.

## TODO

- [x] Implement scripts, Pipeline, and targeted authenticated job configuration.
- [x] Install prerequisites and configure/read back live Jenkins job.
- [x] Parse PowerShell and validate declarative Pipeline; verify installed hashes.
- [ ] User adds credential and selects build/tag; verify first live draft upload.
- [ ] Verify upload failures, resume, and repeated execution when explicitly requested; no automated tests run by default.

## Important Values

- Installed 2026-10-03: GitHub CLI `2.102.0`; Copy Artifact `805.v9d1393350780`, active in Production mode; existing required dependencies reused.
- Jenkins `2.568.3` is running at `127.0.0.1:8080`. Publish job is buildable, parameters are present, `PRERELEASE=true`; no runs started.
- Windows PowerShell 5.1 parsed both scripts. Jenkins validated both Pipelines; authenticated readback verified scripts, parameters, copy permission, concurrency, and unchanged source retention.
- Installed file SHA-256 values match source. Deployment records the source revision plus uncommitted paths/hashes; no commit was created for this task.
- Evidence: `F:/Jenkins/app/release-installation.json`, `release-tools-installation.json`, `release-verification.json`.
- No Unity build, automated test, tag creation, or GitHub upload ran. Live upload/recovery scenarios remain unverified; retain this plan as in-progress.

## Files

- `F:/Private/empire-at-war/Tools/Jenkins` — versioned scripts and operator README.
- `F:/Jenkins/app` — installed dependencies, tools, and verification records.
- [Source plan](https://drive.google.com/file/d/1DxdN0XIkN6ZAlnkvMaleSt8ADA0QvU-p/view?usp=sharing).
