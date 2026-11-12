---
category: Features
status: done
created: 2026-11-12
completed: 2026-11-12
acceptance: pending-user
---
# Tactical Markers (Ping / Beacon)

## Goal

- Six typed team markers (Look, Attack, Capture, Defend, Rally, Retreat) placed on the battlefield or minimap.
- One system for Player → Player (online), Player → allied AI, AI → Player.
- Markers are communication, not unit orders: they never move units directly.

## Marker Set

| Type | Color | Icon | Default lifetime | Sound | AI meaning |
|---|---|---|---|---|---|
| `Look` | Grey `#C8CDD4` | eye | 8 s | info | none (awareness only) |
| `Attack` | Red `#FF4B4B` | crosshair | 8 s | urgent | attack target/area if force allows |
| `Capture` | Yellow `#FFC531` | flag | 30 s or until site owned by team | info | capture objective |
| `Defend` | Blue `#3F8CFF` | shield | 30 s or until no hostiles nearby | urgent | defend unit/site/area |
| `Rally` | Cyan `#2FE0D0` | map pin | 15 s | info | gather fleet at point |
| `Retreat` | Purple `#B060FF` | U-turn arrow | 8 s | urgent | pull back from area |

- Colors are starting values; tune against HUD palette in `UI_UX_GUIDELINES`.

## Visual Style (from reference sheet 2026-11-12)

- **Icon frame:** octagon, thin glowing border in type color, dark translucent fill, small notches top/bottom; white-ish glyph tinted to type color.
- **World marker:** octagon icon billboard → vertical light beam → dashed ground ring in type color; on place: one expanding ring pulse (0.6 s), then static ring with slow dash rotation; fade out last 1 s.
- **Minimap / log icon:** flat rounded-square frame + glyph (bottom row of reference sheet); expanding ping circle on arrival.
- **Radial menu:** 6 octagon buttons around a center hub (`B / PING`), order clockwise from top: Look, Attack, Capture, Defend, Rally, Retreat; hovered slice glows + scales 1.1.
- **Sender tag:** small label under world icon with sender name in player team color.
- Assets: 6 glyph sprites + octagon frame + square frame (9-slice), one beam/ring material (additive, unlit, URP). Generate in Penpot (allowed project only) or vector → PNG; place under `Assets/Art/Textures/Ui/TacticalMarkers`.

## Controls

- Tap `B` → `Look` marker at cursor (or under minimap cursor).
- Hold `B` (> 0.2 s) → radial opens at cursor; move toward a slice; release → place selected type at the point where the radial opened. Release in hub → cancel.
- `Esc` / RMB while radial open → cancel (`ICancelInput`).
- Cursor over a unit/station/site → marker attaches to that target (follows it; target id stored).
- Click marker card in notification log → camera centers on marker.
- `B` is free in `EmpireAtWar.inputactions`; add as rebindable `PlaceMarker` action (shows in Settings bindings).

## Rules

- Visible only to the sender's team (`IPlayerRelations.IsAllied`). Enemies never receive marker data.
- Max 4 active temporary markers per player; the oldest is replaced. Rate limit: 3 markers / 2 s per player; extra are dropped silently on host.
- Target attach only if target is visible to sender's team (`IVisionService.IsVisible`); otherwise marker becomes a plain point.
- Point clamped to map bounds (`IMapModelObserver.SizeRange`).
- Not saved: markers are transient, cleared on battle end / load.
- Solo vs AI enemy (no allies): markers still place locally as personal notes; no network or AI traffic.

## Architecture

```
Services/TacticalMarkers
├── TacticalMarkerType (enum : byte)          Look, Attack, Capture, Defend, Rally, Retreat
├── TacticalMarker                            Id, Type, Sender, Team, Point, TargetId, ExpiresAt, State
├── TacticalMarkerSettings (SO)               lifetimes, colors, caps, rate limit, sounds, icons
├── TacticalMarkerRegistry                    authority list: add/replace/expire/resolve; events Added/Removed
├── ITacticalMarkerObserver                   read-only view for UI, minimap, AI
├── PlaceMarkerCommand                        Issuer, Type, Point, TargetId → IPlayerCommandSink.Submit
├── MarkerResolutionWatcher                   ends Capture/Defend when objective resolved
└── AllyAiMarkerSource                        AI side: reads markers of own team, emits replies + own markers
Entities/TacticalMarkers
├── MarkerInputController                     B tap/hold, world + minimap pick, target snap
├── MarkerRadialUi / MarkerRadialView         radial menu (MVP: real UI state)
├── WorldMarkerView (pooled)                  octagon billboard, beam, dashed ring, pulse
└── MiniMapMarkerPresenter                    via existing MiniMap MarkView/BaseMiniMapPresenter
Services/Networking
├── Messages/TacticalMarkerMessage            host → allied clients (add / remove)
└── HostTacticalMarkerSender / ClientTacticalMarkerReceiver
```

- Command path reuses existing flow: `IPlayerCommandSink.Submit(PlaceMarkerCommand)` → offline/host `AuthorityCommandSink` → registry; online client `NetworkCommandSink` → `PlayerCommandMessage` (`PlayerCommandKind.PlaceMarker`) → `HostCommandReceiver` issuer check → `AuthorityCommandSink`.
- Host validation in authority: issuer is real player, rate limit, cap, map bounds, target visibility.
- Host → clients: new `HostBroadcast.ToTeam(online, team, message)` — sends only to connections whose player is allied with marker team (`INetworkPlayerDirectory` + `IPlayerRelations`). Not broadcast-then-hide.
- Client keeps a local mirror registry; lifetime counted locally from message; explicit remove message for resolved/replaced markers.
- Notifications: `IBattleNotifications.Post` card "‹Sender›: Attack here" with marker icon key; urgent types use `BattleNotificationPriority.Urgent`, info types `Progress` (or nearest existing).
- Minimap: new `MarkType`/view reusing `MiniMapData` marker pipeline (not `MiniMapAlertType`; alerts are short and untyped).

## AI Integration

- Marker to AI = **request**, not order. Each allied AI (`EnemyService` per `PlayerSlot`) evaluates:
  - visibility/known intel (`HostileIntelModel`), own available force vs threat at point (`CombatMatchup` / `ForceCompositionBuilder`), distance, current priorities.
- Result → reply marker-ack card: `Accepted` ("On my way"), `Delayed` ("Regrouping, 30 s"), `Declined` ("Too strong — need support").
- Mapping into existing AI:
  - `Capture` → extra `EnemyCaptureObjective` with priority bonus via new `IEnemyCaptureObjectiveSource` (`AllyAiMarkerSource`).
  - `Defend` → threatened-objective entry (same source, `threatened = true`).
  - `Attack` → target/area bias in `EnemyStrategicContextBuilder` / task-force target pick.
  - `Rally` → task-force staging point override for next assault.
  - `Retreat` → AI groups inside marker radius retreat (`IssueRetreat`) if losing locally; else Declined.
  - `Look` → intel hint only (raises priority of scouting that area).
- AI → Player markers (rate-limited, max 1 per AI / 10 s):
  - enemy capital ship detected → `Look` on it;
  - own station/site under attack → `Defend`;
  - assault launching → `Rally` then `Attack`;
  - AI retreating → `Retreat`;
  - capture complete → short info card only.
- AI runs only on host; its markers flow through the same registry → team broadcast.

## Implementation Steps

- [x] 0. Execute all phases authorized 2026-11-12; plan defaults retained (no enemy hints, no free text). Human + AI same-team support verified in `SkirmishModel.Create` and `PlayerRoster.IsAllied`.
- [x] 1. Model + settings: `TacticalMarkerType`, `TacticalMarker`, `TacticalMarkerSettings` SO, `TacticalMarkerRegistry` (cap, rate limit, expiry, resolve). → verify: compile.
- [x] 2. Command path offline/host: `PlaceMarkerCommand`, `IPlayerCommandSink.Submit`, `AuthorityCommandSink` validation. → verify: compile, EditMode tests written (not run).
- [x] 3. Input: `PlaceMarker` action (`B`), tap/hold, target snap, minimap placement through `IMiniMapInputObserver`, Esc cancel. → verify: bindings appear in Settings.
- [x] 4. Art: 6 glyphs, octagon + square frames, beam/ring material, 2 sounds (info, urgent). → saved/imported; user visual/audio approval queued.
- [x] 5. Views: radial menu, pooled `WorldMarkerView`, minimap marker, notification card with click-to-focus. Read `UI_UX_GUIDELINES` + `UI_CODE_BUILD_GUIDE` first. → verify: prefabs saved/imported, no console errors.
- [x] 6. Online: `PlayerCommandKind.PlaceMarker`, serialization, `TacticalMarkerMessage`, `HostBroadcast.ToTeam`, client receiver + mirror registry, disconnect cleanup. → compile verified; user 2-client check queued.
- [x] 7. AI receive: `AllyAiMarkerSource`, evaluation → Accepted/Delayed/Declined reply. → verify: `EnemyAi` tests written.
- [x] 8. AI send: event triggers + rate limit.
- [x] 9. Tutorial hint (optional): one event hint the first time an ally places a marker.

## Tests (categories)

- `BattleInput` (input, radial), `Ui` (views, notifications, minimap), `Networking` (team-only delivery, issuer/forged target drop), `EnemyAi` (accept/decline mapping), `Core` if shared infra changes.
- Assert wiring/relationships only (no tuned lifetimes/caps, per no-stat-pinning rule).
- User explicitly authorized targeted test execution on 2026-11-12; results recorded below.

## Edge Cases

- Target dies → marker drops to last position, then expires.
- Target becomes invisible → marker keeps last known point (no live tracking leak).
- Sender disconnects / abandoned-player takeover → their markers expire normally; takeover AI may place new ones.
- Marker on hostile-owned site with `Capture` → valid; on own site → treat as `Defend`.
- Replay of same marker type at near-same point (< 50 m, < 2 s) → refresh existing instead of new.
- Paused game → lifetimes freeze (use battle time, not unscaled time).
- Cinematic camera / input lock (`IInputLock`) → placement disabled.

## Open Questions

- Confirmed in live source 2026-11-12: skirmish slots choose teams independently; `PlayerRoster.IsAllied` compares team IDs.
- Should enemies get a generic "enemy pinged" hint? Default: no.
- Chat-free text labels (Beyond All Reason style) — out of scope for v1.

## Files (expected touch points)

- `Assets/Scripts/Services/PlayerCommands/IPlayerCommandSink.cs`, `AuthorityCommandSink.cs`
- `Assets/Scripts/Services/Networking/NetworkCommandSink.cs`, `HostCommandReceiver.cs`, `HostBroadcast.cs`, `Messages/PlayerCommandKind.cs`, `Messages/PlayerCommandSerialization.cs`
- `Assets/Scripts/Entities/MiniMap/*` (new mark type + presenter)
- `Assets/Scripts/Services/BattleNotifications/*` (marker card + focus)
- `Assets/Scripts/Services/Enemy/EnemyStrategicContextBuilder.cs`, `EnemySquadronCommander.cs` (via objective source)
- `Assets/Settings/Input/EmpireAtWar.inputactions`
- VContainer battle modules (registration)

## Implementation

- Completed phases 0–9 on 2026-11-12 using three parallel agents; parent integrated battle/AI scope registrations, imports and saved assets.
- Core/online: validated commands, team-filtered delivery, late-load snapshots, client mirrors, target visibility/death, objective resolution, per-player limits and transient load/disconnect cleanup.
- Input/UI: rebindable B tap/hold, corner-clamped radial with neutral opening, world/minimap entity/site snap, pooled world views, typed minimap pulse, sender tags, focusable cards and one-shot allied marker tutorial event.
- AI: all six request mappings, force/intel/distance/priority and route-safety evaluation, acknowledgements, capped automatic emissions and clock-reset cleanup.
- Art: six generated glyphs, octagon/square frames, minimap variants, dashed ring mesh, additive URP material and two synthesized cues. Canonical type-first texture folder replaces the draft `Assets/Art/Ui` path.
- Verification: Unity compilation succeeded (`scriptCompilationFailed=false`); zero shader diagnostics; saved prefab/settings, all GUID references, Addressable/key mapping, minimap icons, mixer and B binding validated. Final build/preview produced no new Console errors.
- Initial implementation authored tests without execution under the local override; the user subsequently authorized the review test runs below. No Play Mode or two-client session run. User-only gates → [[TODOs/User_Acceptance_Queue]].
- Reusable guidance extracted → [[Architecture/TACTICAL_MARKERS]]. Evidence: `Logs/TacticalMarkers/verification.json`, `marker-art-preview.png`, `radial-preview.png`; editor authoring: `TacticalMarkerAssetBuilder.Build`, `Validate`, `RenderPreview`.

## Review — 2026-11-12

- Reopened after user-reported startup `InvalidOperationException: ValueFactory attempted to access the Value property`.
- Scope: independent DI/core, UI/input and AI/network reviews; fix confirmed bugs/missing parts and run authorized BattleInput, Ui, Networking, EnemyAi and Core tests.
- Startup cycle confirmed: `CaptureSitesSystem → AuthorityCommandSink → TacticalMarkerRegistry → TacticalMarkerTargets → ICaptureSiteDirectory → CaptureSitesSystem`.

### Review fixes

- `AuthorityCommandSink` dispatches through lifecycle-registered `ITacticalMarkerCommandHandler` in `PlayerCommandHandlers`; it no longer constructs the registry while capture sites are being injected.
- Site targets use stable directory indices, matching capture construction/save identity; multiple mines and satellites sharing one `PointId` no longer collide.
- `MarkerInputController` retains the radial blocker through pointer processing on cancel; RMB cannot leak a move order. Esc owns cancellation from the initial B press.
- AI Capture/Defend requests bias canonical objectives after ordinary sources, preserving IDs, capture radii, progress and task-group reservations; area-only requests approach the point.
- Registry observes battle state, clears on Loading/Ended and rejects late marker traffic after Ended.
- Added actual battle-container composition, battle-end, duplicate-site, input-cancel, canonical-objective, actual host team delivery and forged-issuer regressions. All five targeted categories passed.

- EditMode composition exposed pooled-view teardown using runtime-only destruction; fixed with the project play/edit destruction convention.
- Authorized EditMode results: BattleInput 296/296, Ui 276/276, Networking 30/30, EnemyAi 360/360, Core 151/151; zero failed/skipped in final category runs. Evidence: `Logs/TacticalMarkers/review-tests/*.json`.
- Real saved-battle composition resolves capture sites and routes a marker command after lifecycle initialization; reported startup DI cycle regression passes in BattleInput and Core.
- Unity compilation succeeds; source whitespace checks pass. Networking fixture initializes KCP config explicitly for EditMode, without opening sockets.
- Review complete; reusable fixes recorded in [[Architecture/TACTICAL_MARKERS]]. Remaining in-game, two-client and visual/audio acceptance → [[TODOs/User_Acceptance_Queue]].


## Review 2 — 2026-11-12 (source review, no fixes applied)

- Scope: registry, targets, resolution watcher, command/network path, input + radial, world/minimap presenters, ally AI source/emitter/executor hooks. Console: 0 errors. Tests not run.
- Plan coverage: all 9 steps present in source; wiring (`SkirmishScope`, `NetworkBattleRegistrations`, `AiPlayerScope`), `B` binding, art, sounds, prefabs and settings asset exist.

### Findings (most severe first)

1. **Defend resolves after ~1 s on a quiet target.** `MarkerResolutionWatcher` resolves `Defend` once age ≥ 1 s and no visible hostile is within `ObjectiveRadius`. A pre-emptive "defend my station" ping vanishes almost at once. `TryPlace` turns `Capture` on an allied site into `Defend`, so those vanish too. Fix: resolve only after hostiles were seen and then cleared, or never before the full lifetime.
2. **"On my way" with no action for Capture/Defend.** `AllyAiMarkerSource` accepts them but only adds `PriorityBonus` to capture objectives; `SelectRequest` skips both types. The fleet moves only if the strategy picks `CaptureZone` (rules 0.7/0.6, below AssaultBase/HuntFleet). Fix: route accepted Capture/Defend through `ExecuteMarker` like Attack/Rally, or reply `Delayed` while the group's state is not `CaptureZone`.
3. **An Attack ping on a unit attack-moves to a point.** `EnemyTaskForceExecutor.ExecuteMarker` calls `IssueAttackMove(point)` and ignores `TargetId`. Ships can engage other units on the way. Fix: when the target is a known hostile entity, call `IssueAttack(receivers, target)`.
4. **Reply spam.** AI requests are re-checked every 3 s, and every status change posts a card, so Accepted ↔ Delayed flips repeat. Each allied AI also answers other AIs' markers (for example an AI's Look on a capital ship gets "On my way" from every other ally). `CaptureCompleted` posts a card for every AI capture with no rate limit. Fix: one reply per marker (or upgrades only), human senders only, and a rate limit on capture cards.
5. **Own pings notify yourself.** `MarkerWorldPresenter.Added` posts a card and plays the sound for your own markers. A client that loads late also gets cards and sounds for every marker that is already up. Fix: skip the card for `_local.IsLocal(sender)`; skip the sound and card for the late-join snapshot (`Age` > 0.5 s).
6. **Wrong reply text.** The `Delayed` card says "Regrouping, 30 s", but there is no 30 s timer; Delayed means no ships, home under pressure, or target far away.
7. **Network traffic.** A marker on a moving target sends a reliable `TacticalMarkerMessage` to allies every 0.2 s (`TRACK_INTERVAL`). This is acceptable at a cap of 4 per player; a ≥ 25 m change threshold would cut it.

### Verified OK

- Team-only delivery (`HostBroadcast.ToTeam`), issuer check, cap/rate limit/repeat refresh, NaN and bounds clamping, hidden targets keep their last point, dead target → point marker.
- Late-join snapshot, disconnect clear, clear on save restore, `BattleState.Ended`/`Loading` clear, radial slice mapping (clockwise from top = enum order), hub release cancels, RMB/Esc cancel without leaking an order.

### Review 2 fixes (2026-11-12)

1. `MarkerResolutionWatcher` resolves `Defend` only after hostiles were seen near it and then cleared (`_contested` set, cleaned on `Removed`). Quiet Defend → full lifetime.
2. `AllyAiMarkerSource.SelectRequest` now also returns accepted `Capture`/`Defend` (Defend allowed under home station pressure). "On my way" is posted only when a task group takes the request (`Take`); if none takes it within 3 s → one "Delayed" card, upgraded to Accepted later if a group takes it.
3. `EnemyTaskForceExecutor.ExecuteMarker(marker, attackTarget, context)`: Attack on a live hostile unit → `IssueAttack(target)` (no re-issue as it moves); Attack on a point and Defend → attack-move; Capture/Rally → move.
4. Replies: one answer per marker, then only an upgrade to Accepted; AI answers human senders only (`!IsAi`); `AllyAiMarkerEmitter` capture cards limited to one per 30 s (`CAPTURE_REPORT_INTERVAL`).
5. `MarkerWorldPresenter`: no card for your own markers (sound kept as feedback); markers older than 0.5 s on arrival (late-join snapshot) show without sound or card.
6. Delayed text → "Busy now — will come when free".
7. `HostTacticalMarkerSender` sends tracking moves only after ≥ 25 m (`RESEND_DISTANCE`); adds, refreshes, retargets and removals always sent.

- Verification: Unity recompile completed, 0 console errors. Tests not run (local override). Categories touched: `EnemyAi`, `Networking`, `Ui`.
- Reusable guidance: none beyond [[Architecture/TACTICAL_MARKERS]]; acceptance gates unchanged in [[TODOs/User_Acceptance_Queue]].
