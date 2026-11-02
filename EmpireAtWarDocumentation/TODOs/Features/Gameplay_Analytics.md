---
category: Features
status: todo
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
- No central "unit destroyed" event found; kills/losses counter needs a source (verify `IEntityLocator` removal or health death path).
- DI: VContainer scopes `ProjectScope`, `MainMenuScope`, `SkirmishScope` in `Services/VContainer/Scopes`.
- Settings persisted by `JsonSettingsRepository` / `SettingsData`; consent can live there.

## Rules

- Release + development builds send (separate UGS environments); Editor sends nothing unless `ANALYTICS_EDITOR` define.
- No events before consent; consent default = not granted.
- Event names `snake_case`; parameters typed and declared in UGS event schema before shipping.
- One event per meaningful moment; no per-frame events. Performance sampled every `60 s` max.
- `BattleSimulation` mode excluded from win-rate events (dev tool) — tag `game_mode` and filter, or skip entirely (decide).
- No PII: installation ID only; no names, paths, save names.

## Event Schema (v1)

```
match_started    match_id, game_mode, map_type, victory_condition, player_faction, opponent_factions,
                 ai_count, starting_money, initial_station_level, is_loaded_save
match_completed  match_id, game_mode, result(win|loss|draw), victory_condition, player_faction,
                 opponent_factions, duration_seconds, player_ships_left, enemy_ships_left,
                 player_base_alive, enemy_base_alive, units_destroyed, units_lost
match_abandoned  match_id, game_mode, duration_seconds, reason(quit_menu|quit_app|restart)
survival_wave    match_id, wave_index, duration_seconds, player_ships_left   (Survival only)
performance_sample match_id, avg_fps, p5_fps, memory_mb, unit_count, quality_level
session context  game_version (Application.version), graphics_tier, os  (UGS standard events cover most)
```

## Implementation

### Phase 0 — Foundation

- [ ] Shared with crash plan Phase 0: UGS project link, environments, `companyName`, version scheme.
- [ ] Add `com.unity.services.core` + `com.unity.services.analytics` (verify latest compatible versions via Context7 / package search).
- [ ] `IAnalyticsService` (`Services/Analytics`): `RecordMatchStarted(...)`, `RecordMatchCompleted(...)`, `RecordMatchAbandoned(...)`, `RecordPerformanceSample(...)`; typed args, no strings dictionary.
- [ ] `UgsAnalyticsService` adapter: `UnityServices.InitializeAsync` at boot, custom `Event` subclasses, `Flush` on match end + quit.
- [ ] `NullAnalyticsService` only for Editor/tests (explicit registration, not silent fallback).
- [ ] Register in `ProjectScope` (lives across scenes).

### Phase 1 — Consent

- [ ] Add `shareAnalytics` / `shareCrashReports` to settings data (or one combined toggle — decide).
- [ ] First-launch consent prompt in main menu + toggle in Settings (read `UI_UX_GUIDELINES` + `UI_CODE_BUILD_GUIDE` first).
- [ ] Apply via `EndUserConsent.SetConsentState(...)` with `AnalyticsIntent` (Unity 6.2+ flow). Revoke → deny consent, then `RequestDataDeletion`.

### Phase 2 — Match events

- [ ] `MatchAnalyticsTracker` in `SkirmishScope`: observes battle start and `INotifier<BattleResult>`; owns `match_id` (GUID) and battle duration.
- [ ] Abandon: scene unload / quit before `BattleResult` → `match_abandoned`.
- [ ] Loaded saves: keep `match_id` in save header (`BattleSaveHeader`) or mark `is_loaded_save` — decide.
- [ ] Kills/losses counter: find authoritative unit death source; aggregate per team.
- [ ] Survival wave events from `SurvivalWaveDirector`.

### Phase 3 — Performance sampling

- [ ] `PerformanceSampler`: rolling FPS avg + p5, `Profiler.GetTotalAllocatedMemoryLong` → MB, live unit count; one sample per `60 s` + one at match end.
- [ ] Read `Rules/BATTLE_PERFORMANCE_GUIDELINES`; sampler must allocate nothing per frame.

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

- One consent toggle or separate analytics/crash toggles.
- Include `BattleSimulation` mode or not.
- Steamworks Stats/achievements — out of scope; separate plan if wanted.

## TODO

- Implementation not started.
