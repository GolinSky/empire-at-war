# Luke's Red Squadron AOTR art

- Source: AOTR Workshop `1397421866`, `Data/ART/MODELS/RV_X_Wing.ALO`; source SHA-256 `0996a85694be39ada8e22827d0c3e00c71cb509db0c3f94f854bd5be4741c93e`. Original files unchanged.
- Existing project XWing uses the different vanilla `RV_XWING.ALO`; it is not reused for Luke or his five wingmen.
- Run `python Tools/Blender/LukeRedSquadron/Prepare.py`, then portable Blender 3.6.23 `--background --factory-startup --python Tools/Blender/LukeRedSquadron/Convert.py`. No shared Blender scene is modified.
- Run `unity command run_script --file Tools/Blender/LukeRedSquadron/BuildArt.cs --entry LukeRedSquadronArt.Apply --json` to copy staged art and build the geometry-only prefab. Parent integration builds gameplay/placement prefabs and registrations.
- 26 bones, 12 meshes, 8 visible meshes / 9,099 triangles. Preserve all original helpers in FBX; exclude shadow meshes and collision from gameplay geometry. Source `hidden` flags do not hide the three shadow-volume meshes, so shader identity determines helper exclusion.
- Retained visible meshes: `Wing1`, `Wing2`, `Plane` (fuselage), `Astrodroid`, `Plane01`–`Plane04` (additive engine glow). Shared material signatures map to four external Unity materials.
- Laser attachments: `MuzzleA_00`–`MuzzleA_03`; torpedo attachment: `MuzzleB_00`. Four `PE_X_WING` bone names become unique Blender names using `.001`–`.003`. Engine-plane centers are also valid explicit trail anchors.
- Static combat pose has open S-foils. Source bow `-Y`, up `+Z`; FBX exports `-Z` forward / `Y` up. Geometry prefab multiplies imported root scale to normalize visible Z length to 6 Unity units and centers the model.
- Seven lossless RGBA PNG texture conversions retain original pixels. Source DirectX normal maps use Unity's green-channel flip. Hull/wing materials use red-hue team livery; astrodroid and engine glow retain original colors.
- Icon: actual model rendered at 512×512 RGBA, alpha bounds `(62,73)..(476,405)`; no clipping. FBX round-trip materials can infer unwanted alpha from source diffuse alpha, so icon rendering explicitly rebuilds materials from the audited source definitions. Unity hull shader is opaque.
- Verification: source→Blender→FBX oriented geometry and UV comparisons use existing strict importer limits; bone count, parents, source matrix metadata and head positions pass. Blender custom-normal storage introduces up to 0.00425 vector error on visible source normals (roughly 0.25°); model-specific limit is 0.005. Degenerate triangles and invisible helper normals are excluded. Source topology is not simplified.
- `Evidence/` retains source structure, material mapping, conversion comparisons, texture/output hashes and icon bounds. Full staging/packed blend stays in `Temp/LukeRedSquadron/Output`.
- ALA animations and proprietary engine-particle proxy behavior are not imported; approved gameplay uses the static combat pose. Game tests and runtime battle validation were not run by the art conversion.

## Gameplay integration

- `Integrate.cs` → `LukeIntegration.Apply`; `ConfigureAbility.cs` → `LukeAbilityConfiguration.Apply`; `Inspect.cs` → `LukeInspect.Run`. Invoke with `unity command run_script --file <path> --entry <method> --json` after `BuildArt.cs`.
- Squadron ID `303`; six members: Luke, Wedge, Biggs, Red Leader, Hobbie, Porkins. Luke uses `3600` hull / `2600` shields; each escort uses `120` hull / `60` shields. Total `4200` hull / `2900` shields. Escorts protect the leader until destroyed.
- HeroStrikecraft armor; cruise/combat speed `68/78`; acceleration `78`, turn rate `210`, formation spacing `5`. Shield regeneration `1` per living shielded member per second.
- Four leader lasers (`169`) provide `450` raw DPS using the HeroLaser damage/accuracy profile. Twenty escort lasers (`170`) provide `200` raw DPS. Both use Rebel bolt colors; ordinary X-wing weapons are unchanged.
- Rebel hero roster: level `4`, cost `6000`, build `40 s`, capacity `3`, limit `1`. Gameplay and six-model hologram preview belong to `RebellionUnitViews`; squadron data to `Data`; rendered icon to `SharedIcons`. Existing X-wing audio profile is reused.
- Lucky Shot (`56`): `1.25 s` attack-run preparation, then one visible proton torpedo at speed `220`; range `350`, recovery `60 s`. A living Luke is required. Targets only enemy Capital, HeavyCapital and Structure classes, including space stations.
- PrecisionStrike (`16`) delivers `4000` base hull damage through shields and hardpoints; defensive damage-taken modifiers still apply. No area damage or squadron targets. Move/Stop, suppression and target loss cancel preparation; a launched shot survives caster death. Only the authority applies damage.
- Saving an active shot converts remaining preparation/travel plus full cooldown to recovery. Loading never replays damage. Late replica activation cannot launch from an escort after Luke dies.

## Verification

- Unity imported and saved art, gameplay/placement prefabs, data, icons and Addressables registrations. Runtime and Editor script compilation completed successfully on `2026-11-16`; no test execution.
- Saved readback: `Evidence/UnityReadback.json` records six flight/health members, 24 guns, 30 injectables, zero missing materials, actual derived `650` raw DPS and roster/ability/group values. Other current squadron maxima: `526` raw DPS, `3548` hull, `2400` shields.
- Geometry-only Unity renders of the model, six-member formation and matching hologram were visually inspected. Local images remain in `Temp/LukeRedSquadron/Previews`; transparent source-model icon is committed.
- Added Luke saved-asset/binding/roster regressions and Lucky Shot targeting, timing, authority, cancellation, save/load and hull-damage regressions. Updated Vader's former absolute durability supremacy to rank below Luke while retaining the remaining comparisons.
- Reviewed affected Squadrons, Abilities, BattleInput, Health, GameFlow, Combat, Balance, Economy, Audio and UI-related asset fixtures; generic inventory, weapon-bake, icon, hologram and audio discovery already cover the added unit. Tests were not run under the local execution restriction.
- Battle balance, live projectile presentation and online gameplay remain user acceptance checks. Static stats and source checks do not prove every duel outcome.
