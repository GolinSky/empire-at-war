---
category: Features
status: done
created: 2026-10-28
completed: 2026-10-28
---
# Structure Construction Flow

## Goal

- Mining facilities, defense platforms and capture-site facilities show construction on the map; human ordinary structures also require production time before placement.

## Implementation

- [x] `CaptureSiteData.siteSettings`: one radius/capture duration/facility height entry per kind.
- [x] `ConstructionComponent`: progress, completion, explicit renderer/material bindings; all 12 structure prefabs migrated.
- [x] `ConstructionReveal`: full translucent white silhouette, bottom-to-top green hologram and bright build edge; normal materials restored on completion.
- [x] `HealthModel`: hull and hardpoint capacity grows from 10% to 100%, retaining absolute damage; destroyed hardpoints stay destroyed.
- [x] Entities suppress income, firing and shields while constructing; completion enables them.
- [x] Human defense platforms and mining facilities use the production queue before entering placement reserve; AI spawns immediately. Factory defaults and capture-site overrides use data build times.
- [x] Follow-up 2026-10-28: unfinished geometry remains translucent white above the green build front; hardpoint overlays and picking stay disabled during construction.
- [x] Capture-site facility spawns on purchase; nine obsolete framework roots removed; occupied sites cannot be captured; destruction returns Neutral without refund.
- [x] Structure saves store progress and duration; restore construction before health fractions. Constructing sites reconnect to saved facilities.

## Important Values

- Mining: radius `60`, capture `10 s`, facility height offset `-60`.
- BattleAsteroid: radius `60`, capture `12 s`, facility height offset `0`.
- Satellite: radius `100`, capture `6 s`, facility height offset `0`.
- Capture-site duration: `SiteFacilityCost.BuildTime`; reinforcement duration: catalog `FactionData.BuildTime`.
- Save version `4`; older saves rejected by existing compatibility policy.

## Verification

- Follow-up 2026-10-28: queue/capacity/cancellation fixture expectations updated; Unity compilation finished with `scriptCompilationFailed = false`, shader messages empty, no new Console errors after refresh. Tests and Play Mode not run.
- Human ordinary structures now wait for catalog build time in production, then retain the existing on-map construction duration after placement.

- Unity compilation succeeded; `scriptCompilationFailed = false`; `ConstructionReveal` shader diagnostics empty.
- All 12 prefab construction/material/renderer bindings saved and reloaded; three data entries persisted; old framework roots absent.
- No new Unity import/serialization errors after fixing material-property-block initialization.
- Static mine/satellite previews at `25%`, `65%` and `100%`: clipped hologram and final normal materials inspected.
- Health, capture and production fixture sources updated; tests and Play Mode not run under repository policy.
