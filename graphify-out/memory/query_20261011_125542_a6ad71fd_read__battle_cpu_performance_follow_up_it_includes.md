---
type: "query"
date: "2026-10-11T12:55:42.357006+00:00"
question: "read  Battle CPU performance follow-up\nit includes me running the battle - but just skip it - we have some optimazation already - we have profiler and frame debugger data - just analyze related files we have mentioned in obsidiab note"
contributor: "graphify"
outcome: "useful"
source_nodes: ["Squadron", "ShipSfxService", "WeaponComponent", "CombatAttackCoordinator", "ShipNavigationService"]
---

# Q: read  Battle CPU performance follow-up
it includes me running the battle - but just skip it - we have some optimazation already - we have profiler and frame debugger data - just analyze related files we have mentioned in obsidiab note

## Answer

Expanded from original query via graph vocabulary: [combat, target, squadron, navigation, radar, audio, profiler, capture]. Graphify oriented Squadron and ShipSfxService relationships; live Serena source at 74368d81 is authoritative. Existing data priorities: target snapshot capture 16.459 ms in fresh 12 frames / 21.118 ms in the earlier 78-frame CSV; due apply 12.010/16.864 ms, separate 287.191 ms spike at completed frame 20777; navigation burst 81.268 ms across 35 plans at frame 20800; Squadron.Tick 8.469 ms covers orders, pilot and flight, independent weapon ticking. Current improvements retained: centralized radar, cached ordered candidates, counted health collection, cheaper ring candidate checks, pooled idle effects and 0.5 s aim refresh. Repeated whole-pool mixing over 20 voice slots and tilted-parent targeting fallbacks are source-grounded candidates, not measured gains. Summary exporter omits allocation bytes/ancestry/stacks; Frame Debugger persists only complete frames. No battle, Unity commands, captures, replay, tests or game changes. Full analysis and source lines saved through empire-vault in TODOs/Optimization/Battle_CPU_Performance_Plan - Research.md; plan and backlog updated, broader implementation/runtime acceptance remains in-progress.

## Outcome

- Signal: useful

## Source Nodes

- Squadron
- ShipSfxService
- WeaponComponent
- CombatAttackCoordinator
- ShipNavigationService