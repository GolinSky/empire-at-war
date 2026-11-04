---
category: Tooling
status: in-progress
created: 2026-11-02
tags:
  - analytics
  - diagnostics
---
# Crash And Error Reporting

## Goal

- Collect native crashes, managed exceptions and error logs from player builds with build identity and battle context.
- Review crash-free sessions by build before and after releases.
- Companion → [[TODOs/Features/Gameplay_Analytics]]. Guidance → [[Tooling/Crash_Reporting]].

## Decision

- **User-confirmed 2026-11-02:** keep built-in Unity Diagnostics; diagnostic collection is independent of the gameplay statistics toggle.
- Diagnostics does not use `EndUserConsent`. Analytics retains opt-in; diagnostics can collect before the player answers and after they decline.
- No deprecated Cloud Diagnostics package, Sentry SDK or custom upload transport.
- Preserve release version semantics: `Application.version` remains `bundleVersion`; attach existing `BuildInfo` version, Jenkins build number and commit separately. Jenkins release-tag validation expects the base version.
- Development/release distinction is `build_channel` metadata. UGS `SetEnvironmentName` does not establish Diagnostics routing; separate UGS environment routing is not implemented or claimed.

## Implementation

### Phase 0 — Cloud prerequisites

- [x] Cloud project link verified live and on disk 2026-11-02: `23aa8f08-7eb2-4bfc-880c-1060be169147`. User requested setup recheck; no settings changed by that check.
- [x] User supplied `GolinSkyCompany`; applied through Unity API and saved 2026-11-02. Live persistent-data path: `C:/Users/golin/AppData/LocalLow/GolinSkyCompany/EmpireAtWar`. Updated [[Tooling/Battle_Telemetry]] path. Existing data under `DefaultCompany` was not moved; migration is a separate decision.
- [x] Reuse `BuildInfo` version/build/commit instead of rewriting release versions.

### Phase 1 — Diagnostics configuration

- [x] Applied `EngineDiagnosticsSettings.enabled = true` through the live Editor API; saved `InsightsSettings.m_EngineDiagnosticsEnabled: 1` in `ProjectSettings/UnityConnectSettings.asset`.
- [x] Effective activation rechecked 2026-11-02: live `EngineDiagnosticsSettings.enabled=true`; Editor exception capture remains `false`. This verifies configuration, not successful standalone uploads.
- [x] Disabled `CrashReportingSettings.captureEditorExceptions`; legacy Cloud Diagnostics reporting remains disabled.
- [x] `UnityCrashContext.InitializeCapture` runs before scene load: player exception capture enabled, Editor exception capture disabled; no Editor metadata uploads from project code.
- [x] Consent wording corrected: `MainMenuUi.prefab` discloses separate Diagnostics; `SettingsUi.prefab` toggle controls gameplay statistics only. `PrivacySettingsData` summary updated.
- [x] Current API verified with Context7, official Unity docs and installed `6000.4.7f1` API XML. Metadata limit `64`; native log buffer default `10`, max `50`. Existing default retained.
- [ ] Validate managed exception/error sampling, `LogError` visibility, repetition behavior and offline delivery in a linked standalone build. No custom log forwarder added, to avoid double-submitting native captures.

### Phase 2 — Context and build support

- [x] `ICrashContext` / `UnityCrashContext` in `Assets/Scripts/Services/Diagnostics`; no gameplay classes call the Diagnostics API.
- [x] Build fields: `build_version`, `build_number`, `build_commit`, `build_channel` (`development`/`production`); loaded before the first scene from existing build stamp.
- [x] `BattleCrashContextTracker` observes battle lifecycle, including simulations; sets mode, map type, planet, player faction, state, battle seconds and entity count. Refresh every `5 s` of running real time and on state transitions; clear on scene disposal.
- [x] Tracker registered only in player builds; custom context uses enum values and numeric counts, no player/machine/save names or paths. Built-in log/path content is not scrubbed by this adapter; inspect payloads before shipping.
- [x] Jenkins optional `UPLOAD_DIAGNOSTICS_SYMBOLS` stage + `Symbols` phase + `Upload-DiagnosticsSymbols.ps1`; uses retained `player-symbols/GameAssembly.pdb` and `il2cppOutput`, Editor-bundled `usymtool`, existing process/resource monitoring. Disabled by default.
- [x] Service account `jenkins-symbol-upload` created 2026-11-03 with project role **Observability Symbols Uploader** on `EmpireAtWar` (user screenshot).
- [x] Token renewal 2026-11-03: `Upload-DiagnosticsSymbols.ps1` requests a fresh token per run from the endpoint in the official `usymtool.ps1` (`services.unity.com/.../projects/<id>/symbols/token`); args aligned with official script (`-filter GameAssembly.pdb`, Bee `-il2cppFileRoot`). Jenkinsfile binds `unity-diagnostics-service-account` as `UNITY_SA_AUTH_HEADER`. PowerShell parse clean; not run.
- [x] Jenkins global secret-text credential `unity-diagnostics-service-account` present (verified in `F:\Jenkins\data\credentials.xml` by ID, 2026-11-03; secret not read).
- [x] Jenkins updated 2026-11-03 via `Configure-GitHubPublish.ps1`: build tools installed from `b41b7f2bc` (19/19 hashes match, uploader present); both job Pipeline scripts equal repo files ignoring CR; backups in `F:\Jenkins\app\release-config-backups\20261103-*`. CRLF/LF false failures fixed in `3bdf8aadf`; rerun passed end to end, tools installed from `3bdf8aadf`, `release-tools-installation.json` written. GitHub publish job inert: `github-releases` credential absent, never run.
- [x] Build `#22` failed in Prepare: `Get-FileHash` not recognized. Cause: Jenkins started from PowerShell 7 inherited its `PSModulePath`; Windows PowerShell 5.1 build steps then lose core cmdlets (reproduced via `cmd`). Fix `4c304920b`: `Start-Jenkins.ps1` sets machine `PSModulePath`; tools installed, launcher copied to `F:\Jenkins`, Jenkins restarted, env verified.
- [x] Build `#23` failed in Prepare: snapshot renormalize changed `Docs/Audits/2026-10-29-Live-Battle-CPU/project-auditor.csv` (only file stored CRLF in index). Fixed `8634e5e68`; remaining Prepare checks (Addressables BuildWithPlayer, package pins, no submodules) pass on HEAD. Rebuild pending.
- [ ] Build `#24`: Prepare passed; CI EditMode gate failed 28/2725 tests (balance data drift since 2026-10-29, AcceptPopupUi navigation, ShareUsageDataRow tooltip key, A-Wing `Gun2` health binding, enemy blocked-ship retry, untracked `Tools/AloImport` fixtures). Unrelated to Diagnostics; blocks player build and symbol upload until resolved. Fixed in `08834ed23` (A-Wing/Delta-7 gun bindings, Settings tooltip, popup navigation, Gladiator survival waves, ALO schema+fixture; tests updated to current balance). Source/Editor checks only (roster builds 78/78, survival rule holds, compile clean).
- [x] Build `#25` SUCCESS (2026-11-03): tests passed, symbols uploaded (`All uploads completed without error`, uuid `0F240A16915E4A708E1F61346193D0673`). Release build → `build_channel=production`; `--eaw-diagnostics-*` probes absent (Jenkins has no development-build option). Deployed with new `Tools/Jenkins/Deploy-Player.ps1` to `C:\Users\golin\Desktop\EmpireAtWar\Game`.
- [ ] Forced exception/native-crash verification needs a development player (no Jenkins option yet).
- [x] Committed to `main` 2026-11-02 as `93c9b72ed` (Diagnostics code, Cloud link, company, Jenkins scripts). Not pushed; no Jenkins build has run from it yet.

### Phase 3 — Verification

- [x] Development-only standalone command-line probes: `--eaw-diagnostics-exception`, `--eaw-diagnostics-error`, `--eaw-diagnostics-native-crash`; compiled out of Editor and release. Never launch the working Editor with a native-crash probe.
- [x] Unity imported scripts/metas and saved both edited prefabs; explicit reserialization of only those prefabs. `scriptCompilationFailed=false`, Editor idle, no new Console errors.
- [x] Player-only Diagnostics sources + `SkirmishScope` compiled in memory with `DEVELOPMENT_BUILD` / without `UNITY_EDITOR`: zero compiler errors. Compilation only; no probe or gameplay executed.
- [x] Jenkins PowerShell syntax parsing and `git diff --check` passed.
- [ ] User-linked standalone: exception/error/native crash reports visible, matching symbols resolve native/C# stacks, metadata correct, development channel distinguishable.
- [ ] Confirm Diagnostics continues after declining gameplay analytics, while gameplay events stay opted out.
- [ ] Validate menu disclosure layout, logs for unintended personal data, offline/cache behavior and repeated-error handling.
- [ ] Add actual dashboard link and release triage evidence to [[Tooling/Crash_Reporting]].

## Edge Cases

- Unlinked project → serialized enable intent saved, effective Diagnostics disabled; existing UGS Analytics initialization also needs Cloud link.
- Early native crash before C# initialization → only Unity-provided context is available.
- Context counters may lag by up to `5 s` of running time; transitions refresh immediately.
- No tests, builds, forced exceptions, native crashes, network uploads or Play Mode were run. Local test execution override remains in force.

## TODO

- Local implementation/source checks complete; Cloud link and effective Diagnostics activation verified 2026-11-02. Keep active: company identity set to `GolinSkyCompany`; symbol credential/automatic renewal and live Jenkins installation remain unresolved; standalone reporting is unverified.
- Reusable guidance extracted → [[Tooling/Crash_Reporting]].
