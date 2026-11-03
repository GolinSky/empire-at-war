---
type: engineering-reference
system:
  - diagnostics
verified: 2026-11-02
verification: Official Unity documentation, live settings, Editor compilation,
  player-branch in-memory compilation and Jenkins PowerShell parse; no
  tests/player/upload executed.
---
# Crash Reporting

## Rules

- Owner → [[TODOs/Tooling/Crash_And_Error_Reporting]]. User decision `2026-11-02`: built-in Unity Diagnostics collects independently of gameplay statistics consent.
- Diagnostics uses project/build settings, not `EndUserConsent`. Declining gameplay statistics does not opt out of Diagnostics.
- No extra SDK or custom log uploader. Legacy Cloud Diagnostics stays disabled; Editor exception capture stays disabled.
- Custom metadata contains only build identity, enums and numeric counts. Built-in logs/stacks require payload review; the project adapter does not scrub them.

## Implementation

- `UnityCrashContext` initializes exception capture + existing `BuildInfo` metadata before scene load. Editor branch disables exception capture and writes no metadata.
- `ICrashContext` isolates native calls; `BattleCrashContextTracker` observes state, refreshes counters every `5 s` while running, and clears context on scene disposal. Simulations retain diagnostic context.
- Keys: `build_version`, `build_number`, `build_commit`, `build_channel`, `game_mode`, `map_type`, `planet`, `player_faction`, `battle_state`, `battle_seconds`, `unit_count`.
- API: `UnityEngine.CrashReportHandler.CrashReportHandler.SetUserMetadata(key, value)`; maximum `64` entries. Native log buffer default `10`, max `50`; default retained.
- `build_channel` is metadata, not UGS environment routing. Diagnostics does not rely on the Analytics adapter's initialization/environment.
- Version stays `PlayerSettings.bundleVersion`; Jenkins build/commit are separate metadata, preserving release tag validation.

## Important Values

- Engine enable intent: `ProjectSettings/UnityConnectSettings.asset` → `InsightsSettings.m_EngineDiagnosticsEnabled: 1`.
- Effective getter rechecked 2026-11-02: `UnityEditor.EngineDiagnostics.EngineDiagnosticsSettings.enabled=true`; linked `cloudProjectId=23aa8f08-7eb2-4bfc-880c-1060be169147` confirmed live and on disk. Standalone uploads remain unverified.
- Windows backend verified `IL2CPP` on `6000.4.7f1`.
- Optional Jenkins parameter `UPLOAD_DIAGNOSTICS_SYMBOLS`, default `false`; secret-text credential `unity-diagnostics-service-account` = service-account Authorization header → `UNITY_SA_AUTH_HEADER`.
- Service account `jenkins-symbol-upload`, project role **Observability Symbols Uploader** on `EmpireAtWar`. Key values shown once.
- Each upload fetches a fresh token (valid `7 days`): `GET https://services.unity.com/api/cloud-diagnostics/crash-service/v1/projects/<cloudProjectId>/symbols/token` → `AuthToken` → `USYM_UPLOAD_AUTH_TOKEN`. Source: official `usymtool.ps1` from Debugging Symbols → Download CI script.
- Upload args match official script: `-symbolPath player-symbols -filter GameAssembly.pdb`, `-il2cppOutputPath player-symbols/il2cppOutput`, `-il2cppFileRoot <project>/Library/Bee/artifacts/WinPlayerBuildProgram/il2cppOutput/cpp`; no `-projectId`. Configuration/how-to → repository `Tools/Jenkins/README.md`.

### Activate and inspect

1. Link the intended project in Unity Cloud/Editor. Enable Diagnostics under Services; recheck the effective getter and any Build Profile override.
2. Update/install committed Jenkins tools/job using the existing documented workflow; configure the service-account credential and opt into symbol upload. No live job update was performed by this implementation.
3. Build a disposable Development player. Launch once per probe: `--eaw-diagnostics-exception`, `--eaw-diagnostics-error`, `--eaw-diagnostics-native-crash`. Probes are excluded from Editor/release.
4. Check upload output, then matching Diagnostics reports for native/C# function/file/line resolution. A successful tool exit alone is not end-to-end proof.
5. Confirm crash diagnostics still work after declining gameplay statistics; inspect logs for unintended personal data, repeated errors, offline/cache behavior and consent UI layout.

### Release triage

1. Unity Cloud → intended project → View Diagnostics → Issues / Sessions.
2. Review crash-free sessions and exceptions by version; use build metadata to identify Jenkins run/commit. Compare like channels.
3. Inspect occurrence stacks, breadcrumbs and context; prioritize by impacted sessions/users and recurrence. Reproduce the issue before narrowing its owning code.
4. Recheck the same issue/build comparison after a fix; retain matching symbols for each supported release.

## Edge Cases

- Cloud project ID verified `23aa8f08-7eb2-4bfc-880c-1060be169147`; actual dashboard URL/organization not inspected. Jenkins recheck: live job lacks upload stage, global upload credential ID absent, installed uploader absent. Company set to `GolinSkyCompany` through Unity API on 2026-11-02; persistent data now uses `LocalLow/GolinSkyCompany/EmpireAtWar`. Existing `DefaultCompany` data was not moved.
- Native crash before managed initialization → no custom context yet. Periodic counters can lag by `5 s`.
- Token request failure (revoked key, missing role) → explicit CI failure; add a new service-account key and update the Jenkins credential. CI uploads may not appear in the manually uploaded symbols list.
- Jenkins currently replaces retained debug files on the next build; durable per-release symbol retention remains a release-operations decision.
- Tests, players, network uploads and forced crashes were not executed. Source/API verification does not prove log sampling, offline persistence, deduplication or stack resolution.

## Files

- `Assets/Scripts/Services/Diagnostics/` — interface, native context, battle tracker and probes.
- `Assets/Scripts/Services/VContainer/Scopes/{ProjectScope,SkirmishScope}.cs` — registrations.
- `Assets/Prefabs/Ui/MainMenu/{MainMenuUi,SettingsUi}.prefab` — usage-data wording.
- `ProjectSettings/UnityConnectSettings.asset`, `Tools/Jenkins/Upload-DiagnosticsSymbols.ps1`.
- [Consent behavior](https://docs.unity.com/en-us/cloud/developer-data/user-consent), [custom metadata](https://docs.unity.com/en-us/cloud/developer-data/custom-reports), [CI symbols](https://docs.unity.com/en-us/cloud/developer-data/upload-symbol-files-ci), [Diagnostics view](https://docs.unity.com/en-us/cloud/developer-data/diagnostics-page-reference).
