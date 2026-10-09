---
category: Features
status: done
created: 2026-10-09
completed: 2026-10-09
---
# ISD II Replacement Source Mapping

## Goal

- Replace Empire ship `ISDII = 205` from Workshop `1770851727`; preserve live balance and registrations.
- Source root: `D:/SteamLibrary/steamapps/workshop/content/32470/1770851727/Data`.

## Implementation

- XML chain: `SFX_Base → Vessel_Base → Capital_Base → Star_Destroyer_Template → Star_Destroyer_II_Template → Star_Destroyer_II_Upkeep → Star_Destroyer_II`.
- Hull `Empire_Imperial_SD.ALO`; structure `Empire_Imperial_SD_2.alo` → `Decal_Attach_04`.
- Two `Empire_Imperial_SD_ICQ_01/02.ALO` → `Ta_ISD_ICQ_01/02`; three `Empire_Imperial_SD_Center_Turret_01..03.ALO` → `HP_New_TA_01..03`.
- Eight `Empire_Imperial_SD_2_TLO_01..08.ALO` → `TA_ISD2_TLO_01..08`; mount muzzles at their own variant bones.
- `Empire_Imperial_SD_DeathClone.ALO` inherits only the structure attachment; no living turret attachments in the wreck.
- Source `29` hardpoints − facing dummy − structure dummy → `24` real weapons + shield + hangar + tractor = `27` targets.
- Source has no engine hardpoint. Old three engine targets and unused engine-health row removed.

## Decision

- Chosen: preserve current project balance for this existing-model replacement. Why: source mod balance is not the project's live balance. Avoid: applying historical vault values or silently rebasing the whole ship.
- Dedicated weapons `70/71`: eight octuple turbolasers / two quad ion cannons. Preserve primary/secondary damage and projectile/audio families; use source pulse counts, range ÷10 and mean reload.
- Other mappings: `IonCannon 10 ×6`, `LightDualTurbolaser 36 ×2`, `LightTurbolaser 33 ×3`, `MediumTurboLaser 4 ×3`.
- All guns targetable under the import guide. Source broadside cone widths `90° / 80°`; yaw centers follow port/starboard mounting convention.
- Preserve Power to Main Batteries `24` and Tractor Beam `22`; the first remains the project's Full Salvo equivalent.
- Source repeated garrison rows are additive: TIE Interceptor `2` active + `4` reserve, Lambda/Sentinel transport garrison `1` active + `0` reserve.
- Unsupported transport squadron identities are not substituted. Preserve TIE Interceptor/Brute/Punisher: each `1` active + `1` replacement; initial `4 s`, interval `30 s`.

## Important Values

| Value | Source | Project |
| --- | --- | --- |
| Hull / shields | 40,000 / 20,000 | 5,500 / 4,200 |
| Speed | 3 source units | 25 |
| Shield regeneration | 400 source units/s | 7 HP/s |
| Cost / build / population | 25,000 / 80 s / 24 | 6,000 / 45 s / 8 |
| Tech / cap | Source scenario rules | 3 / 3 |
| Octuple damage / shots / reload / range | 240 / 1 / mean 4.5 s / 3,500 | 135 / 1 / 4.5 s / 350 |
| Quad ion damage / shots / reload / range | 2,000 shield-only / 1 / mean 6 s / 3,000 | 225 project Ion matrix / 1 / 6 s / 300 |
| Full Salvo / Power to Main Batteries | 15 s / 60 s recovery | 20 s / 60 s recovery |

- Main battery shot/reload delay ×`0.33`; secondary guns disabled; shield regeneration ×`0`; speed ×`0.25`.
- Preserved health: weapons `750`, shield `1,000`, hangar `2,000`, tractor `1,500`.
- Geometry length `180` project units; bow `+Z`, up `+Y`; FBX import `0.02`, attachment transport cancellation `0.01× / +90° X`.
- Saved bounds `101 × 49.5 × 180`; roots and banking transform have unit scale. Shield, hull limits, collider, fog and selection fit the new hull.

## Edge Cases

- `MeshShield.fx` on engine effects means visible additive glow; the source shield cage remains hidden.
- `Empire_ISD_Bridge2 / alDefault.fx` is real untextured structure geometry; retain with a neutral material.
- Preserve per-mesh material slots. ALAMO collapses duplicate/degenerate shadow triangles; raw binary triangle counts are not an imported-geometry equivalence test.
- Source helpers remain in editable blends; stripped from game prefabs. Neutral hull + own fitted team stripes; wreck material matches. Stripe offset `0.1` avoids surface-depth overlap.
- Removed `38` old visuals/snapshots and their metadata; all retained Unity asset dependencies checked before deletion → zero references. Four empty asset folders removed; replacement wreck materials retained.
- Engine source groups retain `1,428 / 1,840` triangles. Back-face culling hid four small exhaust caps → two-sided engine materials, source blue/white emission maps, HDR intensity `2`.
- URP material validation derives `_EMISSION` from GI flags → set `RealtimeEmissive`; material reimport preserves emission and `_Cull = 0`. Rear/top/bottom screenshots show all seven exhaust surfaces.
- Previously shared ISD I use remains a historical record. Active ISD II art has zero ISD I/obsolete dependencies.
- No automated Unity tests or combat Play Mode run. Runtime firing, launches, abilities and destruction are unverified.

## Files

- Plan: [[Done/Features/ISDII_Import|ISD II replacement]].
- `Tools/Blender/ISDIIReplacement/README.md` — source mappings and repeatable replacement instructions.
- `Tools/Blender/ISDIIReplacement/ImportEvidence.json` — 55 unchanged SHA-256 hashes, 16 conversions, saved checks and removed-asset paths/GUIDs.
- `output/ISDIIReplacement/SourceAudit.json` — complete resolved unit/hardpoints/projectiles/source binary evidence.
- `output/ISDIIReplacement/Blender/` — 16 compressed editable blends; all used image textures packed.
- `output/ISDIIReplacement/Integrated.png`, `Top.png`, `EnginesRear*.png`, `Previews/` — inspected ship, seven engine surfaces, eight palettes, stern, wreck and hologram.
