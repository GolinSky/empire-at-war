---
tags:
  - code-audit
  - refactoring
created: 2026-09-27
status: implemented
scope: read-only source review
updated: 2026-09-27
---
# Duplication — Markers, keys and tooling

> [!info] Implementation update
> D4, D5 and D6 implemented. The follow-up request explicitly authorized D4: MiniMapMarkerCollection owns registration/cleanup by composition, while both presenters retain their own marker construction and visibility policies.
> See [[TODOs/Codebase Audit 2026-09-27/09 Implementation Results|implementation results and verification]]. Evidence/line numbers below describe the original audit snapshot unless marked implemented.

[[TODOs/Codebase Audit 2026-09-27/00 Overview|← Audit overview]]

## D4 — Minimap presenters repeat marker lifetime bookkeeping
**Priority:** P3 · **Confidence:** confirmed.

**Evidence:** [Assets/Scripts/Entities/MiniMap/Presenter/CaptureSiteMiniMapPresenter.cs:13](file:///F:/Private/empire-at-war/Assets/Scripts/Entities/MiniMap/Presenter/CaptureSiteMiniMapPresenter.cs#L13) and [Assets/Scripts/Entities/MiniMap/Presenter/ReinforcementZoneMiniMapPresenter.cs:10](file:///F:/Private/empire-at-war/Assets/Scripts/Entities/MiniMap/Presenter/ReinforcementZoneMiniMapPresenter.cs#L10) both build a source→marker dictionary, register markers, iterate during LateTick, and unregister/clear on disposal.

**Solution:** extract a small marker collection/lifetime collaborator only. Keep capture-site operational visibility, zone diameter, and last-seen ownership policy in their respective presenters. Avoid a generic presenter inheritance hierarchy for two small classes.

**Verification:** the collection test checks independent ownership, repeated cleanup, and reuse after cleanup. Source review confirms last-seen ownership, operational capture-site hiding, and zone diameter/visibility policy remain in their original presenters.

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
