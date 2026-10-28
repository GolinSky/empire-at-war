---
category: Features
status: todo
created: 2026-10-28
---
# Space Hazards Plan - Visuals

Art direction for [[TODOs/Features/Space_Hazards_Plan|Space Hazards Plan]]. Working summary lives in its Visual Spec section.

## Nebula (Gas Cloud)

- Message: "abilities don't work here"; must not suggest shield or hull damage.
- Shape: irregular, stretched, layered mass; soft feathered border; drifting wisps.
- Colors: purple, magenta, dusty pink; slight blue tint in shadowed areas. Base `#8658B8`, `#B04F95`.
- Brightness medium-low; soft contrast. Mood: mysterious interference, sensor distortion.
- Layers:
  1. Base volume: translucent plane/mesh, 2–3 slow-panning noise layers, uneven opacity.
  2. Edge: noisy fade, fog-like fringes, faint glow only in dense areas.
  3. Micro motion: slow drift, slight internal swirl, lazy dust motes.
  4. Interference: very subtle shimmer/distortion, occasional faint pulse. No lightning.
  5. Suppression cue on entry: brief purple pulse under/around the ship, faint "signal jam" ripple, ability icons dim. Subtle, not spammy.
- Ships: fully visible; selection circles readable; shield bubble normal; weapons fire normally.

```
Gas cloud / nebula hazard for a space RTS. Large irregular semi-transparent purple-magenta gas formation in space, with soft feathered edges, layered drifting mist, subtle swirling motion, faint interference shimmer, no lightning, readable from top-down strategy camera, ships visible inside, mysterious but not highly aggressive.
```

## Ion Storm

- Message: abilities **and** shields disabled; must look more threatening than the nebula at a glance.
- Shape: denser, unstable mass; soft outer edge, broken energetic interior.
- Colors: deep blue, cyan, electric violet, white electrical highlights. Base `#466AC5`, `#48CBE9`, `#8F62F6`.
- Brightness medium with sharp flash peaks; stronger contrast. Mood: unstable, high-energy.
- Layers:
  1. Base cloud: nebula foundation, denser, colder, more turbulent.
  2. Energy structure: bright veins/streaks, animated flow lines, pulsing intensity zones.
  3. Arcs: short branching, random positions, not constant; sometimes between two cloud points, sometimes around an entering ship.
  4. Flashes: small local flashes every 1.5–3 s; brief brightness spike in nearby gas; never screen-filling. Arc duration 0.15–0.40 s.
  5. Charged particles: fast tiny sparks/streaks, chaotic motion.
  6. Shield suppression cue on entry: bubble flickers → collapses (0.2 s) + blue-white arc snaps across hull. Key difference from the nebula.
- Ships: visible but partially obscured; selection markers and team colors readable; small periodic hull flicker while inside.
- Implementation: same base as nebula + FX layer; shared material with preset override; arcs as sprites/mesh strips, few particle systems.
- Shader: animated emissive charged regions, faster scroll, pulse mask, flash multiplier, optional distortion near arcs.

```
Ion storm hazard for a space RTS. Dense blue-cyan-violet charged storm cloud in space, irregular semi-transparent volume with turbulent motion, internal glowing energy veins, brief branching lightning arcs, occasional flashes, highly readable from top-down strategy camera, visually communicates shield suppression and system disruption.
```

## Readability Rules

- Shape visible at far zoom; inner FX readable at medium zoom.
- Ships never disappear; selection circles, health bars, target lines readable.
- Opacity: nebula 0.15–0.28, ion storm 0.18–0.32; denser center pockets allowed if gameplay stays visible.
- No full-screen post-processing, no volumetrics.

## Extra Feedback

- Nebula: purple entry pulse, small "ability blocked" icon over ship, faint distorted comms ring.
- Ion Storm: shield flicker → shutdown, blue arc hull impact, periodic flickers inside, UI "Shields Offline".

## Implementation Review — 2026-10-28

- MVP saved in `Assets/Prefabs/Vfx/SpaceHazards/`; shared shaders/materials and generated minimap icons wired.
- Editor captures: `Logs/SpaceHazards/` at camera min/max zoom and medium zoom; both types distinguishable, hull/team colors readable.
- Five sheets cover `-650` → `180` m; dominant sheet near ship height, outer opacity `0.015` reduces parallax duplicates.
- Runtime entry cues, flashes, fog/UI readability and final art acceptance require user in-game review. Full optional VFX pass deferred.

## MVP

- Nebula: one irregular cloud mesh, two scrolling noise layers, purple gradient, light dust, entry pulse.
- Ion Storm: reuse nebula base, recolor blue/cyan, simple arc prefab, shield collapse on entry, random flash pulse.
