---
category: Features
status: review
acceptance: pending-user
updated: 2026-11-18
---
# Squadron Bay Rotation

## Goal
- Ship-wide cap on simultaneously active squadrons: `HangarMaxActive` on `ShipData` (per ship, NOT per bay).
- Capped ship keeps N squadrons out; when one dies it launches the next bay's squadron (`Vulture → DroidBomber → Belbullab22 → Vulture …`).

## History
- 2026-11-18 Gemini: `b5cef55ad` added the cap + ship values → `37c7b4f8c` reverted it → `3912484bd` hand-edited per-bay `maxActive` on 37 `ShipData.asset` (wrong: per-bay, not per-ship).
- 2026-11-18: `3912484bd` reverted (`8925de639`). Hangar code + ship data now equal `8f23d1266` (pre-Gemini). Unity refresh + recompile: 0 errors.
- `b5cef55ad` is a usable reference for Phases 1–2 (code + asset values); re-check it against the lists below.

## Rules
- Avoid: editing per-bay `maxActive` / `reserve` to fake a ship cap.
- `HangarMaxActive`: `-1` → per-bay limits only (default, stations); `0` → ship launches no squadrons; `N > 0` → total active across bays ≤ N.
- Per-bay `MaxActive` and `Reserve` still apply under the cap.
- Rotation = existing `HangarModel._nextAutomaticBay = (bay + 1) % BayCount`; empty/disabled bays skipped.
- Set asset values via Unity API (`SerializedObject`, `SetDirty`, `SaveAssets`), not by hand.

## Phase 1 — Cap code
1. [x] `IHangarData.HangarMaxActive`; `ShipData` `[field: SerializeField, Min(-1)] int HangarMaxActive = -1` in `[Header("Hangar")]` after `hangarBays`; `StationHangarData` → `-1`.
2. [x] `HangarModel`: `IsShipCapReached()` (`HangarMaxActive >= 0 && TotalActive() >= HangarMaxActive`) blocks `FindLaunchBay` and `FindReplacementBay`; `PendingLaunchCount` clamped to `HangarMaxActive - TotalActive()`.
3. [x] Test stubs: `HangarModelTests.TestHangarData`, `QuasarFireHangarTests.CarrierData`. Tests: cap 0 → no launches/pending 0; cap 1 → bays rotate one at a time as squadrons die; restore with cap reached.

## Phase 2 — Ship values (40 ships with hangars)
- `HangarMaxActive = 0` (12): `Dauntless`, `CorellianBattlecruiser`, `MC75Profundity`, `Nebula`, `Executor`, `Mandator`, `Procurator`, `Rothana`, `Imperator`, `Bulwark`, `BulwarkII`, `RecusantDreadnought`.
- `HangarMaxActive = 1` (rotating, 28 → 19 after Phase 4 moved the 9 excepted carriers to -1): `Acclamator`, `AcclamatorAssault`, `BothanAssaultCruiser`, `Captor`, `DHOmni`, `Endurance`, `Gladiator`, `HomeOne`, `ISDI`, `ISDII`, `ImperialVenator`, `InterdictorStarDestroyer`, `Invincible`, `Liberator`, `Lucrehulk`, `MC80Freedom`, `MC80Independence`, `MC80Justice`, `MC80bRemonda`, `MediatorBattlecruiser`, `Praetor`, `Providence`, `QuasarFire`, `Resolute`, `Secutor`, `Venator`, `VenatorII`, `Wavecrest`.
- [x] `Bulwark` (Mk1) = 0 — user confirmed 2026-11-18.
- [x] Values applied (restored from `b5cef55ad`, verified 12×0 / 28×1 = 40).
- [ ] Check `ShipData` balance fixtures + ship tooltip descriptions mentioning squadron counts for ships set to 0.

## Phase 3 — Readers that sum per-bay `MaxActive`
- [x] `Services/Enemy/UnitCombatProfileCatalog.cs:63` — AI air-wing strength → use `min(sum MaxActive, HangarMaxActive)` when cap ≥ 0.
- [x] `Services/Repository/BattleAssetPreload.cs:161` — skip squadron preload when `HangarMaxActive == 0`.
- [x] (no change: station bays only, station cap = -1) `Services/Squadron/SquadronPool.cs:211`, `Components/ViewComponents/Weapon/ShotPoolPrewarmer.cs:205` — prewarm counts; clamp to cap (overestimate is safe, optional).
- [x] (kept blocked: `TryLaunchReplacement` honors the cap) Hangar abilities `FighterReinforcements` (50), `LiberatorRapidDeployment` (51) are blocked by the cap → decide keep blocked or bypass.

## Phase 4 (final) — Bay rotation for all ships except big carriers
- Rotation for every hangar ship EXCEPT: `Venator`, `VenatorII`, `Lucrehulk`, `Secutor`, `MC80Independence`, `HomeOne`, `ImperialVenator`, `ISDI`, `ISDII`.
- [x] Excepted carriers: `HangarMaxActive = -1` and every bay `maxActive = 1` (reserve unchanged) → one squadron per bay launches in parallel, no rotation. Applied 2026-11-18 via SerializedObject.
- [x] Ships with `HangarMaxActive = 0` have nothing to rotate; unaffected.

## Edge Cases
- Replacement launches ≈ immediately after loss (launch interval elapses while the squadron lives; no loss-triggered reset per [[Review/Bugs/Carrier_and_Squadron_Rebalance]]).
- Hangar hardpoint destroyed with a squadron out → `_active` counted until it dies; then nothing launches.
- AI `AiWorldSnapshot.cs:207` uses `LaunchedCount + PendingLaunchCount` → already cap-aware once Phase 1 lands.
- Cap is authority-side `HangarModel` state; no new replication.

## Tests
- Categories: `Squadrons` (Phase 1), `Balance` (Phase 2 data), `EnemyAi` (Phase 3 catalog), `Combat` (prewarmer).
- No stat-pinning: assert cap behavior with test data, never asset values.
- Not run (local override). Compile: 0 errors after Phases 1–3 (2026-11-18).

## TODO
- User in-game check: capped ship keeps 1 squadron out and rotates types; 0-cap ships launch none.

## Review 2026-11-26
- Bay rotation feature complete. Balance fixtures: nothing to change (only `HangarModelTests`, `QuasarFireHangarTests` read `HangarMaxActive`; `BalanceInventoryTests` check structure).
- Carrier role/text on the 12 zero-cap ships split out → [[Review/Bugs/Carrier_Role_On_Zero_Squadron_Ships]].
- Open user gate: in-game bay rotation check.
