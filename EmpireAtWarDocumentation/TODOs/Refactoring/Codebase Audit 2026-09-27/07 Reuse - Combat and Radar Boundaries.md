---
tags:
  - code-audit
  - refactoring
created: 2026-09-27
status: implemented
scope: read-only source review
updated: "2026-09-27\r"
category: Refactoring
---

# Reuse — Combat and radar boundaries

> [!info] Implementation update
> R1 and R2 implemented: weapon rules accept plain accuracy/range/roll values; radar's unused masks are deleted and dependencies are constructor-injected. Existing ObservableList remains; radar is not claimed to be completely Unity-free.
> See [[TODOs/Refactoring/Codebase Audit 2026-09-27/09 Implementation Results|implementation results and verification]]. Evidence/line numbers below describe the original audit snapshot unless marked implemented.

- [[TODOs/Refactoring/Codebase Audit 2026-09-27/00 Overview|← Audit overview]]

## R1 — WeaponModel contains engine data and random sampling

- **Priority:** P2 · **Confidence:** confirmed MVP boundary issue.

- **Evidence:** [Assets/Scripts/Components/Weapon/WeaponModel.cs:9](file:///F:/Private/empire-at-war/Assets/Scripts/Components/Weapon/WeaponModel.cs#L9) depends on WeaponsData/DamageMatrixData and uses UnityEngine.Random.value and Mathf.
- That conflicts with the pure-C# model rule.

- **Solution:** expose a small read-only combat configuration contract containing plain values; keep ScriptableObject lookup in the Unity adapter.
- Pass a random sample into hit evaluation or inject a narrow random-source contract.
- Use ordinary numeric operations in the model.

- **Reuse:** one hit/accuracy rule can then be used by weapons and other combat consumers without pulling Unity assets into the model.
- Keep damage-type/ship-class policy in the combat domain, not global Utils.

- **Responsibilities:** Model = range/accuracy rules; Presenter/component = obtains configuration and random sample; Unity adapter = assets/random integration.

- **Future verification:** preserve hit threshold, accuracy matrix and half-max-range calculation.
- Do not introduce a project-wide randomness service without another real consumer.

## R2 — Remove dead radar masks and hidden injection

- **Priority:** P2 · **Confidence:** confirmed unused members.

- Original proposal: move layer-mask resolution.
- RadarModel.LayerMask and EnemyLayerMask had no readers.
- RadarComponent already uses ILayerService.

- **Implemented:** deleted both masks and LayerData dependency; removed dead comments/imports; constructor-injected IRadarData and PlayerType.
- Station/platform/mining installers now construct RadarModel instead of injecting serialized empty instances, and the obsolete serialized model properties were removed from those data classes.
- IRadarData and IRadarModelObserver have separate files.

- Unity ObservableList contract retained.
- Remaining radar dependencies are not all engine-independent.

- **Verification:** compilation succeeds.
- No radar runtime test was run.
