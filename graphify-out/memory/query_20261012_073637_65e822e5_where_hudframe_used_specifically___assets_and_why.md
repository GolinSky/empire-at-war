---
type: "query"
date: "2026-10-12T07:36:37.488199+00:00"
question: "where HudFrame used specifically - assets and why - what exactly it doing"
contributor: "graphify"
outcome: "useful"
source_nodes: ["HudFrame", "FactionUnitUi"]
---

# Q: where HudFrame used specifically - assets and why - what exactly it doing

## Answer

Exact graph labels used: HudFrame and FactionUnitUi; live serialized assets and symbols verify the details. 29 direct HudFrame components in 10 prefab assets: CoreGameUi 14 (4 dark panels and 10 amber transparent pending/targeting outlines), SettingsUi 6 (category navigation, four category panels and confirmation card), SkirmishUi 2 (PlayersField and BattleSettings), MainMenuUi 1 (main panel, accentWidth=0), MiniMapUi 1, ReinforcementUi 1, EndGameUi 1, PauseMenuUi 1, TooltipUi 1, FactionUnit 1 (QueuedHighlight/Outline). CoreGameUi panels: UpperPanel, Panel, ShipActions, OrbitalPopup. Amber outlines: ShipActions/WaypointMove, Stop, AttackMove, Guard, Retreat, Hunt under PendingHighlight/Outline; OrbitalPopup/HypervelocityGunButton, PlasmaCannonButton, IonCannonButton under PendingHighlight/Outline; MiddlePanel/OrbitalButton/TargetingIndicator/Outline. HudFrame OnPopulateMesh clears mesh, draws an eight-corner beveled fill, outer border 1 UI unit thick, inner border inset 4 with alpha multiplied by 0.6, and top accent 2 high with configured width clamped to available width. Standard panel bevel 10/accent 80; settings 12/104; skirmish 14/104; amber overlays have transparent fill, bevel 4 and accent 80. UnitActionButton.SetPending and SuperWeaponButton.SetPending toggle parent overlays; SuperWeaponsView.SetPending toggles orbital targeting indicator; FactionUnitUi.RenderAvailability toggles queued overlay. No files in Assets changed.

## Outcome

- Signal: useful

## Source Nodes

- HudFrame
- FactionUnitUi