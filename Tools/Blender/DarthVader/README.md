# Darth Vader TIE Advanced

- Source: Remake Workshop `1770851727`, `Data/Art/Models/Empire_TIE_Advanced.ALO`; original files unchanged.
- Production conversion: Blender MCP / Blender 3.6.23, fresh `Darth Vader Production Source` scene. Preview comparison scene is separate.
- Staging: `Temp/VaderImport/Output/DarthVaderTIEAdvanced.fbx`, packed `DarthVader.blend`, nine source-equivalent PNG textures, three green-flipped `_OpenGL.png` normal-map derivatives, `DarthVaderIcon.png` (512 RGBA).
- Evidence: `Evidence/SourceAudit.json`, `Evidence/MaterialManifest.json`, `Evidence/ConversionVerification.json`. Full original geometry audit remains in staging.
- 15 bones, 10 meshes; 7 visible meshes / 2,334 triangles. Shadow volume, shield helper and collision remain in FBX, hidden for presentation; exclude from game prefabs.
- ALAMO global helper cleanup stopped on another scene's object after geometry import. Production helpers retained original triangle geometry; collision welding preserves all triangle positions/UVs. Source normals were restored on cockpit, hunter and shield after round-trip validation identified importer shading changes.
- Source bone matrices/hierarchy restored from binary; constraints converted to bone parents. All 10 meshes pass source→FBX oriented-triangle position/UV checks; visible source normals pass. Shadow/collision normals intentionally excluded from checks; no helper renders in-game.
- Source bow `-Y`, up `+Z`; FBX axes `-Z` forward / `Y` up. Preserve source origin during conversion. Unity integration must center/size visible geometry and verify final bow `+Z`.
- Source visible bounds: `(-8.950274, -7.470791, -6.347546)` → `(8.950274, 12.191975, 6.347547)`.
- Remake hardpoint `HP_TIE_Fighter_X1_G_HL` fires both bones from `HP_01` (same location as `MuzzleA_00`); source `MuzzleB_00/01` also retained. Weapon choice/offsets belong to project integration.
- Materials retain exact original shader parameters. `M02` cockpit lights, `M03` glass (color factor `0.10588236`), and `M06` engines use additive presentation. `M01`, `M04`, `M05` use source DirectX normal maps; use source PNG with Unity green-channel flip, or already-flipped `_OpenGL` derivatives without a second flip.
- 512 icon verified visually; alpha bounds `(24,63)..(496,444)`, no crop. Source ALA animation and proprietary shader equivalence are outside static conversion.
- Verification command (artifact validation, not game test suite): portable Blender `--background --factory-startup --python Tools/Blender/DarthVader/Verify.py`. Requires staged `SourceAudit.json` and `DarthVader.fbx`; writes `Verification.json`.

## Unity integration

- `BuildArt.cs` → `VaderArt.Apply`; `Integrate.cs` → `VaderIntegration.Apply`; `Inspect.cs` → `VaderInspect.Run`, using `unity command script run --file <path> --entry <method> --json`.
- Preserve imported root scale when normalizing: multiply existing scale by target/measured length. FBX root scale is 100; replacing it caused a 100× undersized saved model. Final visible length: 6 Unity units.
- Squadron ID 208; leader weapon 164 ×2; escort weapon 165 ×12 (standard TIE tuning). Paired `MuzzleB_00/01` used intentionally instead of Remake XML's shared `HP_01` barrel position.
- Vader: 3200 hull, 2400 shields, 400 raw laser DPS. Six TIE escorts: 58 hull, no shields, 21 DPS each. Total: 3548 hull, 2400 shields, 526 DPS; cruise/combat speed 58/66. Shared HeroStrikecraft armor class.
- Leader absorbs damage only after escorts are lost. Replenish Wingmen (55) replaces dead escorts at full health, never heals surviving members; 60 s recovery, requires live leader. AI uses offensive combat trigger plus missing-escort readiness.
- Empire hero: level 4, price 5000, build time 35 s, population 2, maximum 1. Uses existing TIE squadron voice/audio set.
- Saved gameplay and hologram prefabs, icon, faction/UI/audio mappings, and entries in existing EmpireUnitViews/Data/SharedIcons groups are registered.
- Inspection artifacts: `Temp/VaderImport/Previews/{Formation,Leader}.png` and `Readback.json`. Saved values verified against other maxima: hull 3000, shields 2220; Anakin 365 raw DPS. Unity compilation/import and saved visual checks completed; tests and battle playtest not run.
