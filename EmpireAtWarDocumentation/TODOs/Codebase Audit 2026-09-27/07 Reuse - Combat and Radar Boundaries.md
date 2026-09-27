---
tags:
  - code-audit
  - refactoring
created: 2026-09-27
status: proposed
scope: read-only source review
---
# Reuse — Combat and radar boundaries

[[TODOs/Codebase Audit 2026-09-27/00 Overview|← Audit overview]]

## R1 — WeaponModel contains engine data and random sampling
**Priority:** P2 · **Confidence:** confirmed MVP boundary issue.

**Evidence:** [Assets/Scripts/Components/Weapon/WeaponModel.cs:9](file:///F:/Private/empire-at-war/Assets/Scripts/Components/Weapon/WeaponModel.cs#L9) depends on WeaponsData/DamageMatrixData and uses UnityEngine.Random.value and Mathf. That conflicts with the pure-C# model rule.

**Solution:** expose a small read-only combat configuration contract containing plain values; keep ScriptableObject lookup in the Unity adapter. Pass a random sample into hit evaluation or inject a narrow random-source contract. Use ordinary numeric operations in the model.

**Reuse:** one hit/accuracy rule can then be used by weapons and other combat consumers without pulling Unity assets into the model. Keep damage-type/ship-class policy in the combat domain, not global Utils.

**Responsibilities:** Model = range/accuracy rules; Presenter/component = obtains configuration and random sample; Unity adapter = assets/random integration.

**Future verification:** preserve hit threshold, accuracy matrix and half-max-range calculation. Do not introduce a project-wide randomness service without another real consumer.

## R2 — RadarModel owns Unity layer selection
**Priority:** P2 · **Confidence:** confirmed MVP boundary issue.

**Evidence:** [Assets/Scripts/Components/Radar/RadarModel.cs:19](file:///F:/Private/empire-at-war/Assets/Scripts/Components/Radar/RadarModel.cs#L19) exposes LayerMask through the observer; the model injects LayerData and picks friendly/enemy masks from player identity.

**Solution:** move physics-mask resolution into the radar Unity adapter or existing LayerService. Keep range, delay, contacts and ownership rules in a pure model. The entity continues to wire its own components; a new helper must not become a backchannel for commanding sibling components.

**Reuse:** the existing layer service is the natural shared boundary for physics filtering. Avoid adding another service that duplicates it.

**Future verification:** player/opponent masks remain identical, contact filtering stays unchanged, and model-facing contracts no longer require UnityEngine. Read-only contact access is preferable where mutation is currently exposed.
