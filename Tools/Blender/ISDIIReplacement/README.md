# ISD II model replacement

- Empire ship `ISDII = 205`; replaced in place on 2026-10-09. Existing gameplay, data, preview, icon and Addressable GUIDs remain valid.
- Source: `D:/SteamLibrary/steamapps/workshop/content/32470/1770851727/Data`, unit `Star_Destroyer_II`.
- Policy: vault `Architecture/ALO_MODEL_IMPORT_GUIDE.md`, `PROJECT_ORGANIZATION` and UI guides. Historical `../ISDII/` imports from Workshop `1397421866` are obsolete.

## Source assembly

Inheritance: `SFX_Base → Vessel_Base → Capital_Base → Star_Destroyer_Template → Star_Destroyer_II_Template → Star_Destroyer_II_Upkeep → Star_Destroyer_II`.

| Part | Original model | Mounts |
| --- | --- | --- |
| Hull | `Empire_Imperial_SD.ALO` | Source hull hierarchy |
| ISD II structure | `Empire_Imperial_SD_2.alo` | `Decal_Attach_04` |
| Quad ion turrets ×2 | `Empire_Imperial_SD_ICQ_01/02.ALO` | `Ta_ISD_ICQ_01/02` |
| Center turrets ×3 | `Empire_Imperial_SD_Center_Turret_01..03.ALO` | `HP_New_TA_01..03` |
| Octuple turrets ×8 | `Empire_Imperial_SD_2_TLO_01..08.ALO` | `TA_ISD2_TLO_01..08` |
| Death clone | `Empire_Imperial_SD_DeathClone.ALO` | Only inherited ISD II structure; no living turrets |

- Resolve all 29 source hardpoints. Omit the facing-only dummy projectile and structure-only dummy art from gameplay → **24 weapons + shield + hangar + tractor = 27 targetable hardpoints**.
- Weapon counts: eight octuple turbolasers, two quad ion turrets, six hull ion cannons, two dual hull turbolasers, three single hull turbolasers, three center turrets.
- All 13 turret attachments and their muzzle bones come from this source definition. No ISD I prefab/model/material/texture dependencies exist in active ISD II prefabs.
- The source has no engine hardpoint. Remove the obsolete integration's three engine targets; preserve the visible engine effects.

## Conversion and materials

- Blender `3.6.23`, ALAMO importer and isolated MCP port `9890`; the user's shared port/scene remains untouched.
- Preserve geometry, UVs, original material-slot order, bones, hierarchy and attachment positions. Source duplicate shadow triangles may be collapsed by ALAMO; compare imported geometry rather than raw binary triangle totals.
- Sixteen converted FBXs in `Assets/Art/Models/EmpireShips/ISDIIReplacement`; sixteen compressed, texture-packed editable blends in `output/ISDIIReplacement/Blender`.
- Texture PNGs preserve decoded source pixels. Opaque parts use `EmpireAtWar/Ship Lit`; additive lights/engine `MeshShield.fx` effects use URP additive materials. Normal textures are linear, normal-map imported, green channel flipped.
- Engine `MeshShield.fx` materials render both faces (`_Cull = 0`): back-face culling hid four small exhaust caps. Source mesh groups retain all `1,428 / 1,840` triangles. Use the source blue/white textures for emission at intensity `2`; `RealtimeEmissive` flags keep URP validation from disabling `_EMISSION` on save/import.
- Collision, shield cage, shadow-volume and source-hidden renderers are retained in editable source files and removed from game prefabs. `Empire_ISD_Bridge2` / `alDefault.fx` is real untextured structure geometry and remains visible with a neutral material.
- FBX import scale `0.02`; cancel attachment transport scale `100×` and X rotation `−90°`. Fit living geometry to length **180 project units**, bow `+Z`, up `+Y`; banking/root scales are one.
- Four mirrored foredeck stripes use a separate fitted overlay, a linear white mask and shared per-owner palette. Offset `0.1` avoids depth overlap; source geometry/UVs/textures remain unchanged. Matching wreck stripes retain wreck animation/material behavior.
- Death geometry uses the living model's scale and center. The existing wreck generator provides the project's split/glow/fade behavior around the source death-clone mesh.

## Mapping decisions

This replaces an existing model and preserves its current project balance and fighter complement. Source values are recorded for comparison, not silently applied as a rebalance.

| Value | Workshop source | Preserved project value |
| --- | --- | --- |
| Hull / shields | `40,000 / 20,000` | `5,500 / 4,200` |
| Speed | `3` source units | `25` project units |
| Shield regeneration | `400` source units/s | `7 HP/s` |
| Cost / build time / population | `25,000 / 80 s / 24` | `6,000 / 45 s / 8` |
| Technology / cap | Source scenario rules | Existing `3 / 3` |
| Octuple damage / shots / reload / range | `240 / 1 / mean 4.5 s / 3,500` | `135 / 1 / 4.5 s / 350` |
| Quad ion damage / shots / reload / range | `2,000`, shield-only / `1 / mean 6 s / 3,000` | `225`, project Ion matrix / `1 / 6 s / 300` |
| Full Salvo duration / recovery | `15 s / 60 s` | Existing Power to Main Batteries `20 s / 60 s` |

- Dedicated weapon IDs `70` (`ISDIIOctupleTurbolaser`) and `71` (`ISDIIQuadIonCannon`) preserve current primary/secondary damage and projectile/audio families. Old IDs `41/42` remain available for historical assets.
- Other hull/center guns map to existing project profiles: Ion Cannon `10 ×6`, Light Dual Turbolaser `36 ×2`, Light Turbolaser `33 ×3`, Medium Turbolaser `4 ×3`. Their projectile effects and balance remain project conventions; source exact values are in `SourceAudit.json`.
- Source ranges for dedicated turrets divide by ten; reload uses the source min/max mean. Broadside yaw centers follow port/starboard position, with source cone widths `90°` / `80°`; all weapons are targetable under the import guide.
- Preserve ability `24` Power to Main Batteries: eight main guns, shot/reload delay ×`0.33`, other guns disabled, shield regeneration ×`0`, speed ×`0.25`; ability `22` Tractor Beam retains existing supported targets/range and `20 s` recovery.
- Source repeated garrison rows are additive: two active + four reserve TIE Interceptor squadrons, plus one active Lambda/Sentinel transport garrison. These shuttles lack supported project squadron identities. Preserve existing TIE Interceptor/Brute/Punisher bays: each one active, one replacement, `4 s` initial / `30 s` launch interval.
- Preserve hardpoint health: weapons `750`, shield `1,000`, hangar `2,000`, tractor `1,500`. Remove unused engine-health configuration.
- Keep existing ship/faction/HUD/placement/ability registrations. Update loadout, description and icon imagery in place; introduce no duplicate ship identity.

## Obsolete visuals

- Removed all thirty-eight prior visual assets/snapshots on 2026-10-09, as requested. Deleted the old ISD II models, materials, textures, wreck materials and temporary snapshots, plus four empty asset folders.
- Checked every retained Unity asset's direct dependencies before deletion: zero references to removed visuals. Active prefab, icon and data GUIDs remain valid. The mixed wreck-material folder retains the replacement materials.
- Previously shared ISD I assets are recorded as obsolete **for ISD II usage** in `ImportEvidence.json`; this task does not globally deprecate another ship's dependencies.
- `ImportEvidence.json` records removed paths/GUIDs and the dependency check. `Prepare.py` restores that record for rebuild verification; no obsolete snapshots are recreated.

## Rebuild

1. Read the vault import/placement/UI guides and inspect the current Editor, source XML and prefab registrations.
2. Run `uv run --with pillow python Tools/Blender/ISDIIReplacement/Prepare.py`.
3. Start a separate configured Blender `3.6.23` with `--python F:/Private/empire-at-war/Tools/Blender/ISDIIReplacement/Start.py`. Use `Start-Process -WindowStyle Hidden`; preserve the user's Blender scene. Inspect addon status, scene and viewport through the MCP client before mutation.
4. Run `python Tools/Blender/ISDIIReplacement/Convert.py`, then `uv run --with pillow python Tools/Blender/ISDIIReplacement/Stage.py`.
5. Preserve the deletion manifest from `ImportEvidence.json`; never recreate historical ISD II snapshots or copy the old shared ISD I configuration.
6. Run the following scripts in order with `unity command run_script --file Tools/Blender/ISDIIReplacement/<Type>.cs --entry <Type>.Main --timeout 30000 --json`: `BuildISDIIReplacementArt`, `BuildISDIIReplacementShip`, `RegisterISDIIReplacement`, `BuildISDIIReplacementStripes`, `BuildISDIIReplacementPreview`, `RenderISDIIReplacement`.
7. Run `python Tools/Blender/ISDIIReplacement/CaptureGeometry.py`, then `VerifyISDIIReplacement` and `RenderISDIIEngines` using the same Unity command syntax. A material-only repair can use entry `BuildISDIIReplacementArt.FixEngines`.
8. Run `python Tools/Blender/ISDIIReplacement/PreserveBlends.py` and `uv run --with pillow python Tools/Blender/ISDIIReplacement/SaveEvidence.py`. Inspect Blender/Unity outputs, screenshots, Console, Editor compilation and saved scenes.
9. Update the related vault plan/backlog and links. Run automated Unity tests only on explicit request, with the repository's dirty-scene and asynchronous test safety rules.

## Verification — 2026-10-09

- All **55 source SHA-256 hashes unchanged**. Decoded texture conversion is lossless; sixteen packed editable blends verified.
- All sixteen Blender FBX roundtrips pass geometry/UV/bone/parent checks. Maximum source-space geometry error `0.000157953`, bone error `0.000098632`.
- Unity import verifies all sixteen models against Blender position/UV corners and bones. Maximum project-space position error `0.000003026`; exact saved gameplay mount positions.
- Four persisted prefab/reference checks pass; visible renderer counts `59 / 59 / 25 / 49` for model/gameplay/wreck/placement. Matching bounds `101 × 49.5 × 180`, no old ISD II or ISD I art dependencies, no missing scripts or broken references.
- Dedicated profiles/audio, existing Empire/HUD/placement/Addressable registrations, live balance, hangar identities and abilities verified. All 38 obsolete assets and metadata removed; no retained references.
- Engine closeups from the rear, above and below verify three large blue and four small white exhaust surfaces. Saved emission/culling and original engine triangle totals verified after material import.
- Inspected Unity top/stern, ship, wreck and hologram renders; eight different team palettes verified. Updated existing ISD II test expectations for the new loadout; **no automated tests or combat Play Mode run**.
- Final Unity compilation/import checks pass; no new ISD II errors after final engine reimport and saved-asset checks. Runtime combat, turret firing, launches, abilities and destruction remain untested.
- Evidence: `ImportEvidence.json`, `output/ISDIIReplacement/SourceAudit.json`, `VerifiedUnity.json`, `Integrated.png`, `Top.png`, `EnginesRear*.png`, `Previews/` and `Blender/`.
