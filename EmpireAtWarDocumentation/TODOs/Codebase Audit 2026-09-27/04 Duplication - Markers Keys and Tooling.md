---
tags:
  - code-audit
  - refactoring
created: 2026-09-27
status: proposed
scope: read-only source review
---
# Duplication — Markers, keys and tooling

[[TODOs/Codebase Audit 2026-09-27/00 Overview|← Audit overview]]

## D4 — Minimap presenters repeat marker lifetime bookkeeping
**Priority:** P3 · **Confidence:** confirmed.

**Evidence:** [Assets/Scripts/Entities/MiniMap/Presenter/CaptureSiteMiniMapPresenter.cs:13](file:///F:/Private/empire-at-war/Assets/Scripts/Entities/MiniMap/Presenter/CaptureSiteMiniMapPresenter.cs#L13) and [Assets/Scripts/Entities/MiniMap/Presenter/ReinforcementZoneMiniMapPresenter.cs:10](file:///F:/Private/empire-at-war/Assets/Scripts/Entities/MiniMap/Presenter/ReinforcementZoneMiniMapPresenter.cs#L10) both build a source→marker dictionary, register markers, iterate during LateTick, and unregister/clear on disposal.

**Solution:** extract a small marker collection/lifetime collaborator only. Keep capture-site operational visibility, zone diameter, and last-seen ownership policy in their respective presenters. Avoid a generic presenter inheritance hierarchy for two small classes.

**Future verification:** registration/removal happen once; unrevealed ownership stays hidden; operational capture-site hiding and always-drawn zones remain distinct.

## D5 — Enemy reservation keys are independently constructed
**Priority:** P2 · **Confidence:** confirmed.

**Evidence:** [Assets/Scripts/Entities/EnemyFaction/Controllers/EnemyFactionController.cs:223](file:///F:/Private/empire-at-war/Assets/Scripts/Entities/EnemyFaction/Controllers/EnemyFactionController.cs#L223) builds `runtime request type FullName + ':' + Id`. [Assets/Scripts/Entities/EnemyFaction/Models/EnemyUnitLimitModel.cs:52](file:///F:/Private/empire-at-war/Assets/Scripts/Entities/EnemyFaction/Models/EnemyUnitLimitModel.cs#L52) and line 94 build the equivalent key from a generic request type.

**Solution:** give reservation identity one owner. Use a small typed key containing request kind and canonical request identifier, or centralize existing key construction as the smaller first step. Reservation, availability checks and release must use the same representation. Keep this domain-specific; it is not a generic string utility.

**Future verification:** reserve/query/release agree for every request kind, and equal text IDs in different request kinds remain distinct. No collision was reproduced.

## D6 — Audio-generation scripts duplicate WAV encoding
**Priority:** P3 · **Confidence:** confirmed.

**Evidence:** [Tools/Audio/generate_ship_sfx.py:105](file:///F:/Private/empire-at-war/Tools/Audio/generate_ship_sfx.py#L105) and [Tools/Audio/generate_weapon_sfx.py:86](file:///F:/Private/empire-at-war/Tools/Audio/generate_weapon_sfx.py#L86) both remove DC offset, normalize peak level, encode signed 16-bit PCM, handle byte order and write mono WAV at 44.1 kHz.

**Solution:** extract the shared encoder into a small module within `Tools/Audio`. Keep synthesis and weapon-specific RMS/seam reporting in the calling scripts. Pass output path, samples, peak and sample rate explicitly.

**Future verification:** compare bytes from representative existing inputs and preserve command-line invocation from any working directory. Do not put offline authoring utilities in the game's runtime Utils folder.
