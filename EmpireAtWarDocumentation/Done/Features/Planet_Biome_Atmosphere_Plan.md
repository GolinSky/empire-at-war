---
category: Features
status: done
created: 2026-11-19
updated: 2026-11-19
completed: 2026-11-19
acceptance: pending-user
---
# Planet Biome Atmosphere

## Goal

- Each battle map gets sun, ambient and colour grade from its planet's biome (Tatooine orange, Coruscant warm, Hoth cold).
- Biome comes from the planet surface albedo; one explicit `Biome` value per planet is the source of truth.
- Keep team colours and fog readable; tints stay subtle.

## Current state (verified 2026-11-19)

- `Battle.unity` uses one setup for all 57 planets:
  - Sun: `Prefabs/Light/Directional Light.prefab`, colour `#FFF4D6`, intensity 0.96, temperature off.
  - Ambient: Trilight, sky `#C7CEE0` / equator `#8A94A6` / ground `#525A6B`, intensity 0.75.
  - Global Volume priority 0 → `Scenes/Planets/Corusant/Global Volume Profile.asset`: ColorAdjustments (exposure 0.52, contrast 5, saturation −3, filter white), Bloom, Vignette 0, MotionBlur.
  - `SceneContext.prefab`: priority-100 sparse `PerformantVolumeProfile` (Bloom, MotionBlur only) at quality 0.
- No runtime code writes `RenderSettings` or the sun in battle.
- `PlanetMapProfileEntry.Preview` is the map-layout thumbnail (all ≈ `#101925`); unusable for biome.
- `UI/PlanetPreviews/*_Preview.png` share images and include blue nebula; unreliable.
- Planet prefab surface albedo (`Art/Textures/Models/Planets/Original/w_planet_*_Albedo.png`) separates biomes cleanly → used for classification below.

## Decision

- **Chosen:** add `PlanetBiome` enum + `Biome` field on `PlanetMapProfileEntry`; one `PlanetAtmosphereSettings` ScriptableObject maps biome → sun, ambient and a sparse grade `VolumeProfile`.
- Why: explicit data is testable, allows lore overrides (Dantooine), and needs no runtime texture reads.
- Avoid: sampling albedo at runtime (texture must be readable, slow, fragile); duplicating the full base Volume profile per biome (bloom/exposure tuning would need 10 copies).
- Grade = second global Volume, priority 50, overriding only `WhiteBalance` (temperature, tint) and `ColorAdjustments.colorFilter` + `saturation`. Base exposure/contrast/bloom stay in the base profile.
- Cost: URP bakes WhiteBalance/ColorAdjustments into the existing colour-grading LUT → no extra full-screen pass. Performant volume (priority 100) does not override these, so the grade applies at every quality level.

## Proposed palette

Baseline = Temperate (current values). Saturation is added to the base −3.

| Biome | Sun | Int | Ambient sky / equator / ground | WB temp / tint | Colour filter | Sat |
|---|---|---|---|---|---|---|
| Desert | `#FFCF9A` | 1.037 | `#D0AA8B` / `#8E725F` / `#4B3C34` | +17 / +3.4 | `#FFEBD6` | +3.4 |
| Urban | `#FFDCAF` | 0.994 | `#C9AE99` / `#87756A` / `#483F3B` | +10.2 / +2.6 | `#FFF1E2` | +1.7 |
| Ice | `#DEEAF9` | 0.994 | `#AEC4E5` / `#7288A8` / `#3E4C64` | −18.7 / −1.7 | `#E6F1FF` | −6.8 |
| Ocean | `#E8F2F9` | 0.977 | `#9CC4E0` / `#658CA6` / `#344F64` | −10.2 / −5.1 | `#E2F4FF` | 0 |
| Temperate | `#FFF4D6` | 0.960 | `#C7CEE0` / `#8A94A6` / `#525A6B` | 0 / 0 | `#FFFFFF` | 0 |
| Forest | `#F6FDE0` | 0.952 | `#ABCAAA` / `#709076` / `#3A5241` | +3.4 / −8.5 | `#F1FFEB` | +2.6 |
| Swamp | `#E1E6B9` | 0.892 | `#9DA98D` / `#6A745F` / `#383E34` | +5.1 / −11.9 | `#EBF0D9` | −6.8 |
| Volcanic | `#FFB386` | 0.994 | `#BB8470` / `#7C4C3E` / `#4B2721` | +23.8 / +6.8 | `#FFDFCF` | +5.1 |
| GasGiant | `#FFD5AF` | 0.994 | `#D6AAA4` / `#956F73` / `#4F3B46` | +12.8 / +10.2 | `#FFE7E0` | +4.3 |
| Barren | `#F1F1F0` | 0.952 | `#ADB1BB` / `#757A83` / `#40444C` | −3.4 / 0 | `#F4F4F4` | −11.2 |

- 2026-11-19 user feedback "too much contrast, hurts eyes": every biome deviation scaled ×0.85 toward Temperate (sun, intensity, ambient) or neutral (WB, filter → white, saturation delta). Original proposal ×1/0.85 recoverable from these.
- Base profile contrast (+5) and exposure (0.52) unchanged; they belong to `Scenes/Planets/Corusant/Global Volume Profile.asset`.

## Planet assignment (from albedo average)

| Biome | Planets | Albedo evidence |
|---|---|---|
| Desert (9) | Tatooine, Honoghr, Hypori, Korriban, Saleucami, Bonadan, Ryloth, Geonosis, Utapau | tatooine `#B78975`, desert `#B79A77`, geonosis `#BD561A` |
| Urban (5) | Coruscant, Fondor, Taris, Eriadu, Kuat | Coruscant `#2D2724` + emissive city, urban `#675C50` |
| Ice (4) | Hoth, Ilum, AlzocIII, Polus | hoth/ice `#A7BACC` |
| Ocean (6) | Kamino, Manaan, MonCalamari, Bestine, Fresia, Dantooine | ocean `#016699`, kamino `#154C9B` |
| Temperate (14) | Alderaan, Naboo, Muunilinst, Balmorra, Ralltiir, AbregadoRae, Anaxes, Bothawui, Byss, Carida, Corellia, Hapes, Jabiim, Lwhekk | temperate `#436C7E`, alderaan `#50888F` |
| Forest (8) | Kashyyyk, Corulag, Dathomir, Myrkr, Thyferra, Wayland, Yavin, Endor | forest `#3F6A4E`, yavin `#1A784E` |
| Swamp (5) | Dagobah, Felucia, NalHutta, Atzerri, Mandalore | swamp `#304923` |
| Volcanic (4) | Mustafar, Sullust, Shola, AetenII | mustafar `#481F11`, volcanic `#955F3D` |
| GasGiant (1) | Bespin | gas `#DE8943` |
| Barren (1) | Kessel | asteroid `#51463F` |

Lore/albedo mismatches — decide per planet:
- Dantooine: ocean albedo, lore grassland → Ocean (albedo) or Temperate.
- Dathomir: forest albedo, lore red jungle → Forest or Volcanic-lite.
- Mandalore: swamp albedo, lore desert/urban → Swamp or Desert.
- Utapau: olive albedo (`#99A250`), lore arid sinkholes → Desert.
- Kuat: prefab has only ring texture; shipyard world → Urban.
- Byss: temperate albedo, lore dark-side world → Temperate or Barren.

## Implementation

1. `Entities/Planet/PlanetBiome.cs` enum (10 values above).
2. `PlanetMapProfileEntry`: `[field: SerializeField] public PlanetBiome Biome`. Check `Editor/MapArchetypeAssetBuilder.cs` preserves it when rebuilding `PlanetMapProfiles.asset`.
3. `Entities/Map/PlanetAtmosphereSettings.cs` (SO) + `PlanetAtmosphereEntry` (serializable): biome, sun colour, sun intensity, ambient sky/equator/ground, ambient intensity, `VolumeProfile grade`. `Get(biome)` throws on missing/duplicate (same pattern as `PlanetMapProfile.Get`).
4. Reference it from `MapGenerationSettings` (already in battle scope via `PlanetFactory`).
5. `Components/Environment/BattleAtmosphereView` (MonoBehaviour in `Battle.unity`): serialized sun `Light` + grade `Volume` (global, priority 50, initially empty profile). Method `Apply(PlanetAtmosphereEntry)` sets light colour/intensity, `RenderSettings.ambientSkyColor/EquatorColor/GroundColor/ambientIntensity`, `volume.sharedProfile`.
6. Battle-scope service resolves `IGameModelObserver.PlanetType` → profile entry → atmosphere entry → `Apply` once at battle start (before loading fade-out).
7. Assets: `Assets/Settings/Render Pipelines/Atmosphere/<Biome>Grade.asset` (10 sparse profiles) and `Assets/Settings/Data/Models/Map/PlanetAtmosphereSettings.asset`; assign `Biome` for all 57 entries by editor script, then reserialize + `SaveAssets`.
8. Check ship shaders (`ShipLit`, wreck, decor) read SH ambient; if a custom shader ignores ambient, the sun + grade still carry the mood. Record findings.

## Edge Cases

- Battle scene active → `RenderSettings` belongs to it; unload restores menu settings. Verify no tint leaks into MainMenu.
- Save/load and online rejoin: biome derives from `PlanetType` → no save/network data needed.
- Cinematic camera and ship-icon generator (`ShipIconGenerator` sets its own ambient) unaffected.
- Nebula variant stays random per battle; biome-matched nebula/skybox tint is a possible follow-up, not in scope.
- Strong tints can hurt team-colour reads (red Empire vs Volcanic/Desert) → in-game check on Tatooine, Mustafar, Hoth.

## Tests (category `Map`; not run under local override)

- `PlanetAtmosphereSettings` has exactly one entry per `PlanetBiome`, each with a grade profile containing `WhiteBalance` + `ColorAdjustments`.
- Every `PlanetMapProfileEntry` resolves an atmosphere entry.
- Applier test: given a planet, sun/ambient/volume profile equal the entry's values (wiring, not pinned colours — see no-stat-pinning rule).

## TODO

- [x] User approved palette 2026-11-19; mismatches use the defaults above (Dantooine Ocean, Dathomir Forest, Mandalore Swamp, Utapau Desert, Kuat Urban, Byss Temperate).
- [x] Code steps 1–6.
- [x] Assets step 7; reserialized; Console has no import/serialization errors.
- [x] Shader ambient check (step 8).
- [x] Tests authored (`PlanetAtmosphereTests`, category `Map`); not run.
- [ ] User in-game look at Tatooine, Coruscant, Hoth, Mustafar, Kamino, Dagobah → [[TODOs/User_Acceptance_Queue#Planet Atmosphere]].

## Outcome (2026-11-19)

- Changes from plan: ambient intensity dropped from the data — in Trilight mode `RenderSettings.ambientIntensity` has no effect; scene keeps 0.75.
- Grade saturation is absolute (Volume override replaces, not adds): stored value = base −3 + delta.
- Files: `Entities/Planet/PlanetBiome.cs`; `Entities/Map/PlanetAtmosphere.cs`, `PlanetAtmosphereSettings.cs`, `PlanetAtmosphereApplier.cs` (observes `BattleMap`, like `PlanetSpawner`); `Components/Environment/BattleAtmosphereView.cs`; `MapRegistrations`, `SkirmishScope.atmosphereView`.
- Assets: `Settings/Render Pipelines/Atmosphere/<Biome>Grade.asset` ×10; `Settings/Data/Models/Map/PlanetAtmosphereSettings.asset`; `MapGenerationSettings.Atmospheres`; 57 `Biome` values in `PlanetMapProfiles.asset`.
- `SceneContext.prefab` owns `Planet Atmosphere` (global Volume priority 50, default `TemperateGrade`, `BattleAtmosphereView`) and the nested `Directional Light` (moved from `Battle.unity` with its overrides: intensity 0.96, shadow strength 0.88, normal bias 0.2). `SkirmishScope.atmosphereView` is a prefab value; `Battle.unity` `m_Sun` resolves to the nested light.
- Fix 2026-11-19: first version kept the view in `Battle.unity` as a scene override → prefab value null → `RegisterComponent(null)` NRE in all 35 `RealEntityCompositionTests` (`TestResults_20261119_160523.xml`, 4197 run, 35 failed, all this cause). Rule: battle-scope DI references must live in `SceneContext.prefab`; tests instantiate the prefab without the scene. Guard added: `SerializedReferenceTests` requires `SkirmishScope.atmosphereView`, `BattleAtmosphereView.sun`/`gradeVolume`.
- Shaders: `ShipLitForwardPass`, `ShipWreckForwardPass`, `DecorAsteroid` sample SH ambient; `PlanetLit` does not → planets react only to sun colour + grade.
- Tests 2026-11-19: `PlanetAtmosphereTests` passed (user run); EditMode `Core` 166/166 passed after the SceneContext fix (incl. 36 `RealEntityCompositionTests`, `SerializedReferenceTests`); explicit excluded.
- Verification: Unity compile 0 errors; assets/scene persisted and reserialized; tests not run (local override); not run in Play Mode. Runtime SH update after Trilight colour change unverified in game.
- Reusable guidance → [[Rules/BATTLE_PERFORMANCE_GUIDELINES#Quality and post-processing]] (grade volume priority/cost).
