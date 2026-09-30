---
category: Refactoring
status: blocked
---
# Ship Lit Autodesk Material Conversion Plan

## Lifecycle Review

- Reviewed: 2026-09-30; blocked on the recorded user visual approval of the before/after gallery. Conversion and idempotency evidence do not replace that acceptance gate.

## Goal

- Convert unit Autodesk materials to Ship Lit while preserving verified appearance.
- Preserve shared non-unit consumers through unit-only copies.
- Team-mask authoring remains optional and outside the approved conversion.

## Decisions

- Preserve square-root roughness, branch-based colors/emission, red AO sampling, source color-space decoding.
- `metal_gloss` is interpreted as roughness per verified shader semantics.
- Convert 6 unit-only materials in place; create exactly 4 `_ShipLit.mat` copies for shared materials.
- Keep shader feature set/keywords and VFX/reinforcement preview materials unchanged.

## Implementation

1. Audit shader channels/importers, consumers, and baseline renders.
2. Add `MetallicSmoothnessPacker` for pure channel math.
3. Add focused `AutodeskMaterialConverter` alongside `ShipLitSetupTool` in `Editor/Rendering`.
4. Bake packed textures; convert/copy materials; repoint only approved unit-prefab slots.
5. Refresh, explicitly reserialize/save affected assets; compare values, visuals, and idempotency.

## Recorded Results

- Audit: 1,462 prefab/scene/asset paths; 475 slots; 10 distinct materials.
- Phase 0 + Phases 1–3 explicitly approved in chat; seven before/after render pairs captured.
- Menu rerun: 0 conversions; 99 already-Ship-Lit skips.
- 59 output files retained hashes and timestamps on rerun.
- Largest average foreground RGB change: Separatist station, approximately 0.23% of 8-bit range.
- No new Console errors after cursor 2513; `MainMenuScene` stayed clean.

## Edge Cases

- Shared `Dull_Metal.mat` isolation check → operate on its unit copy.
- `_UvTiling` / `_UvOffset` drive Autodesk tiling; not `_MainTex_ST`.
- Preserve known art oddities: ignored lambert1 color texture, cross-set textures, defaultMat red AO, set2 emission.
- Texture packing/filtering/compression → small highlight changes; pixel identity not claimed.

## TODO

- User visual approval of `Logs/ShipLitConversion/2026-09-28/review.html` remains recorded as pending.
- Optional manual skirmish / Frame Debugger checks; no automated tests or Play Mode were performed.

## Files

- [[TODOs/Refactoring/ShipLit_Autodesk_Material_Conversion_Plan - Research]] — property mapping, channel math, consumer tables, full conversion and visual evidence.
- `Logs/ShipLitConversion/2026-09-28/` — gallery, audits, verification, idempotency results.
