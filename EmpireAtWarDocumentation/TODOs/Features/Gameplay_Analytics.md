---
category: Features
status: in-progress
created: 2026-11-02
tags:
  - analytics
---
# Gameplay Analytics

## Goal

- Track core game statistics from player builds: matches started/finished, win/loss/draw per faction, mode, map, duration, abandon rate, performance per build.
- Dashboards: faction win rates, average match length, popular modes/maps, FPS/memory by build.
- Companion: [[TODOs/Tooling/Crash_And_Error_Reporting|Crash And Error Reporting]] (crashes/exceptions).

## Decision

- **Chosen (user-confirmed 2026-11-02):** Unity Gaming Services Analytics SDK (`com.unity.services.analytics` 6.x), free tier 50k MAU.
- Why: schema-defined custom parameters queryable in dashboards; shares UGS project + consent with Diagnostics.
- Behind `IAnalyticsService` so GameAnalytics/PlayFab can replace the adapter without touching gameplay.
- Avoid: gameplay classes calling `AnalyticsService.Instance` directly; mixing old `StartDataCollection` flow with `EndUserConsent` (SDK throws).
- Single-player only today → client-reported results are acceptable. Multiplayer → authoritative server result later.

## Current State (2026-11-02)

- Match result already exists: `BattleVictoryService` (`Services/BattleService`) is `INotifier<BattleResult>`; observed by `EndGameUiController`, `SkirmishOrchestrator`.
- `BattleResult`: `Outcome` (`PlayerVictory`/`EnemyVictory`/`Draw`), `Mode` (`Skirmish`/`Survival`/`BattleSimulation`), `VictoryCondition`, `Planet`, `PlayerFaction`, `EnemyFactions`, `PlayerShipCount`, `EnemyShipCount`, base-alive flags.
- Battle setup: `BattleDefinition` (mode, `MapSpec`, players, features, starting money, station level); start → `BattleStartupSequence.RunAsync`.
- Kill/loss source: `IShipService.ShipRemoved` (fired by `Ship.Release`: death, or scope teardown after the match). Ships only; squadrons/structures not counted.
- DI: VContainer scopes `ProjectScope`, `MainMenuScope`, `SkirmishScope` in `Services/VContainer/Scopes`.
- Settings persisted by `JsonSettingsRepository` / `SettingsData`; consent can live there.

## Rules

- Release + development builds send (separate UGS environments); Editor sends nothing unless `ANALYTICS_EDITOR` define.
- No events before consent; consent default = not granted.
- Event names `snake_case`; parameters typed and declared in UGS event schema before shipping.
- One event per meaningful moment; no per-frame events. Performance sampled every `60 s` max.
- `BattleSimulation` sends nothing: tracker not registered for that mode (dev balance tool).
- No PII: installation ID only; no names, paths, save names.

## Event Schema (v1)

```
match_started    match_id, game_mode, map_type, victory_condition, player_faction, opponent_factions,
                 ai_count, starting_money, initial_station_level, is_loaded_save
match_completed  match_id, game_mode, result(win|loss|draw), victory_condition, player_faction,
                 opponent_factions, duration_seconds, player_ships_left, enemy_ships_left,
                 player_base_alive, enemy_base_alive, units_destroyed, units_lost
match_abandoned  match_id, game_mode, duration_seconds, reason(quit_menu|quit_app|restart)
survival_wave    match_id, wave_index, battle_seconds, player_ships_left   (Survival only)
performance_sample match_id, avg_fps, p5_fps (95th-pct frame time → fps), memory_mb (allocated), unit_count (IEntityLocator entities), quality_level (index 0–5)
session context  game_version (Application.version), graphics_tier, os  (UGS standard events cover most)
```

## Implementation

### Phase 0 — Foundation

- [ ] Shared with crash plan Phase 0: UGS project link, environments, `companyName`, version scheme.
- [x] Add `com.unity.services.core` `1.18.0` + `com.unity.services.analytics` `6.3.0` (latest in registry 2026-11-02).
- [x] `IAnalyticsService` (`Services/Analytics`): `RecordMatchStarted`, `RecordMatchCompleted`, `RecordMatchAbandoned`, `RecordPerformanceSample`, `Flush`; typed data classes `MatchStartedData`, `MatchCompletedData`, `MatchAbandonedData` (+ `MatchAbandonReason`), `PerformanceSampleData`.
- [x] `UgsAnalyticsService` adapter: `IInitializable` → `UnityServices.InitializeAsync` with environment `development` (`Debug.isDebugBuild`) / `production`; one `UgsAnalyticsEvent : Event` with `Set(name, value)`; event/param names = Event Schema v1. Calling `Flush` on match end is Phase 2.
- [x] `DisabledAnalyticsService` registered in Editor unless scripting define `ANALYTICS_EDITOR`.
- [x] Registered in `ProjectScope` (`#if UNITY_EDITOR && !ANALYTICS_EDITOR`). Verified 2026-11-02: Assembly-CSharp compiles, `scriptCompilationFailed=False`; tests not run (local override).

### Phase 1 — Consent

- [x] One combined consent (user decision 2026-11-02): `PrivacySettingsData` (`shareUsageData`, `hasAnsweredConsent`) as `SettingsData.Privacy`; covers analytics + crash reports.
- [x] First-launch prompt: `UsageConsentPrompt` child of `MainMenuUi.prefab` (cloned from `SettingsUi` `Prompt` styling; Allow/Decline) shown by `MainMenuUiController.Open` while `!HasAnsweredConsent`; answer → `ISettingsService.AnswerUsageDataConsent` (saves + applies, leaves other draft edits).
- [x] Settings toggle: `ShareUsageDataRow` in Gameplay panel of `SettingsUi.prefab` → `ISettingsPresenter.SetShareUsageData` → draft, saved on Apply.
- [x] `AnalyticsConsentApplier : ISettingsApplier` → `IAnalyticsService.SetConsent`; UGS adapter sets `EndUserConsent` `AnalyticsIntent`, revoke of a granted consent → `RequestDataDeletion` (only when UGS is initialized). Unanswered → stays `Unspecified`.
- Verified 2026-11-02: compile clean (incl. test assembly; `MainMenuUiControllerLifecycleTests` updated for new ctor arg), prefab refs non-null, buttons Color Tint. Tests not run (local override). In-game popup/toggle check pending user.

### Phase 2 — Match events

- [x] `MatchAnalyticsTracker` (`Services/Analytics`, `SkirmishScope`, skipped for `BattleSimulation`): first `BattleState.Running` → new `match_id` (GUID N) + `match_started`; `INotifier<BattleResult>` → `match_completed` + `Flush`. Durations = `BattleClock.Elapsed` (battle seconds, game-speed scaled, paused excluded).
- [x] Abandon: `LateDispose` with started && no result → `match_abandoned` + `Flush`; reason from `IUserStateNotifier`: `RestartBattle` → `Restart`, `ExitGame` → `QuitToMenu`, none → `QuitApplication`. No mid-battle load route exists.
- [x] Loaded saves (decision 2026-11-02): new `match_id` + `is_loaded_save=true` (`GameData.PendingBattleSave != null` at `Initialize`); `duration_seconds` = total battle clock incl. time before the save. No save-format change.
- [x] Kills/losses: `IShipService.ShipRemoved` between start and result; `ILocalPlayer.IsFriendly(owner)` → `units_lost`, else `units_destroyed` (ships only; AI allies count as own).
- [x] Survival: `SurvivalWaveAnalytics` (registered in `SurvivalMode`) → `survival_wave` per cleared wave via `SurvivalModel.Changed`; starts at `Running`, so restored cleared waves are not re-sent.
- Verified 2026-11-02: Assembly-CSharp + tests compile, `scriptCompilationFailed=False`; no test builds battle containers. Tests not run (local override). Events not observed end-to-end: needs linked UGS project + consent.

### Phase 3 — Performance sampling

- [x] `PerformanceAnalyticsSampler` (`SkirmishScope`, skipped for `BattleSimulation`): `ITickable` adds `Time.unscaledDeltaTime` only while `BattleState.Running`; sample per `60 s` of running frames + final partial on `Ended` or scene close.
- [x] `FrameTimeHistogram` (pure C#): 400 × `0.25 ms` buckets + >100 ms overflow, no per-frame allocation; avg fps = frames ÷ seconds; low fps = bucket upper bound of 95th percentile (conservative, ≤0.25 ms quantization).
- [x] Read `Rules/BATTLE_PERFORMANCE_GUIDELINES`. Values are field telemetry per build/quality, not matched captures; do not use them as optimization acceptance evidence.
- Verified 2026-11-02: compile clean; Editor eval sanity check (95×10 ms + 5×50 ms → avg 83.3, low 100; all 200 ms → 5/5). Tests not run (local override).

### Phase 4 — Dashboards and verification

- [ ] Define event schemas in UGS dashboard before first upload.
- [ ] Build dashboards: faction win rate, mode/map popularity, avg duration, abandon rate, FPS by build.
- [ ] Dev build: play one Skirmish win, one loss, one quit → events visible in UGS Event Browser with correct params.
- [ ] Tests (when execution allowed): tracker maps `BattleResult` → event args (`GameFlow` category), consent gating.
- [ ] Canonical guidance note `Tooling/Analytics` (event list, how to add an event, dashboard links).

## Edge Cases

- Restart match → `match_abandoned(reason=restart)` then new `match_started`.
- Game speed changes → `duration_seconds` uses battle clock (decide scaled vs real; record both if cheap).
- Offline → SDK caches to disk; verify cache survives quit.
- `Draw` outcome → `result=draw`.
- Multiple AI opponents → `opponent_factions` sorted, joined string (UGS has no array params).

## Open Decisions

- Steamworks Stats/achievements — out of scope; separate plan if wanted.

## TODO

- Phase 0 code done 2026-11-02; UGS project link (user) still open → player builds throw `UnityProjectNotLinkedException` from `Initialize` until linked.
- Phase 1 consent done 2026-11-02; user check pending: first launch shows popup once, Settings > Gameplay toggle reflects/changes answer.
- Phase 2 match events done 2026-11-02 (code); end-to-end check in UGS Event Browser is Phase 4.
- Phase 3 performance sampling done 2026-11-02 (code).
- Next: Phase 4 (user links UGS project, define schemas, dashboards, dev-build event check, `Tooling/Analytics` guidance note).
