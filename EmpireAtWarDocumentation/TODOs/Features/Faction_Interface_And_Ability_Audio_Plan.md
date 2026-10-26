---
category: Features
status: in-progress
created: 2026-10-26
validated: 2026-10-26
implemented: 2026-10-26
---
# Faction Interface and Ship Ability Audio Plan

## Goal
- Map existing ship abilities to the approved activation/deactivation SFX.
- Add faction announcements for research, stations, mines, production/reinforcements, unit losses, new build options, defeat, enemy detection and maximum unit capacity.
- Evaluate sensor-jamming, ability-ready, superweapon-ready and faction-selection effects.
- Implementation completed 2026-10-26; active for user listening acceptance and optional ready-sound choices. No agent Play Mode.

## Rules
- Reuse the existing audio pipeline; related work: [[TODOs/Features/Audio_System_Extension_Plan]] and [[TODOs/Features/Audio_System_Extension_Plan - Research]].
- Recheck live implementation before editing; earlier audio-plan implementation claims are not new verification.
- Import only mapped files; preserve original filenames and source extractions.
- Announcements: 2D, local player's faction voice. Enemy station upgrades describe the enemy but use the listener's faction voice.
- Ability SFX: preserve the established visible-ship/fog/cloak routing and audio limits.
- Service owns play/refuse decisions; no audio MVP, no budget helper classes ([[audio-no-mvp-service-owns-decisions]]).
- Emit announcements from command/completion paths, never from model property setters (setters also run on save restore).
- Before implementation, read `PROJECT_ORGANIZATION` for asset placement and `UI_UX_GUIDELINES` before any UI edits.

## Implementation Results (2026-10-26)
- Imported **62 WAVs / 4,912,750 bytes**: 44 announcer, 14 unit-ready, 4 SFX. Only mapped live units imported; source filenames preserved.
- Inventory: `Logs/FactionAudio/import_inventory.csv`; exclusions: `excluded_unit_ready.json`; storage: `storage_inventory.json`. Duration 107.06 s; PCM payload 4,689,894 bytes; compressed estimate ≈ 0.47 MB, not a measured build size.
- Mapped 21 abilities, including sensor jamming and `NebulaOverchargeWeapons`; excluded `ImperialVenatorIntensifyFirepower` because its settings are full-salvo. Preserved execution/restore and weapon-end clips; generic activation/end cues only fill missing specific cues.
- Added complete faction sets; Republic/Separatist share Rebellion references. Catalog subtitle maps `I000_EHD5503_ENG.wav` to “Tech Upgrade in progress”; actual listening remains pending.
- Added announcement priority, cooldowns, live purchase/cancel/completion/deployment/cap hooks, station/mine damage and losses, neutral mine captures, unit-ready/loss, unlock follow-up, defeat and fog detection. No general announcement queue.
- **Restore-safe correction:** `CompleteLevelUpgrade` emits `LevelUpgradeCompleted` on human/AI models; station facade forwards it. `CurrentLevel` setters stay silent. A first-`ApplyLevel` guard was insufficient because saves restore levels after station creation.
- **Damage correction:** `IHealthDamageFacade` reports actual hull/shield decrease inside damage commands; restored health values never trigger warnings.
- Fleet cue: hostile ship set transitions empty → visible after 20 s quiet; initial/respawn visibility snapshots stay silent. Mine/station warning cooldown 20 s per entity; ship loss cooldown 8 s; cap/rejection cooldown 3 s.
- Unit-loss follow-up: announce local ship death only when currently on camera or last seen ≤ 5 unscaled seconds ago; fog-hidden ships do not refresh history. Trigger = `IHealthModelObserver.OnDestroy`, not delayed entity removal. Dead ships stop tracking; cleanup unsubscribes and never repeats speech. Allocation-free `ITickable` updates cached transforms for local ships to remember camera movement. Compilation and `FactionAnnouncementTests` **22/22** passed, including exact 5 s boundary, stale/never-visible, fog, corpse tracking, delayed cleanup and existing throttles (`Logs/FactionAudio/tests-recent-camera-loss.json`).
- Unit-ready plays at purchased production completion; en-route at later ship/squadron deployment. Structure placement is silent. Generic construction fallback covers unmapped units; redundant “reinforcements available” omitted.
- Menu faction SFX: actual human changes in skirmish/survival only; initial binding, AI slots and simulation AI sides silent. `MainMenuScope.interfaceSfxData` saved in `MainMenuScene`.
- Startup fix: `FindMatchingGroups("Sfx")` returns `Sfx` and child `Ducked`; select the exact `group.name == "Sfx"` before `Single`. Regression test initializes the service with the real project mixer; prior routing tests bypassed initialization. Fix compiled. Follow-up focused `MenuFactionAudioTests` run passed 9/9, including real-mixer initialization (`Logs/FactionAudio/tests-menu-mixer-fix.json`).
- Ability SFX follow-up: doubled all 164 ability-phase volumes plus 2 generic fallback volumes (166 values); Unity save/readback verified. Only volume lines changed. Evidence: `Logs/FactionAudio/ability-volume-doubled.csv`.
- Unit-purchase search: unimported `EHD_Build_Vehicle` / `RHD_Build_Vehicle` clips exist: “Constructing Unit” = `I000_EHD4606_ENG.wav` / `I000_RHD4706_ENG.wav`; “Unit in production” = `I000_EHD4613_ENG.wav` / `I000_RHD4713_ENG.wav`; “Vehicle in production” = `I000_EHD4614_ENG.wav` / `I000_RHD4714_ENG.wav`. Source: `F:/Temp_audio/Empire_Rebellion_Audio/Audio/Interface/Voices/<Faction>/<event>/`; catalog + `SFXEVENTSHUD.XML` verify build-start events. Search only; no purchase cue wired. Existing selected-unit move/attack speech already wired.
- Optional ability-recharge and superweapon-ready replacements remain unchanged pending explicit listening choice. Existing orbital voice assignments retained.
- Unity refresh, explicit two-audio-asset reserialization and save completed; menu scene saved/clean. Compilation passed; no import/serialization errors. Intentional error-path logs came from passing AI lifecycle tests.
- EditMode filters: **Audio 63/63**, **Sfx 10/10**, **Faction 88/88**, **FogDetectionEventTests 1/1**, **HealthModelTests 23/24**. Filters overlap; counts are not distinct totals. Reports: `Logs/FactionAudio/tests-*.json` (Unity status can return incremental result rows).
- Remaining test failure: `HealthModelTests.PointDefense_WhileCloakedPreservesMissileAndCanFireAfterDecloaking`, unchanged point-defense path, outside audio changes. Read-only evidence: EditMode `Time.time == 0`, fresh timer `IsComplete == false`; timer uses strict `_timer < Time.time`. No unrelated fix applied.
- Remaining acceptance: listen for volume/priority/timing/duplicates, verify Empire progress wording, decide optional ready clips. Plan stays active.

## Validation (2026-10-26, pre-implementation audit)

### Already in place — do not rebuild
- Per-ability audio is data-driven: `ShipAbilityAudioProfile` (`start`/`execution`/`end`/`restore`) in `ShipSfxData.abilities`; every listed ability already has all four clips assigned (164 clip refs).
- `AudioShipComponent.HandleAbilityChanged` plays `Start` on →`Active`, `End` on →`Recovering`, `Restore` on `Recovering→Ready` (local only). Natural expiry already routes through `End`.
- Playback goes through `ShipSfxService.Request` → positional gain, vision/fog, 20-voice pool, 2 voices per ship, 3 starts per 0.06 s window, per-`SfxProfile` cooldown.
- Already imported: `Assets/Audio/SFX/Shields/GUI_SHIELDS_UP_1.WAV`, `GUI_SHIELDS_DOWN_1.WAV`, `Assets/Audio/SFX/Abilities/GUI_TOGGLE_17.WAV`, `GUI_TOGGLE_18.WAV`.
- Missing: `GUI_TOGGLE_UW_2.WAV` and all 14 announcer WAVs.
- Ability mapping = asset edit of `ShipSfxData.asset` `start.clip`/`end.clip` only; no new ability-audio types.

### Conflicts found — must resolve before wiring
- **Double cue.** Generic `ShipSfxData.abilityActivated` = `GUI_TOGGLE_17`, `abilityCanceled` = `GUI_TOGGLE_18`; they play on *every* `Ready→Active` / `Active→Recovering` beside the per-ability `Start`/`End`.
  - Engine boost/S-foils would play `GUI_TOGGLE_17` twice; shield boost would play `SHIELDS_UP` + `TOGGLE_17` together.
  - Two one-shots + the `execution` loop exceed `perShipVoices = 2` → a cue is dropped nondeterministically.
  - **Decided (user, 2026-10-26):** play the generic cue only when the ability's `Start`/`End` profile has no clip. ~2-line change in `AudioShipComponent`; add a coverage test.
- **Shield-state cue collision.** `ShipSfxData.shieldsUp/shieldsDown` (shield HP threshold cues, cooldown 0.5 s) use the same `GUI_SHIELDS_*` clips. Acceptable reuse, but shield boost ending may coincide with a shield-down transition → set `cooldown ≥ 0.25 s` on the mapped profiles; cooldown is keyed per `SfxProfile` instance, not per clip.
- **Announcer has no priority/queue.** `InterfaceSfxService.PlayAnnouncement` replaces whatever is on `_announcer` and stops unit voice. "Existing priority/ducking must remain intact" is not a real constraint — there is none to preserve.
  - New rule: an orbital/super-weapon announcement is never interrupted by an economy announcement; economy announcements drop (not queue) while `IsAnnouncing`, except cap/rejection which is also dropped. No queue — stale voice lines are worse than silence.
  - Add a per-event cooldown (≥ 1.5 s) to collapse rapid buy/cancel spam.
  - `ShipSfxService.TryPlayVoice` refuses unit responses while `IsAnnouncing` → frequent announcements mute selection/move voices. Cooldowns above limit this.
- **Announcements are not faction-routed today.** `InterfaceSfxData` holds single clips; orbital lines are Rebellion `I000_rhd_9000…` for every faction.
- **Republic/Separatist have no counterparts** for any event in this plan. "Do not replace Republic/CIS mappings" has nothing to protect.
  - **Decided (user, 2026-10-26):** Republic/Separatist borrow Empire/Rebellion voices. Every `FactionType` gets a complete `FactionAnnouncerSet` entry (no empty/silent entries); coverage test asserts all four.
  - **Decided (user, 2026-10-26):** Republic **and** Separatist both use the Rebellion voice set. Same rule for `GUI_Select_Rebel` and the unit-ready fallback (`RHD_Construction_Complete`).
  - Data: Republic/Separatist entries reference the same clip assets (no duplicate imports).

### Hook audit — what exists vs what is needed
| Trigger | Live source | Gap |
|---|---|---|
| Research/station purchase accepted | `FactionService.TryPurchaseUnit` → `_model.QueueUnit` | Returns `void`; silent failures (queue full, `!CanProduce`, host check). Needs success signal. |
| Purchase rejected | `FactionUiController.TryPurchaseUnit` → `PlayRejected()` only when money is short | Queue-full/cannot-produce gives no feedback; out of scope, record only. |
| Cancel accepted | `FactionService.CancelBuilding` → `TryCancelCurrentUnit` | Also called internally by `ForceUpgradeStation` (refunds queued upgrades) → would false-fire "Tech Upgrade canceled". |
| Station upgrade done | `FactionService.BuildUnit` `LevelUnitRequest` → `_model.CurrentLevel++`; `ForceUpgradeStation` | `OnLevelUpgraded` also fires from save restore (`HumanProductionSaveSource`, `AiProductionSaveSource`), `HostedFactionLevel`, initial scope setup → do not use it as the cue trigger. |
| Refund when station lost | `BuildUnit` refund branch | Not a user cancel → no cancel cue. |
| Research completion | `FactionResearchModel.OnResearchCompleted` | Added to scope; verify completion/restore semantics before subscription. |
| Enemy station upgrade done | `EnemyFactionController` `Model.CurrentLevel++` (lines ~150, ~235) | Lives in AI player scope; local scope cannot resolve it. |
| Unit cap | `ReinforcementModel.CurrentUnitCapacity` setter → `OnCapacityChanged` | Setter also runs while restore re-adds units; no restore-complete signal exists anywhere. |
| Production per frame | `PlayerFactionModel.Advance` → `OnProductionChanged` every frame (allocates snapshot list) | Never subscribe audio to it or diff snapshots. |

### Expanded-scope audit (2026-10-26, second pass)

#### Source-data findings
- **`I000_EHD5503_ENG.wav` collision.** Catalog lists it for both `EHD_Research_Complete` ("Research Complete") and `EHD_Tech_Upgrade_Progress` ("Tech Upgrade in progress"); same `sha256 981f0841…`, same archive offset → one recording, two labels. Listen before import; one mapping is wrong.
  - Rebellion is clean: `RHD5503` = Research Complete, `RHD5504` = Tech Upgrade in progress (different hashes).
- **No `Research_Complete` folder under `Audio/Interface/Voices/`.** Completion lines live under `Audio/Unit_Ready/<Faction>/Shared/`:
  - `RHD_Research_Complete/I000_RHD5503_ENG.wav`; Empire `EHD_Research_Complete` was not extracted separately (it is the `EHD5503` collision).
  - `RHD_Upgrade_Complete/I000_RHD1602_ENG.wav`, `EHD_Upgrade_Complete/I000_EHD1602_ENG.wav` — both exist, distinct hashes.
  - **Decision (recommended):** research completion = `Upgrade_Complete` (`1602`) for both factions; pairs with the purchase line `Upgrade_Progress` (`1601`) and avoids the Empire collision.
  - Also available, not selected: `RHD_Tech_Upgrade_Complete` `I000_RHD5506_ENG.wav`, `EHD_Tech_Upgrade_Complete` `I000_EHD5505_ENG.wav` (alternative to `SPACE_STATION_UPGRADED`).
- Empire counterparts verified present for every additional battle event: `Space_Station_Under_Attack` (3), `Mining_Under_Attack`, `Mining_Destroyed`, `Capture_Mining` (2), `LOST_Mining`, `Reinforcements_En_Route`, `Reinforcements_Available` (2), `Unit_Destroyed`, `New_Construction`, `Defeated`, `Enemy_Fleet_Approaching`.
- Unit-ready (space only): Empire 17 WAV / 16 units (976 KB), Rebellion 17 WAV / 15 units (876 KB), under `Audio/Unit_Ready/<Faction>/Space/Unit_Complete_<Unit>/`. Generic fallbacks: `EHD_Construction_Complete`, `RHD_…`/`UHD_Construction_Complete`.
- Non-voice SFX all present under `Audio/Interface/SFX/<event>/`: `GUI_Toggle_UW_3.wav`, `GUI_Ability_Recharge_2.wav`, `GUI_Weapon_Ready.wav`, `GUI_Select_Empire.wav`, `GUI_Select_Rebel.wav` (≈ 620 KB total).

#### Hook audit — expanded events
| Event | Live source | Restore-safe trigger | Gap / rule |
|---|---|---|---|
| Research completed | `FactionService.BuildUnit` `ResearchUnitRequest` → `_research.Complete` | `BuildUnit` branch (new `ResearchCompleted` event) | `FactionResearchModel.OnResearchCompleted` also fires from `ResearchSave` restore and AI `EnemyFactionController` → never subscribe announcer to it. |
| Unit ready | `FactionService.BuildUnit` → `_reinforcementPool.Add` | `BuildUnit` default branch | `ReinforcementService.Add`/`OnReinforcementAdded` also run from `ReinforcementSaveSource.RestoreBeforeUnits` and cheats → do not use. Only purchased production; hangar fighters never pass here. |
| Reinforcements en route | `ReinforcementService.TrySpawnReinforcement` success | placement command | Same moment as unit placement; play en-route **or** unit-ready, never both for one unit. |
| Reinforcements available | none — no "newly available" concept | — | Define (e.g. reserve goes empty → non-empty from production) or drop. Overlaps unit-ready → recommend drop unless unit has no ready voice (fallback). |
| Station / mine under attack | `HealthModel.OnValueChanged` | per-entity damage check | `OnValueChanged` fires on regen/heal/restore too → announce only on hull/shield **decrease**; per-entity cooldown ≥ 20 s. Subscribe via entity facade from `IEntityLocator.EntityAdded` (owned stations/mines only), not `GetComponent`. |
| Mine destroyed | `HealthModel.OnDestroy` (`MiningFacility.HandleDestroyed`) | destroy event | Once per entity; skip `OnRelease`/battle teardown. |
| Mine captured / lost | `CaptureSiteModel` `Owner =` at capture (line ~74) / `ReleaseFacility` (~123) | add `Captured`/`Lost` events in those methods | No owner event today; `Restore` (~112) must not emit. Capture sites are `SiteFacilityType.Mining`/`BattleAsteroid` — announce Mining only. |
| Unit destroyed | `IEntityLocator.EntityRemoved` + `HealthModel.IsDestroyed` (pattern in `FactionService.HandleEntityRemoved`) | removal + destroyed | Ships only (no squadrons/fighters); global throttle ≥ 8 s. |
| New construction | `BuildUnit` `LevelUnitRequest` | same as `StationUpgraded` | Unlocks are level-gated only (`FactionData.AvailableLevel`); play only if any roster entry has `AvailableLevel == newLevel`. Research does not unlock units. Sequence after "Space Station upgraded", not simultaneous. |
| Defeat | `EndGameUiController.UpdateState(BattleResult)` (skips `BattleSimulation`) | `IObserver<BattleResult>` | Announcer observes the same notifier; `Outcome == EnemyVictory` for local side, once. |
| Enemy fleet approaching | per-ship `Ship.HandleEnemyDetected` → `AudioShipComponent` alarm SFX **and** `AudioDialogShipComponent` selected-ship `Alarm` voice | needs fleet-level detection | Plan text "adds speech to nonverbal alarm" is wrong: a voiced alarm already exists for selected ships. Needs a battle-level "first sighting after quiet period" signal; per-ship callbacks are too frequent to forward. Lowest priority. |
| Sensor jamming SFX | `ShipSfxData.abilities` entry `DauntlessSensorJamming` (id 39) exists | data only | Set `start.clip`; dedupe rule silences generic cue. |
| Ability recharge SFX | `ShipAbilityAudioProfile.restore` (local only) | data only | Optional clip swap across profiles; no code. |
| Superweapon ready chime | `SuperWeaponPresenter` → `PlayAnnouncement(OrbitalAvailable)` | same call site | Add `InterfaceSfxData.superWeaponReady`; play on `_ui` source alongside the announcer. |
| Faction select SFX | Main menu `SkirmishSlotSetup`/`SkirmishUi`, `SurvivalUi`, `SimulationSetupUi` | UI change handler | **No menu UI-SFX path exists** (`InterfaceSfxService` is skirmish-scoped; `ProjectScope` has only `AudioService`/`MusicService`). Needs a menu-scope sound owner. Human slot only; not AI slots or initial bind. Lowest priority; consider a separate task. |

#### Announcement priority (replaces the economy-only drop rule)
| Tier | Events | Behaviour |
|---|---|---|
| 1 | Defeat | Stops anything; plays once. |
| 2 | Orbital/superweapon, station under attack, mine destroyed | Interrupts tier ≤ 3. |
| 3 | Station upgraded, enemy station upgraded, research/upgrade complete, new construction, mine captured/lost, unit destroyed, unit cap | Drops while tier ≤ 3 is playing. |
| 4 | Purchase/cancel progress, unit ready, reinforcements, enemy fleet | Drops while anything plays. |
- `InterfaceSfxService.TryPlayAnnouncement(AudioClip, tier)` tracks the playing tier; one `AudioSource`; no queue.
- Unit-ready voices go through the announcer (2D), not `TryPlayVoice` — `TryPlayVoice` refuses while announcing and uses unit-voice cooldown.
- Per-event cooldowns live in `FactionAnnouncementService` (decision owner); tiers in `InterfaceSfxService` (playback owner).

#### Storage recount (supersedes the 15-clip estimate)
- Announcer voices: ≈ 25 WAV per faction × 2 ≈ 4.6 MB source. Unit-ready (space): 34 WAV ≈ 1.85 MB. New SFX: 5 WAV ≈ 0.62 MB + `GUI_TOGGLE_UW_2`. Total ≈ 7 MB source, ≈ 65 files.
- Imported Vorbis mono q≈0.5 ≈ 10 % → ≈ 0.7 MB build size.
- **Changed import rule:** use `CompressedInMemory` (not `DecompressOnLoad`) for announcer + unit-ready voices. Direct references load every faction set with the scene; `DecompressOnLoad` would hold ≈ 7 MB PCM resident vs ≈ 0.7 MB compressed. One 2D voice at a time → Vorbis decode cost negligible.
- `preloadAudioData: 1` for announcer events (avoid first-play delay); `preloadAudioData: 0`, `loadInBackground: 1` for unit-ready voices (≈ 34 clips, rarely all used).
- Addressables: still not justified (< 1 MB compressed). Revisit only if per-unit voices grow past ≈ 5 MB.
- Placement: unit-ready voices `Assets/Audio/Voices/UnitReady/<Faction>/`; reference from `ShipUnitAudioProfile` (per `ShipType`, already in `ShipSfxData.ships`) via a new `ready` clip field; missing → faction `Construction_Complete` fallback (covers Republic/Separatist ships per borrow decision).

### Architecture decision
- **Chosen:** new `FactionAnnouncementService` registered in `LocalPlayerScope.RegisterPlayerServices` (local human only), depends on `IInterfaceSfxService` (parent `SkirmishScope`), `InterfaceSfxData`, `PlayerFactionModel.FactionType`, local `FactionService`, `ReinforcementModel`.
  - Why: faction services live in the child `LocalPlayerScope`; `InterfaceSfxService` (skirmish scope) cannot resolve them. Keeps `InterfaceSfxService` as the playback owner.
  - Avoid: putting faction subscriptions into `InterfaceSfxService`; calling the announcer from `FactionUiController` (station completion and cheats have no UI path); MVP split.
- **Domain events on `FactionService`** (command path only, so restore is silent by construction):
  - `event Action<UnitRequest> PurchaseAccepted` — after `QueueUnit` succeeds.
  - `event Action<UnitRequest> PurchaseCanceled` — from public `CancelBuilding` only; `ForceUpgradeStation` uses a private non-emitting cancel.
  - `event Action StationUpgraded` — from `BuildUnit` `LevelUnitRequest` branch and `ForceUpgradeStation`.
  - Filter in the announcer by request type: `ResearchUnitRequest` → research lines, `LevelUnitRequest` → tech-upgrade lines; other types silent.
- Expanded scope requires auditing additional event hooks and extending the announcer fields below; the original architecture audit covered the initial seven events only.
- **Data (initial scope):** `[Serializable] FactionAnnouncerSet` (one file) with `upgradeProgress`, `upgradeCanceled`, `techUpgradeProgress`, `techUpgradeCanceled`, `stationUpgraded`, `enemyStationUpgraded`, `unitCapReached`; `InterfaceSfxData` gains `DictionaryWrapper<FactionType, FactionAnnouncerSet> factionAnnouncers` (same pattern as `ShipSfxData.voices`). No new ScriptableObject asset.
- **Enemy upgrade:** expose station level change on the space-station entity (facade event, e.g. `IStationLevelFacade.LevelUpgraded`) and observe enemy stations via `IEntityLocator` in the announcer. **Decided (user, 2026-10-26): always play** — no fog/vision check. Station facade event must skip restore (raise from `ApplyLevel` only when level increases after first apply).
- **Additional events on `FactionService`:** `ResearchCompleted(ResearchType)` and `UnitProduced(UnitRequest)` from `BuildUnit` (live completion only). `CaptureSiteModel`: `Captured`/`Lost` from capture/release paths, not `Restore`.
- **Data (expanded):** `FactionAnnouncerSet` also holds `upgradeComplete`, `newConstruction`, `stationUnderAttack` (random of 3), `miningUnderAttack`, `miningDestroyed`, `miningCaptured`, `miningLost`, `reinforcementsEnRoute`, `unitDestroyed`, `defeated`, `enemyFleetApproaching`, `constructionComplete`; use `RandomAudioClips` where multiple variations exist.
- **Unit cap:** see Decision → Unit cap; no polling.

### Performance (FPS)
- Ability mapping: zero runtime cost (data only); dedupe change removes one voice-slot request per activation.
- Announcer: event-driven, no `Tick`. One `AudioSource` already exists; do not add sources.
- Multi-select activation (e.g. 20 ships): bounded by `startsPerWindow = 3`; add `cooldown ≈ 0.1–0.15 s` on mapped `Start`/`End` profiles so one ability type plays once per burst.
- No LINQ/allocations in handlers; dictionary lookup by `FactionType` resolved once in `Initialize` (local faction is fixed per battle).

### Storage (Addressables / import)
- Keep direct clip references in `InterfaceSfxData`/`ShipSfxData` (both serialized on `SkirmishScope`). Do **not** add an Addressables group or `AssetReference`s.
  - Why (original scope estimate only; remeasure expanded inventory): 15 new clips, source WAVs ≈ 96 KB each (~2 s); imported Vorbis ≈ 15–25 KB each → ≈ 0.3 MB total. Per-faction async loading saves ≈ 0.15 MB and adds async/null-state paths.
  - Avoid: adding clips to the `Audio` group (`m_BundleMode: 0`, packed together) — duplicates scene-referenced clips into a bundle.
- Import settings for announcer voice clips (2D):
  - `forceToMono: 1`, Vorbis `quality ≈ 0.5`, `loadType: CompressedInMemory`, `preloadAudioData: 1`, `loadInBackground: 0` (superseded `DecompressOnLoad`; see Storage recount).
  - Existing `PlanetaryAttack` clips use `quality: 1`, stereo, `preloadAudioData: 0` — record, do not change in this task.
- `GUI_TOGGLE_UW_2.WAV`: match `GUI_TOGGLE_17` settings (`loadType: CompressedInMemory`, `quality 0.7`, `loadInBackground: 1`) for consistency with ability SFX.
- Placement (confirm with `PROJECT_ORGANIZATION`): `Assets/Audio/SFX/Abilities/GUI_TOGGLE_UW_2.WAV`; announcer lines `Assets/Audio/Voices/Announcer/Empire/` and `.../Rebellion/`.

## Decision

### Ship ability mapping — approved
| Existing project ability | Activate event → file | Deactivate event → file |
|---|---|---|
| `BoostShieldPower` and Power-to-Shields variants | `GUI_Toggle_Shields_On` → `GUI_SHIELDS_UP_1.WAV` | `GUI_Toggle_Shields_Off` → `GUI_SHIELDS_DOWN_1.WAV` |
| `Invulnerability` | `GUI_Toggle_Shields_On` → `GUI_SHIELDS_UP_1.WAV` | `GUI_Toggle_Shields_Off` → `GUI_SHIELDS_DOWN_1.WAV` |
| `BoostEnginePower`, `CorellianCorvettePowerToEngines`, `ImperialBoostEnginePower`, `CD273PowerToEngines` | `GUI_Toggle_Turbo_On` → `GUI_TOGGLE_17.WAV` | `GUI_Toggle_Turbo_Off` → `GUI_TOGGLE_18.WAV` |
| `LockSFoils` | `GUI_Toggle_Turbo_On` → `GUI_TOGGLE_17.WAV` | `GUI_Toggle_Turbo_Off` → `GUI_TOGGLE_18.WAV` |
| `BoostWeaponPower` and weapon-boost variants | `GUI_Weapon_Boost_SFX` → `GUI_TOGGLE_UW_2.WAV` | No dedicated matching off event identified; do not invent a pairing. |

- Shield variants: `NebulonBBoostShieldStrength`, `MonCalCruiserPowerToShields`, `HomeOnePowerToShields`, `MC80IndependencePowerToShields`, `CorellianBattlecruiserPowerToShields`, `AssaultFrigateMkIIPowerToShields`.
- Weapon-boost IDs: `VictoryBoostWeaponPower`, `TIEAvengerPowerToWeapons`, `ArquitensBoostWeaponPower`, `AcclamatorBoostWeaponPower`, `TectorBoostWeaponPower`. Review `ImperialVenatorIntensifyFirepower` and `NebulaOverchargeWeapons` settings before including them.
- Base ability mapping replaces existing `start.clip`/`end.clip` only; keep `execution` loops. `restore` remains unchanged unless the additional ability-ready replacement is selected after listening.
- Weapon boost `end`: keep the current `end` clip (no new pairing). With the dedupe rule, the generic `abilityCanceled` stays silent because `end` has a clip.
- `TURBO` is engine boost in EaW; Corellian Corvette XML applies ×2 speed. `SPOILER_LOCK` shares its toggle sounds.
- Weapon boost is an approved reuse of the expansion's `BLAST → GUI_Weapon_Boost_SFX`; not an original `POWER_TO_WEAPONS` mapping.
- Avoid assigning these clips to unrelated beam, salvo or targeting abilities by name alone.
- Priority: mapped `start.priority` must stay above every alarm (enforced by `ShipSfxDataCoverageTests.AbilityCuesOutrankEveryAlarm`).

### Research purchase announcements — user-selected
| Trigger | Rebellion event / file | Empire equivalent / file |
|---|---|---|
| Research purchase accepted / research starts | `RHD_Upgrade_Progress` / `I000_RHD1601_ENG.wav` | `EHD_Upgrade_Progress` / `I000_EHD1601_ENG.wav` |
| Research cancellation accepted | `RHD_Upgrade_Canceled` / `I000_RHD5705_ENG.wav` | `EHD_Upgrade_Canceled` / `I000_EHD5705_ENG.wav` |

- Spoken text: “Upgrade in Progress” / “Upgrade Canceled”.
- Trigger = `FactionService.PurchaseAccepted`/`PurchaseCanceled` with `ResearchUnitRequest`. Queuing a second tier behind an active one still announces "in progress".
- Failed or unaffordable purchase → existing rejection feedback, not progress announcement.
- Research completion is now in scope. Recommended: `RHD_Upgrade_Complete` `I000_RHD1602_ENG.wav` / `EHD_Upgrade_Complete` `I000_EHD1602_ENG.wav` (see `EHD5503` collision). Trigger = `FactionService.ResearchCompleted`. Do not substitute station completion.

### Station upgrade announcements — user-selected
| Trigger | Rebellion event / file | Empire equivalent / file |
|---|---|---|
| Own station upgrade starts | `RHD_Tech_Upgrade_Progress` / `I000_RHD5504_ENG.wav` | `EHD_Tech_Upgrade_Progress` / `I000_EHD5503_ENG.wav` ⚠ same recording as `EHD_Research_Complete`; listen-verify it says "Tech Upgrade in progress" |
| Own station upgrade cancellation accepted | `RHD_Tech_Upgrade_Canceled` / `I000_RHD5505_ENG.wav` | `EHD_Tech_Upgrade_Canceled` / `I000_EHD5504_ENG.wav` |
| Own station upgrade finishes | `RHD_SPACE_STATION_UPGRADED` / `I000_RHD6001_ENG.wav` | `EHD_SPACE_STATION_UPGRADED` / `I000_EHD6001_ENG.wav` |
| Enemy station upgrade finishes — found candidate | `RHD_ENEMY_SPACE_STATION_UPGRADED` / `I000_RHD6002_ENG.wav` | `EHD_ENEMY_SPACE_STATION_UPGRADED` / `I000_EHD6002_ENG.wav` |

- Text: “Tech Upgrade in progress”, “Tech Upgrade canceled”, “Space Station upgraded”, “Enemy Space Station has been upgraded”.
- Empire progress/cancel indices are `5503/5504`; Rebellion indices are `5504/5505`. Use explicit mappings.
- "Own station" for a hosted player (`ProductionHost != Owner`) = host's station; hosted players cannot buy `LevelUnitRequest`, so only the completion line can reach them. Decision: hosted local player hears "Space Station upgraded" via the host-station facade event, not via own `FactionService`.
- `ForceUpgradeStation` (cheat): announces "upgraded" once; its internal refund of queued upgrades must not announce "canceled".
- Enemy cue: **always plays** (user, 2026-10-26); no fog/visibility gate. Multiple enemy stations upgrading together → one line per cooldown.
- Other available enemy-base cues: `RHD/EHD_Enemy_Base_Shield_Offline`, `Enemy_Base_Shield_Penetrated`, `Enemy_Base_Shields_Up`; outside the requested mapping.

### Unit cap — user-selected
- Rebellion: `RHD_Unit_Cap_Reached` → `I000_RHD6705_ENG.wav`.
- Empire equivalent: `EHD_Unit_Cap_Reached` → `I000_EHD6705_ENG.wav`.
- Text: “At maximum unit capacity.”
- Trigger: local player transitions from below capacity to maximum capacity; do not replay every frame while full.
- **Decided (user, 2026-10-26):** play when `CurrentUnitCapacity >= MaxUnitCapacity` is reached, **and** when a placement attempt is refused by `CanSpawnUnit` for lack of room (e.g. 19/20 with a 2-cost unit). Both share one cooldown (≥ 3 s).
- **Restore gap:** no restore-complete signal exists; `ReinforcementSaveSource` re-adds capacity through the same setter. Trigger from the live placement path in `ReinforcementService` (successful placement / refused placement), not from `OnCapacityChanged`.
- Re-arm when `CurrentUnitCapacity` drops below max (read `OnCapacityChanged` for re-arm only).
- Keep rejected-purchase feedback separate.

### Additional battle announcements — added to scope 2026-10-26
- Event names below are verified Rebellion examples; map Empire counterparts explicitly from `audio_catalog.csv`, never infer filenames by replacing prefixes.
- These are new announcement hooks. Existing orbital announcements remain; ability-ready and enemy-detection feedback already exist.
- Prioritize station/mine warnings, research completion and unit-ready voices before secondary notifications.

| Priority | Addition | Source event(s) | Trigger / limits |
|---|---|---|---|
| High | Station under attack | `RHD_Space_Station_Under_Attack` | First damage after a quiet period; cooldown, not every hit. Three available variations. |
| High | Mining facility threatened / destroyed | `RHD_Mining_Under_Attack`, `RHD_Mining_Destroyed` | Local-owned mine takes damage / is destroyed; warn with cooldown, destruction once. |
| High | Mine captured / lost | `RHD_Capture_Mining`, `RHD_LOST_Mining` | Ownership changes; suppress initial map setup and restoration. |
| High | Research completed | `RHD_Research_Complete` or `RHD_Upgrade_Complete` | Successful completion; choose one matching the existing purchase voice, never both. |
| High | Unit ready | `Unit_Complete_*` | Purchased unit finishes production; use its own unit voice. Do not announce each carrier-launched fighter. |
| Medium | Reinforcements en route / available | `RHD_Reinforcements_En_Route`, `RHD_Reinforcements_Available` | Accepted deployment / newly available reinforcements; avoid duplicate unit-ready speech. |
| Medium | Unit destroyed | `RHD_Unit_Destroyed` | Significant local ship loss; throttle mass casualties; exclude cleanup/despawn. |
| Medium | New construction options | `RHD_New_Construction` | Research/station upgrade actually unlocks build options; not every level change. |
| Medium | Defeat | `RHD_Defeated` | Local player loses; once before results screen. |
| Secondary | Enemy fleet approaching | `RHD_Enemy_Fleet_Approaching` | Newly detected fleet only; respect fog. Adds speech to the existing nonverbal detection alarm. |

- `RHD_Capture_Mining` says “abandoned Mining facility”; use for neutral captures. Select a suitable alternative before using it for enemy-owned captures.
- Research completion examples: `I000_RHD5503_ENG.wav` (“Research Complete”), `I000_RHD1602_ENG.wav` (“Upgrade Complete”).
- Station warning examples: `I000_RHD0101/0102/0103_ENG.wav`; mine warning/destruction: `I000_RHD0506/0507_ENG.wav`.
- Mine capture examples: `I000_RHD5801/5805_ENG.wav`; mine lost: `I000_RHD5814_ENG.wav`.
- Unit-ready examples: Star Destroyer `U000_VSD0302_ENG.wav`, Calamari `U000_MCC0308_ENG.wav`; preserve XML event mapping despite misleading filename prefixes.
- Completion, combat-warning, defeat and unit-voice priorities need an expanded policy; the earlier economy drop rule alone does not define their ordering.
- Verify event hooks before integration; no per-frame production snapshots or audio polling.

### Additional non-voice effects — added to scope 2026-10-26
| Event → filename | Integration | Existing behavior / decision |
|---|---|---|
| `GUI_Sensor_Jamming_SFX` → `GUI_Toggle_UW_3.wav` | `DauntlessSensorJamming` activation | New clip for existing per-ability audio hook; avoid a second activation cue. |
| `GUI_Ability_Recharge_SFX` → `GUI_Ability_Recharge_2.wav` | Ability becomes ready | Optional replacement of existing `Restore` clip; no new ready hook. |
| `GUI_Weapon_Ready_SFX` → `GUI_Weapon_Ready.wav` | Superweapon-ready chime | Optional accompaniment; check overlap/level with ready announcement. |
| `GUI_Select_Empire_SFX` → `GUI_Select_Empire.wav` | Empire selected in setup | Faction-specific UI feedback; play on actual selection change. |
| `GUI_Select_Rebel_SFX` → `GUI_Select_Rebel.wav` | Rebellion selected in setup | Faction-specific UI feedback; suppress initial binding/restoration. |

- Source SFX path: `F:/Temp_audio/Empire_Rebellion_Audio/Audio/Interface/SFX/<event>/<filename>`.
- Optional replacements are included for listening evaluation; preserve existing clips until an explicit replacement choice is made.
- Already present, not missing systems: shield impacts/up/down, engines, hyperspace exit, weapon fire, hardpoint explosions and ability start/execution/end/restore.
- Expanded scope exceeds the original 15-clip estimate. Recount selected files, measure actual bytes and update import/memory estimates before implementation.

## Production-order follow-up — 2026-10-26
- Accepted local ship/squadron purchase → `UnitProduction` at `Progress` priority; existing 1.5 s cooldown and announcement arbitration apply. Failed purchases, structure purchases, cancellation and restore stay silent for this cue.
- Fixed split: Empire → “Constructing Unit” (`I000_EHD4606_ENG.wav`); Republic → “Unit in production” (`I000_EHD4613_ENG.wav`); Separatist → “Constructing Unit” (`I000_RHD4706_ENG.wav`); Rebellion → “Unit in production” (`I000_RHD4713_ENG.wav`). Overrides borrowed-Rebellion policy only for production orders.
- Imported four recordings into existing announcer folders; mono Vorbis quality 0.5, CompressedInMemory, preload enabled. Saved and verified all four references through Unity.
- Verification: compilation passed; `FactionAnnouncementTests` 30/30 passed, including four asset mappings, ship/squadron triggers and shared cooldown (`Logs/FactionAudio/tests-production-order.json`).

## Code Review — 2026-10-26
- Scope: uncommitted implementation on `main`; read-only review, no code changed, tests not re-run.
- Verified OK: restore-safe `FactionService` command events; `ForceUpgradeStation` silent refund; `CompleteLevelUpgrade` on human + AI models; generic ability cue only without specific clip (`AudioShipComponent.cs:99`); cap on refused placement + filling deploy (capacity updates synchronously in `ShipReinforcementPlacement.Spawn`); damage warnings only on hull/shield decrease via `HealthFacade.ApplyDamage` (all damage paths use it); ship loss via `OnDestroy` + 5 s camera memory; menu SFX only on human change.

### Findings (severity order)
1. **Mine destruction → two overlapping lines.** Death → `MiningFacility.OnRelease` → `CaptureSitePresenter.ReleaseFacility` → `SiteLost` → "Mining lost" (Notification); entity removal 0.35 s later (`DestroyDelay`) → "Mining destroyed" (Warning) interrupts it. `ReleaseFacility` is only called on facility death → happens on every destruction. "Mining destroyed" also uses delayed `EntityRemoved` (`FactionAnnouncementService.cs:276`), the path rejected for ship loss. Fix: drive one cue from `SiteLost`, drop the other.
2. **Orbital lines can be dropped (regression).** `PlayAnnouncement` used to always replace; now it routes as `Warning`, and equal tier is refused (`InterfaceSfxService.cs:71`). "Orbital firing" (incl. incoming hostile strike, `SuperWeaponPresenter.cs:218`) / "available" / "bought" drop while station-under-attack or another orbital line plays.
3. **Refused playback still consumes cooldown.** `Announce` sets `_readyAt` before `TryPlayAnnouncement` (`FactionAnnouncementService.cs:121`); damage handler sets `DamageReadyAt` first too. Refused station warning → station silent 20 s; also unit loss 8 s and once-only "Mining destroyed".
4. Low: "new construction" follow-up uses `async void` + `Task.Delay` (only use in codebase) and is cancelled by any announcement starting first, incl. a unit-ready line in the 50 ms gap.
5. Low: `FactionAnnouncementService` = 337 lines (> 200 guideline; candidate split: per-entity station/mine/ship/fleet tracking). `HandleUnitProduced` checks request type twice. `MenuFactionSfxService.cs:40` silent `_source == null` guard violates AGENTS null rule.
6. Test gaps: enemy station upgrade; hosted-player host-station upgrade; new-construction sequencing; research-completed cue; mine lost + destroyed overlap; orbital line during another announcement.

## Building, shield audibility and orbital ion — 2026-10-26
- Mining/battle asteroid construction: `CaptureSitesSystem.ConstructionStarted` after successful payment/start; local payer only, Progress priority, 1.5 s cooldown. Failed purchases and restore silent. Empire/Republic → `I000_EHD4616_ENG.wav` (“Building started”); Rebellion/Separatist → `I000_RHD4718_ENG.wav` (“Beginning construction”).
- Shield ability Start/End IDs `2,3,13,15,17,26,34,45`: local-only centered 2D, gain 1, priority 150, maxInstances 1, cooldown 0.15 s. Bypass weapon burst budget, camera/zoom gain and battle mix normalization. Voices retain precedence.
- Unity source priority: speech 32, local feedback 48, music/ambience 224. Music ducks to 20% during unit/announcer speech; fade state and user mixer volume remain intact; resets on battle disposal.
- Orbital ion: imported native `Unit_Ion_Cannon_Fire/GUN_Ion_Cannon_1.wav`; `PlanetaryIonCannon` SFX profile gain 0.8; launch plays at actual shot, not warmup/restore. Plasma remains awaiting clip selection.
- Plasma preview candidates: RaW `Audio/Space/SFX/Weapons/UNDERWORLD/GUN_UNDERWORLD_PLASMA_1.WAV` (ship plasma reuse) and EaW `Audio/Superweapons/Magnepulse_Cannon/SFX/Empire/Unit_Magnepulse_Fire/GUN_Magnepulse.wav` (magnepulse superweapon reuse).
- Unit-ready inventory: existing Star Destroyer, Victory, Acclamator Assault, Tartan, Nebulon-B, Mon Calamari, MC30c, Assault Frigate MkII and fighter lines. Republic/CIS lack per-unit ready assignments and use generic completion.
- Verification: compilation/import/save passed; announcement 36/36, ShipSfx 18/18, construction 13/13, superweapon 17/17, music duck 1/1 (`tests-music-ducking.json`). Evidence: `Logs/FactionAudio/tests-building-announcements.json`, `tests-shield-feedback.json`, `tests-building-triggers.json`, `tests-ion-orbital.json`.

## Weapon-specific orbital effects and speech — 2026-10-26
- Approved plasma effect imported: `GUN_UNDERWORLD_PLASMA_1.WAV`, gain 0.8; all orbital shots now resolve their `WeaponType` effect from `ShipSfxData`. Ion retains `GUN_Ion_Cannon_1.wav`; existing `GUN_HYPERVELOCITY_1.WAV` SHA-256 matches original source.
- Ion purchase/ready/fire speech: `I000_RHD6301_ENG.wav` (“Ion Cannon Charging”), `I000_RHD2401_ENG.wav` (“Ion Cannon Available”), `I000_RHD6302_ENG.wav` (“Ion Cannon Firing”).
- Hypervelocity purchase/ready/fire speech: `I000_EHD6303_ENG.wav` (“Hypervelocity Cannon Charging”), `I000_EHD2402_ENG.wav` (“Hypervelocity Gun Available”), `I000_EHD6304_ENG.wav` (“Hypervelocity Cannon Firing”).
- `InterfaceSfxData.GetOrbitalBought/Available/Firing(SuperWeaponType)` selects each weapon's speech; same routes handle incoming warnings, preserve priority, restore silence and duplicate suppression.
- No verified plasma-specific announcer set found in EaW/RaW catalogs. Plasma explicitly retains the existing generic PlanetaryAttack trio; RaW `I000_RHD9000/9001/9002` are those same recordings, not new alternatives.
- Six speech clips + plasma effect imported mono Vorbis 0.5, CompressedInMemory, preload enabled; Unity saved both data assets.
- Verification: compile/import/save passed; superweapon tests 44/44 (`Logs/FactionAudio/tests-orbital-effects-and-voices.json`), including real shot SFX for all three types, typed speech mappings and restore behavior.

## Implementation
1. Decided 2026-10-26: generic-cue dedupe (yes), Republic/Separatist borrow Empire/Rebellion voices, enemy upgrade always plays, unit cap = reach max + refused placement. Republic/Separatist → Rebellion voices. Still open: listen-verify `I000_EHD5503_ENG.wav`.
2. Build the expanded exact clip inventory from the Decision tables; verify faction counterparts, deduplicate, measure size, then import missing clips with the settings above. Original baseline: `GUI_TOGGLE_UW_2.WAV` + 14 announcer WAVs; reuse the 4 existing GUI clips by GUID.
3. `ShipSfxData.asset`: set mapped `start.clip`/`end.clip`, add `cooldown ≈ 0.1–0.15 s`; `AudioShipComponent`: generic cue only when `Start`/`End` has no clip.
4. `FactionAnnouncerSet` + `InterfaceSfxData.factionAnnouncers`; announcer drop-while-announcing rule + per-event cooldown in `InterfaceSfxService` (new `TryPlayAnnouncement` returning `bool`; orbital calls keep current `PlayAnnouncement`).
5. `FactionService` events (`PurchaseAccepted`, `PurchaseCanceled`, `StationUpgraded`); private non-emitting cancel for `ForceUpgradeStation`.
6. `FactionAnnouncementService` in `LocalPlayerScope`; research/station lines; cap from `ReinforcementService` placement path.
7. Station facade level event → enemy upgrade + hosted-player completion, with agreed visibility policy.
8. Add high-priority station/mine warnings, research completion and unit-ready hooks; then reinforcement, loss, unlock, defeat and enemy-fleet cues. Audit each trigger for ownership, fog, restore and duplicate suppression; extend announcer priority rules.
9. Evaluate and map additional jamming/ready/faction-selection effects; ready cues reuse existing hooks. Unit-ready voice routing must respect announcer precedence.
10. Tests (EditMode): mapping coverage for all listed `ShipAbilityId`s; Empire/Rebellion announcer sets complete; Republic/Separatist policy explicit; dedupe; `ForceUpgradeStation` emits no cancel; restore path (`RestoreQueue`, save `CurrentLevel`) emits nothing; cap one-shot + re-arm. Compile + EditMode only; scene-safety rules before tests; no agent Play Mode. User does listening acceptance.

## Edge Cases
- Duplicate callbacks, multi-selection activation and rapid cancel/rebuy must not stack identical announcements → per-event cooldown + drop-while-announcing.
- Natural ability expiry → mapped `End` (already the `Recovering` transition). Failed activation, despawn → no cue. Save restore: verify `InitializeAudio` snapshots restored ability states before `HandleAbilityChanged` runs, else restore plays `Start` (pre-existing risk; check, don't assume).
- Same WAV used by several abilities → one imported asset, multiple references; cooldown is per profile, so different abilities can still overlap.
- Empire/Rebellion use their own announcers; Republic/Separatist use the Rebellion set.
- Orbital announcements must win over economy announcements.
- Local player defeated/station destroyed → refunds from `BuildUnit` are not cancellations; no voice.

## Files
- Code: `Assets/Scripts/Services/Audio/InterfaceSfxService.cs`, `InterfaceSfxData.cs`, new `FactionAnnouncerSet.cs`, new `FactionAnnouncementService.cs` (folder per `PROJECT_ORGANIZATION`), `Assets/Scripts/Components/Ship/Audio/AudioShipComponent.cs`, `Assets/Scripts/Services/Faction/FactionService.cs`, `Assets/Scripts/Services/Reinforcement/ReinforcementService.cs`, `Assets/Scripts/Services/VContainer/Scopes/Players/LocalPlayerScope.cs`, station facade under `Assets/Scripts/Entities/SpaceStation/`.
- Data: `Assets/Settings/Data/Models/Audio/ShipSfxData.asset`, `InterfaceSfxData.asset`.
- Tests: `Assets/Scripts/Tests/Editor/ShipSfxDataCoverageTests.cs` (extend), new announcer tests.
- Voice root: `F:/Temp_audio/Empire_Rebellion_Audio/Audio/Interface/Voices`.
- Exact voice path: `<root>/Rebellion/<RHD event>/<RHD filename>` or `<root>/Empire/<EHD event>/<EHD filename>` from the tables.
- Shields: `F:/Temp_audio/Republic_at_War_1129810972/Audio/Space/SFX/Abilities/Shields/` (already imported).
- Turbo: `F:/Temp_audio/Republic_at_War_1129810972/Audio/Space/SFX/Abilities/Controls/` (already imported).
- Weapon boost: `F:/Temp_audio/Republic_at_War_1129810972/Audio/Interface/SFX/UI/Gui_Toggle_Uw/GUI_TOGGLE_UW_2.WAV`.
- Evidence: `F:/Temp_audio/Empire_Rebellion_Audio/audio_catalog.csv`, `Reference_XML/GameData/config.meg/SFXEVENTSHUD.XML`, `SFXEVENTSGUI.XML`, `FACTIONS.XML`.
- Ability evidence: `F:/Temp_audio/Republic_at_War_1129810972/ability_sfx_mapping.csv` and `sfx_event_object_mapping.csv`.
- Existing project IDs: `Assets/Scripts/Services/ShipAbilities/ShipAbilityId.cs`.

## TODO
- [x] Record approved ship-ability mapping.
- [x] Record selected research, station and unit-cap triggers.
- [x] Find enemy-station upgrade announcements; verify Empire counterpart files exist.
- [x] Audit live hooks and existing assets (2026-10-26; see `Validation`).
- [x] Decide: generic-cue dedupe, Republic/Separatist policy, enemy visibility policy, unit-cap definition (2026-10-26).
- [x] Second-pass audit of expanded hooks, clip inventory, storage and announcement priorities (2026-10-26; see `Expanded-scope audit`).
- [x] Decide Republic/Separatist borrow mapping → Rebellion voices (2026-10-26).
- [ ] Listen-verify `I000_EHD5503_ENG.wav` (Research Complete vs Tech Upgrade in progress).
- [x] Import/reuse mapped clips and wire ability/faction announcement events.
- [x] Verify one-shot transitions, cancellation, faction routing, cap re-arm, save restoration and missing-reference coverage.
- [x] Add all proposed battle announcements and non-voice effects to scope (2026-10-26).
- [x] Integrate high-priority station/mine/research-completion/unit-ready cues, then medium/secondary notifications.
- [x] Evaluate jamming, ability-ready, superweapon-ready and faction-selection effects; distinguish replacement from new behavior.
- [x] Verify combat-warning cooldowns, neutral-mine wording, production vs carrier spawns, mass-loss throttling, unlock changes, defeat once, fog and restore suppression.
- [ ] User listening acceptance: volume, priority, timing, duplicate suppression.
- [ ] Fix code-review findings 1–3 (mine lost/destroyed overlap, orbital equal-tier drop, cooldown on refused playback); see `Code Review — 2026-10-26`.
- [ ] Decide low-severity review items 4–5 and add the missing tests from finding 6.
