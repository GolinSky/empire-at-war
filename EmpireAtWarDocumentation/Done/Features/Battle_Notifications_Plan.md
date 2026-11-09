---
category: Features
status: done
created: 2026-11-09
completed: 2026-11-09
acceptance: pending-user
tags:
  - ui
  - audio
  - battle
---
# Battle Notifications Plan

## Goal
- Tell the local player about important battle events with dismissible text cards at top-center.
- Add text where announcer audio is generic (one "under attack" clip, unnamed losses, upgrades without unlocks).
- Look and motion follow the tutorial coach card (`HintUi`).

## Outcome (2026-11-09)
- All phases 1–6 implemented and compiled (runtime + editor test assemblies, no console errors).
- Prefab `Assets/Prefabs/Ui/BattleNotifications/BattleNotificationUi.prefab`, Addressable `BattleNotificationUi` in `Ui` group; key resolves to the prefab.
- Tests authored, **not run** (local override): `BattleNotificationFeedTests` (`Ui`, 6 cases); constructor updates in `FactionAnnouncementTests` (`Audio`), `FactionUnitAvailabilityTests`, `SuperWeaponAudioTests`.
- No Play Mode run; visual and in-game acceptance pending → [[TODOs/User_Acceptance_Queue]].

## Decisions
- **Own feature, not `HintService`:** hints are one card, persisted progress, HUD anchors, paused in combat. Notifications need a queue, grouping and expiry.
- **Sources decide; audio untouched:** text never depends on announcer cooldown/priority refusals.
- **Feed in battle scope** (`SkirmishScope`), always registered so battle- and player-scope services can post. UI + battle sources skipped in `BattleSimulation`.
- **Feed gated by `BattleState`:** accepts gameplay cards only in `Running`/`Paused` → loading and save restore stay silent. `Ended` → `EndBattle()` clears all but Outcome.
- Open questions resolved by default:
  - Tutorial: both show. Hint card is top-left (24, −140); stack is top-center.
  - No settings toggle; no click-to-focus camera (not requested).
  - Rejection reasons use the top-center stack as short `Feedback` cards (3 s).
  - Victory/Defeat/Draw kept as `Outcome` card (12 s) — check against `EndGameUi` in acceptance.

## Important Values
- Stack: anchor top-center, y −12 (`topOffset`); Survival y −78 (`survivalTopOffset`, below Wave/Status rows −8…−70). Width 460, card 44 + 6 gap inside a collapsing slot.
- Max visible 3, max pending 8; Urgent replaces the least important visible Progress/Feedback card.
- Lifetime: Outcome 12 s, Urgent 8 s, Tactical 6 s, Progress 4 s, Feedback 3 s (unscaled time).
- Grouping: a visible/waiting card with the same `GroupKey` absorbs posts (count +1); repeats extend it to at most 2× lifetime (`MAX_AGE_FACTOR`).
- `Hold(...)`: keep-alive without counting, no age cap (station/mine under attack stays while hits continue, leaves 8 s after the last).
- Dismissed group key muted 15 s (`DISMISS_MUTE_SECONDS`).
- Card motion: open grows slot + fades/slides in (0.15 s / 0.18 s); close fades out, then collapses the slot.

### Fix 2026-11-09 (user report: cards stuck / flicker)
- Stuck: grouped repeats reset the timer forever → age cap.
- Duplicates: posts after the 3 s window opened a second card of the same group → visible card absorbs.
- Flicker: attack card 8 s vs 20 s repost cooldown; dismissed cards reappeared → `Hold` + dismiss mute.
- Jumps: closing card kept its slot then snapped; merge scale pulse → collapsing slot, pulse removed.
- Accents: Outcome `#F8FAFC`, Urgent `#EF4444`, Tactical `#F59E0B`, Feedback `#94A3B8`, Progress `#38BDF8`. Body `#080C14F5`, outline `#1E293B99`.
- Station hull critical at ≤25%, re-armed above 50%. Enemy fleet quiet period 20 s.

## Implementation

| Phase | Card | Source |
|---|---|---|
| 1 | Feed, card UI, prefab, Addressable, placement | `BattleNotificationFeed`, `BattleNotificationUi`, `BattleNotificationCardView`, `BattleNotificationUiController` |
| 2 Urgent | Station / mining facility under attack; station hull critical — 25%; [Hero/capital] destroyed (also off camera); enemy fleet approaching | `ThreatNotifier` (battle) |
| 2 Urgent | Incoming [Superweapon] strike on [unit / your station] | `SuperWeaponNotifier` (local player, SuperWeapons module) |
| 2/3 | Ordinary losses grouped: "[Ship] lost" → "N ships lost", squadrons likewise | `ThreatNotifier` |
| 3 Tactical | Enemy station reached level N | `ThreatNotifier` |
| 3 Tactical | Station level N — unlocked [≤3 items] +N more | `EconomyNotifier` (local economy) |
| 3 Tactical | [Site] captured / taken by the enemy (Urgent) / destroyed (Urgent) | `CaptureSiteNotifier` (CaptureSites module) |
| 3 Tactical | Reinforcement zone lost/captured (count diff) | `MapNotifier` (battle) |
| 3 Tactical | [Ship] entered [hazard] — shields down / abilities blocked (selected or hero/capital only) | `MapNotifier` |
| 4 Progress | [Research] complete; [Unit] ready in reserve; [Unit] deployed; [Superweapon] ready; facility construction complete | `EconomyNotifier`, `SuperWeaponNotifier`, `CaptureSiteNotifier` |
| 5 Feedback | Insufficient credits (unit, site facility); Fleet capacity full; Invalid target (superweapon, ability); Ability unavailable | `FactionUiController`, `CaptureSiteBuildUiController`, `EconomyNotifier`, `SuperWeaponPresenter`, `AbilityTargetingNotifier` |
| 6 | Wave N incoming in 10 s; Wave N cleared | `SurvivalNotifier` (Survival mode) |
| 6 Outcome | Victory / Defeat / Draw | `BattleOutcomeNotifier` |

- Online client: `ClientPlayerEconomyFeedback` forwards host feedback to `EconomyNotifier.ShowReplicated` (research name unavailable → "Research complete").
- Ability rejection: `ShipAbilityService.IsWaitingForTarget` true → "Invalid target", else "Ability unavailable".

## Edge Cases
- `SuperWeaponModel.IsRestoring` → no "ready" card.
- Enemy station `LevelUpgraded` during restore is suppressed by the state gate (Loading).
- Ability "Press" rejection while another ability is already targeting reports "Invalid target" (shared state; rare).
- Capture `SiteLost` = owner → neutral; reported as "[Site] destroyed".
- Site/unit names: `SiteFacilityCost.Name`, `FactionData.Name`; attacker superweapon names are hard-coded (attacker may be another faction).

## Files
- `Assets/Scripts/Services/BattleNotifications/*` (feed, notification types, 9 sources).
- `Assets/Scripts/Entities/BattleNotifications/*` (UI, card view, presenter interface, controller).
- `Assets/Scripts/Services/VContainer/Registrations/BattleNotificationRegistrations.cs`; hooks in `SkirmishScope`, `SurvivalMode`, `CaptureSitesModule`, `SuperWeaponsModule`, `HumanPlayerScope`; `UiPlacement` → `dynamicCanvas`.
- `AGENTS.md` test table: `Entities/BattleNotifications`, `Services/BattleNotifications` → `Ui`.

## Reusable Guidance
- No new reusable rule beyond existing `UI_CODE_BUILD_GUIDE`; the state-gated feed pattern is documented here.
