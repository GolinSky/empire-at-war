---
category: Optimization
status: done
created: 2026-11-14
system:
  - battle-cpu
  - enemy-ai
completed: 2026-11-14
acceptance: pending-user
---
# Enemy Production CPU Spike Plan

## Goal

- Remove recurring enemy production stalls in the battle main thread.
- Preserve placement eligibility and fallback order for fresh searches.

## Implementation

- [x] Profile production evaluation, placement, local rings, zones, map fallback, scout target, safety checks and snapshot capture. Emit one telemetry record for map searches exceeding three rings.
- [x] Check bounds, recent destruction, vision, blockers, hazards and structure exclusions before threat disks. Run physics clearance last.
- [x] Capture hostile sightings, bases, patrol structures and lost-home exclusion once per search. Keep the inclusive clearance boundary and live checks for recovery/route callers.
- [x] Cache placement and scout results per AI per frame. Revalidate the last good placement when the origin and recovery mode match. Build, destruction and reset invalidate cached queries and physics sync.
- [x] Skip Phase 4. The observed full-map fallback stayed within 3.30 ms, so no cooldown or sliced scan was needed.
- [x] Author EnemyAi regressions for placement reuse, threat boundary parity and forgotten sightings. Update spawn-rule stubs. Unity compilation and Console checks passed. No automated tests executed under the local override.

## Verification (2026-11-14)

- Baseline `Logs/RenderAudit/20261114T143247414Z-profiler`, frames 0–119: `EnemyService.Tick` p99 284.45 ms / max 294.08 ms from the top-40 export. The slowest frame was 310.33 ms.
- Restored the same Small map seed `-2095135882`, one UltraHard AI, from its 600 s autosave. Exact battle state and intel differ from the baseline. Original checkpoint copy: `Logs/EnemyProductionCpu/baseline-autosave.json`.
- Final `Logs/RenderAudit/20261114T144334101Z-profiler`, frames 122–481: `EnemyService.Tick` p99 4.14 ms / max 4.55 ms. Main-thread p99 25.30 ms / max 40.34 ms. Slowest frame: file loading during an enemy build.
- 21 production evaluations. Placement max 3.67 ms. 12 map scans, max 3.30 ms. Snapshot max 0.16 ms and safety checks ≤0.11 ms per frame. Raw marker metrics: `Logs/EnemyProductionCpu/final-marker-times.json`.
- Scout probe `Logs/RenderAudit/20261114T144725499Z-profiler`, frames 567–569: two same-frame queries returned the same target and shared one map scan. Both queries together took ≤0.29 ms.
- Historical capture cannot attribute the old production cost below `EnemyService.Tick`. User requested profiler checks after implementation, so no markers-only pre-change capture was taken.

## Decision

- Keep the existing ring scan. Revisit only if a larger or more blocked map shows a placement spike.
- Canonical reusable guidance → [[Rules/BATTLE_PERFORMANCE_GUIDELINES#CPU scheduling and snapshots]].
