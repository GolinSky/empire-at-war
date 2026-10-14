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
