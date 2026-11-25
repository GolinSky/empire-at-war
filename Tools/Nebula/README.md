# Match nebula

- `SkirmishVfx.prefab` contains a single raymarched volume and two stationary star emitters.
- `MatchNebula.Awake` chooses one of six compositions once per match (Azure Rift, Violet Pillars, Ember Wings, Jade Halo, Rose Hourglass, Frost Streams); the previous selection is excluded within the session.
- Selection uses its own random stream. Re-enabling the object does not change the composition.
- Six distinct baked 3D fields (`_Variant` 0–5 in the bake compute shader) contain cool emission, warm emission, absorbing dust, and diffuse extinction.
- Fields: 512 × 320 × 48, RGBA32, 30 MiB in memory / ~60 MB on disk each; CPU readability disabled. Baking runs in the Editor, never during a match.
- Rendering: 128 samples, premultiplied emission/transmittance, opaque depth clipping; queue 2560 follows stars at 2550 and precedes planet clouds/atmosphere.
- Stars: 6,500 fine points plus 60 larger points across the entire background volume; no continuous emission, movement, or lifetime fade.
- Internal drift moves fine turbulence slowly; the composition stays fixed in world space for camera parallax.

## Build and preview

Run from the repository root with Unity open:

```powershell
unity command script run --file Tools/Nebula/BuildNebula.cs --entry BuildNebula.Run --timeout 150 --json
unity command script run --file Tools/Nebula/PreviewNebula.cs --entry PreviewNebula.Run --args '["final"]' --json
```

- Builder preserves prefab root identities and existing material/texture GUIDs; it saves all generated assets.
- Shape authoring: `Assets/Art/Shaders/Vfx/NebulaFilamentBake.compute`.
- Palette/emission/extinction: `Assets/Art/Materials/Vfx/Nebula/{AzureRift,VioletPillars,EmberWings,JadeHalo,RoseHourglass,FrostStreams}.mat`.
- Builder restores its authored material values; update those values in `BuildNebula.cs` when changing the default art direction.
- Previews use the gameplay camera and Coruscant post-processing in an isolated preview scene. Output: `output/NebulaRebuild/`.

## Variants added — 2026-11-25

- Jade Halo (3): broken teal ring, gold rim. Rose Hourglass (4): tilted bipolar lobes. Frost Streams (5): three wavy horizontal bands.
- Added by baking only variants 3–5 with the builder's settings; existing fields and the volume object were not rebuilt.
- Previews: `output/NebulaRebuild/candidate-*.png`. Not yet seen in a live match; tests not run (local override).

## Verification — 2026-10-07

- `MatchNebulaTests`: three EditMode tests passed, including 60 selections, no consecutive repeats, distinct fields, valid shaders, persistent stars, and unchanged gameplay RNG.
- Two live match loads selected Violet Pillars then Ember Wings; exactly one `MatchNebula` was active.
- Final textures were reimported from disk and rendered again; all three retain their data with CPU readability disabled.
- Full volume bounds top: −45,782 units. Largest tested Coruscant scale (10.5) extends to −32,684 units → more than 13,098 units of clearance.
- Live match inspection showed planet occlusion and camera parallax. Captured GPU frame samples were 1.57–1.92 ms on the current machine; these are snapshots, not a benchmark or a before/after comparison.
- Unrelated existing UI disposal exceptions (`BaseUi.SetParent`, `CoreGameUiController`, `EconomyUiController`) occurred when leaving matches; no nebula shader/import errors were observed.
