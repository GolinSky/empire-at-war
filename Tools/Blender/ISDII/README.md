# ISD II

- Empire ship `205`; displayed name `ISD II`.
- Source: AOTR workshop `1397421866`, `E_Imperial_Star_Destroyer_2_Fighters` inheriting `T_Imperial_Star_Destroyer_2`.
- Model map: `Temp/ALO_MODEL_MAP.txt`; import rules: vault `Architecture/ALO_MODEL_IMPORT_GUIDE.md`.
- `ImportEvidence.json` records source SHA-256 values, all 11 FBX roundtrip reports and final test results.

## Geometry

- Reuse the verified common `Empire_Imperial_SD.ALO` hull and three triple turrets from the existing ISD I art. The hull hash matches the previous import.
- Import `Empire_Imperial_SD_2.alo`, eight `Empire_Imperial_SD_2_TLO_*.ALO` turrets and two `Empire_Imperial_SD_ICQ_*.ALO` turrets.
- Keep source geometry, UVs, bones, parents and texture channels. Disable collision/shadow helper renderers. Use shared source textures for identical turret materials.
- FBX scale `0.02`; cancel the transport basis on attachments. Prefab root scale is one; hull length is 180 project units.
- Maximum FBX roundtrip geometry error `0.000103` source units; bone matrix error `0.00000301`.
- Own gameplay view, fitted shield, reinforcement hologram, wreck, icon and silhouette. All 100 gameplay mesh renderers are explicitly bound for team colors, including the fitted foredeck stripes shared with ISD I. Neutral hull/turrets, stripe mask strength 1; matching wreck material and renderer/filter binding.

## Loadout

- Hull `21,000`; shields `18,000`; speed `25` exactly as requested.
- Targetable: eight light rapid dual turbolasers, two medium turbo-ion cannons, two shield generators, three engines, tractor beam and hangar; 17 targets total.
- Additional weapons: three medium triple turbolasers, six light turbolasers and four heavy laser cannons; 23 weapons total.
- Main batteries `41`: four shots, `0.317 s` interval, `17.1705 s` reload, range `450` (source range divided by ten), damage `135` per shot.
- Turbo-ion `42`: two shots, `9.5 s` interval, `19.25 s` reload, range `525`, damage `225` per shot. Reloads use the mean of the source min/max.
- Other weapons use existing project medium three-shot, light turbolaser and heavy laser profiles. Projectile effects/speeds follow the project's existing weapon families.
- Broadside arc centers follow the project's port/starboard convention; widths `100° / 80° / 110° / 160° / 200°`, clipped to the supported `[-180°, 180°]` range. Target selection measures yaw relative to the parent transform.
- Main-battery ability `24`: shot/reload delay × `0.33`, disables the other 15 weapons, shield regeneration × `0`, movement × `0.25`; duration `20 s`, recovery `60 s`. Ending restores other active modifiers correctly.
- Existing Tractor Beam `22` retains its corvette/frigate targeting and range restrictions.
- TIE Interceptor, Brute and Punisher bays each allow one active squadron and two total launches (initial plus one replacement); launch interval `30 s`.
- Provisional donor values: cost `25,000`, build `500 s`, level `3`, population `8`, limit `3`, shield regeneration `20/s`, navigation radius `105`. Hardpoint health: weapons `750`, engines/shields `1,000`, hangar `2,000`, tractor beam `1,500`.

## Rebuild

1. Run `Prepare.py` with Pillow; it audits source models/XML and writes `Temp/ISDIIImport/BinaryAudit.json`.
2. Run `Convert.py` through the configured Blender MCP SDK client on isolated port `9884`, with safe mode enabled; it reuses `Tools/Blender/export_isd_i.py`. Preserve the connected user's scene.
3. Run `Stage.py` with Pillow.
4. Evaluate `BuildArt.cs`, `FixAdditive.cs`, `BuildShip.cs`, `BuildPreview.cs`, `Render.cs` and `Register.cs`, in that order, through official `unity command eval`. Each script exposes `Main()` on its named static class.
5. After both ISD I and ISD II gameplay/wreck prefabs exist, run `unity command run_script --file Tools/Blender/ISDI/BuildImperialTeamStripes.cs --entry BuildImperialTeamStripes.Main --json` to restore their localized team stripes.
6. Inspect saved assets and renders, then follow repository scene safety before asynchronous Unity tests.

## Verification

- Final full EditMode suite: `1,066 / 1,067` passed. All 11 ISD II/main-battery checks passed; no current console errors.
- Related fixes: all team renderers bound; weapon arcs correctly cover both broadsides; interrupted secondary salvos cannot restart during Power to Main Batteries.
- Remaining full-suite failure is the existing `VictoryIIShipView.prefab` renderer binding (`65` expected, `42` listed), outside this import.
- Inspected top/stern hull views, placement hologram, wreck and eight team palettes. No manual skirmish or Play Mode run performed.
- Team-color follow-up 2026-10-06: red/blue top and angled ship/wreck renders inspected; all four saved prefab checks passed (ownership, fog, banking, explosion and wreck pairs). No import/serialization errors; no combat or automated Unity test run for this fix. Evidence: `Temp/ImperialTeamColorFix/Verification.json`, `After/`, `WreckAfter/`.
