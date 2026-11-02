---
category: Tooling
status: todo
created: 2026-11-02
tags:
  - analytics
  - diagnostics
---
# Crash And Error Reporting

## Goal

- Collect native crashes, unhandled C# exceptions and `LogError`s from player builds with stack traces, build version and match context.
- See crash-free session rate per build before and after each release.
- Companion: [[TODOs/Features/Gameplay_Analytics|Gameplay Analytics]] (shares UGS project link + consent).

## Decision

- **Chosen (user-confirmed 2026-11-02):** Unity Diagnostics (built into Unity 6.2+; project is `6000.4.7f1`). No deprecated Cloud Diagnostics package.
- Why: zero extra SDK, Windows native crash + exception capture, symbol upload, same UGS dashboard/consent as Analytics.
- Fallback: Sentry Unity SDK if Diagnostics lacks needed context (breadcrumbs, custom tags, attachments) after Phase 2.
- Avoid: home-grown crash uploader; logging gameplay statistics through the crash channel.

## Current State (2026-11-02)

- `ProjectSettings.asset`: `cloudProjectId` empty, `companyName: DefaultCompany`, `bundleVersion: 0.1.0`.
- `Packages/manifest.json`: only `com.unity.modules.unityanalytics`; no `com.unity.services.*`.
- Existing dev telemetry (`BattleTelemetry`, `EnemyAiTelemetry`, `ShipOrderTelemetry`) is Editor/`DEVELOPMENT_BUILD` CSV only → [[Tooling/Battle_Telemetry]]. Keep separate.
- No `Application.logMessageReceived` hook in `Assets/Scripts`.

## Rules

- Release builds report; Editor never reports (avoid dashboard noise). Development builds report to a separate UGS environment (`development`).
- Respect player consent from [[TODOs/Features/Gameplay_Analytics|Gameplay Analytics]] Phase 1; no upload before consent is known.
- No personal data in reports: no machine name, user name, file paths with user folders, save names.
- Every build carries `Application.version` = `bundleVersion` + build number from Jenkins.

## Implementation

### Phase 0 — Project link (user + agent)

- [ ] User: create/link UGS project in Unity Cloud; set `cloudProjectId`; create `production` and `development` environments.
- [ ] Set real `companyName` (changes `LocalLow` path → also update [[Tooling/Battle_Telemetry]] Files path).
- [ ] Decide versioning scheme: `0.1.0+<jenkinsBuild>`.

### Phase 1 — Enable Diagnostics

- [ ] Enable Diagnostics in Project Settings → Services; verify Windows standalone support for current Mono/IL2CPP backend.
- [ ] Verify against current docs (Context7 `/websites/unity_en-us_services`): consent intent API for Diagnostics under `EndUserConsent`, exception sampling, log capture limits.
- [ ] Symbol upload for IL2CPP/native crashes in Jenkins build step → [[TODOs/Tooling/Jenkins_GitHub_Releases]].

### Phase 2 — Context

- [ ] Attach custom metadata: build version, `GameMode`, map/`PlanetType`, player faction, battle seconds, unit count.
- [ ] Source of context: thin `ICrashContext` service updated on battle start/end (reuses events from Gameplay Analytics Phase 2); no gameplay class references Diagnostics API directly.
- [ ] Deduplicate repeated per-frame `LogError` spam (rate limit per message hash) if Diagnostics does not.

### Phase 3 — Verification

- [ ] Dev build: forced exception (cheat/debug command) + forced native crash (`Utils.ForceCrash`) appear in `development` dashboard with symbols resolved.
- [ ] Release build without consent → nothing uploaded (proxy/network check).
- [ ] Document dashboard links + triage routine in canonical guidance note (`Tooling/Crash_Reporting`).

## Edge Cases

- Offline play → reports cached and sent next launch (verify Diagnostics behaviour).
- Crash during boot before consent load → treat as no consent; drop.
- Exceptions in `EditMode` tests must never upload (Editor excluded).

## Open Decisions

- Report from development builds? (default: yes, separate environment).
- Distribution channel (Steam/itch/standalone) — affects consent wording and symbol storage.

## TODO

- Implementation not started.
