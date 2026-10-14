# Empire fleet worn livery

- Scope: Procursator, Allegiance, Titan and Tartan. Hulls and turrets retain neutral source albedos; small chipped stripes show ownership.
- Whole-mesh wash came from unmasked `_TeamRimStrength`; live and wreck materials now use rim/emission tint `0`. Base hull mask/livery strengths are `0`.
- Paint follows Acclamator's albedo hue selection: hue `0.968`, range `0.07`, minimum saturation `0.4`, livery strength `1`, mask strength `0`.
- Three mirrored bands use width `0.009 × ship length`. Triangle clipping preserves source UVs/normals; original geometry and textures remain unchanged.
- Derived paint albedos retain source surface detail, with deterministic multi-scale fading and chipped edges. Approximately 22–25% exposed metal; nine used textures.
- Overlay surfaces/triangles: Procursator `2/256`, Allegiance `2/168`, Titan `3/254`, Tartan `2/100`.
- Gameplay team/fog bindings, hologram overlays, wrecks and icons are saved through Unity. No gameplay tuning or squadron changes.

## Rebuild

1. Finish the ship's original art/gameplay build. This workflow supersedes Titan's old `BuildStripes.cs` and all four ships' old palette rendering.
2. Run `ExportPaintInputs.cs` with Unity; then `uv run --with pillow --with numpy python Tools/Blender/EmpireFleetLivery/PreparePaint.py`.
3. Run `Build.cs` once for each ship, then `TrimUnusedPaint.cs`.
4. Run `Render.cs` once for each ship; regenerates icons plus unowned/eight live/eight wreck renders.
5. Run `Verify.cs`; then `uv run --with pillow --with numpy python Tools/Blender/EmpireFleetLivery/VerifyRenders.py`.

Unity command: `unity command run_script --file Tools/Blender/EmpireFleetLivery/Build.cs --args '["Procursator"]' --timeout_ms 60000 --timeout 70 --json`. Replace file/argument as required; scripts without ship arguments omit `--args`. Inspect nested `result.success`.

## Verification — 2026-10-14

- Existing ship EditMode tests: Procursator `6/6`, Allegiance `12/12`, Titan `12/12`, Tartan `9/9`; total `39/39`.
- Saved prefab/material/team/fog/reference checks: `91,968` passed.
- All eight palettes visibly distinct in live/wreck renders. Blue/green changes affect about `0.3–0.5%` of visible live pixels; original albedo hashes unchanged.
- Evidence in `Evidence/`; canonical renders in `Previews/<ship>/`. These supersede each import's earlier rim-colored preview evidence.

## Seven-ship mask refresh — 2026-10-14

- Scope: ISDI, ISDII, VictoryI, VictoryII, original Victory (stored under Empire; user confirmed), Republic Imperator, Empire AcclamatorAssault.
- Built-in image generation added fine chips, scratches and fading to existing stripe masks. Existing stripe meshes, source albedos and gameplay prefabs remain unchanged. This refresh uses `_TeamMaskMap`, rather than Allegiance's albedo hue selection.
- Victory I/II original masks had identical SHA-256 hashes; both receive the same worn layout. ISDI keeps its existing mask path. ISDII uses `Assets/Art/Textures/Models/EmpireShips/ISDIIReplacement/ISDII_WornTeamStripeMask.png`; its live/wreck stripe materials use the existing high-contrast hull albedo/normal instead of the previous white albedo.
- Other outputs replace `VictoryI_EV_Victory_SD_TeamMask.png`, `VictoryII_EV_Victory_SD_TeamMask.png`, `Victory_Hull_TeamMask.png`, `Imperator_Hull_TeamMask.png`, and `AcclamatorAssault_HullStripeMask.png` in their existing ship texture folders. Existing GUIDs are preserved.
- Import/save: `unity command run_script --file Tools/Blender/EmpireFleetLivery/ImportWornMasks.cs --json`.
- Preview: `unity command run_script --file Tools/Blender/EmpireFleetLivery/RenderWornMasks.cs --args '["ISDI"]' --json`; substitute each ship name above. Preview scenes are closed and render settings restored. No icons or gameplay scenes are changed.
- Verification: 28 neutral/blue/red/green renders in `Previews/WornMasks/<ship>/`; all seven blue/green pairs differ. Results in `Evidence/WornMaskRenderVerification.json`; live/wreck references in `Evidence/WornMaskReferences.json`. No new import/serialization errors after the corrected property audit. No gameplay tests run for this texture/material update.

### Image prompts

- Shared ISDI/ISDII wear: “Create a production grayscale texture mask for chipped old starship hull paint. 1024x1024 square, seamless tile, flat unlit 2D mask only. White/light gray means remaining paint, black means chipped exposed metal. About 20% irregular tiny black chips, about 15% gray faded wear, remaining 65% near white paint. Very fine multi-scale erosion and scratches, aged shabby military spacecraft finish like worn Republic Acclamator paint. Dense fine detail distributed evenly over entire image with no large empty areas, no objects, no symbols, no text, no border, no lighting or shadows. Opaque grayscale RGB.”
- Layout edits use the original mask as image 1 and shared wear as image 2: preserve aspect ratio and stripe positions, sizes, angles and cutouts; apply approximately 20% fine black chips, scratches and gray fading only within white shapes; slight inward edge erosion; flat opaque grayscale, no text/shadows. Victory I: square; Victory and Imperator: portrait 1:2; AcclamatorAssault: square. Victory II reuses the Victory I result.

## Small markings: Arquitens, Procursator, Executor — 2026-10-14

- Empire Arquitens and Executor: three narrow foredeck bands per side, width `0.005 × length` (Arquitens `0.20`, Executor `9.50` Unity units). Source triangle clipping preserves hull UVs and normals. Arquitens adds two mesh surfaces / 154 triangles; Executor adds one / 140 triangles.
- Procursator already had three narrow bands per side. Its two existing stripe materials now use neutral source albedos plus the worn mask instead of faint hue-selected paint; geometry is unchanged.
- All three reuse the generated wear texture `Assets/Art/Textures/Models/EmpireShips/ISDI/ISDI_TeamStripes_TeamMask.png`. No additional image generation or source texture edits.
- New stripe meshes/materials live in each ship's existing `Assets/Art/Models/EmpireShips/<ship>/` and `Assets/Art/Materials/Models/EmpireShips/<ship>/` directories. Wreck materials live in `Assets/Art/Materials/Wrecks/<ship>/`.
- Build: `unity command run_script --file Tools/Blender/EmpireFleetLivery/BuildSmallTeamMasks.cs --args '["Executor"]' --json`; supported names: `ArquitensImperialCruiser`, `Procursator`, `Executor`.
- New live stripes are bound to team color and fog; wreck stripes are bound to `UnitWreckView`. Existing wreck geometry remains intact.
- `RenderWornMasks.cs` accepts optional second argument `true` for saved wreck previews. Twenty-four live/wreck neutral/blue/red/green images are in `Previews/WornMasks/<ship>/` and its `Wreck/` subfolder.
- Verification: six saved live/wreck reference checks passed; all six live/wreck blue-green comparisons differ; Executor tests `8/8`, Procursator tests `6/6`. Evidence: `SmallMaskReferenceVerification.json`, `SmallMaskRenderVerification.json`, `ExecutorSmallMaskTests.json`, `ProcursatorSmallMaskTests.json`.
