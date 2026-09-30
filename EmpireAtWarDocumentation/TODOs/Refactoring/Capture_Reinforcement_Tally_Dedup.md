---
category: Refactoring
status: in-progress
created: 2026-09-30
tags:
  - code-audit
  - duplication
---
# Capture / Reinforcement Tally Dedup

[[TODOs/Refactoring/Audit_Remediation_Coordination|← Coordination]] · Wave 1 · Unity lane: no

## Goal
- One owner for "collect living squadrons + add squadron strength to a `CaptureTallyBuilder`", next to the existing ship tally `ShipPopulation.AddShipStrength`.

## Findings (CO2)
- `CaptureSitesSystem.CollectSquadrons():341` ≡ `ReinforcementZonesSystem.CollectSquadrons():155` (same filter: `Model is ISquadronModelObserver && !HealthModel.IsDestroyed`).
- The squadron loop is also duplicated: `CaptureSitesSystem.TallySite:353-365` ≡ `ReinforcementZonesSystem.Tick:131-140` (`zone.Contains(squadron transform position)` → `_tally.Add(owner, SquadronCaptureWeight)`).
- `CaptureSitesSystem.cs:356` allocates a lambda `position => site.Contains(position)` per site per tick; `ReinforcementZonesSystem` passes a method group, which also allocates.

## Decision
- Chosen: extend `Services/Ship/ShipPopulation.cs`, or add a sibling `SquadronPopulation`, with `AddSquadronStrength(IReadOnlyList<IEntity> squadrons, Func<Vector3,bool> contains, float weight, CaptureTallyBuilder tally)` plus one `CollectLivingSquadrons(IEntityLocator, List<IEntity>)`.
- Why: both systems keep their own weight (`_data.SquadronCaptureWeight`) and per-area policy.
- Avoid: a base class for both systems; a spatial index (not measured).

## Files (owned)
- `Assets/Scripts/Services/CaptureSites/CaptureSitesSystem.cs`
- `Assets/Scripts/Services/ReinforcementZones/ReinforcementZonesSystem.cs`
- `Assets/Scripts/Services/Ship/ShipPopulation.cs` (+ new sibling file if chosen)
- `Assets/Scripts/Tests/Editor/AuditSharedOperationsTests.cs`
- Do **not** touch `HasHostileUnits` semantics or the Random usage (CO9, out of scope).

## Steps
1. [x] Add the squadron helpers; keep the one-type-per-file rule. → `Services/Ship/SquadronPopulation.cs`: `CollectLivingSquadrons(this IEntityLocator, List<IEntity>)`, `AddSquadronStrength(this IReadOnlyList<IEntity>, Func<Vector3,bool>, float, CaptureTallyBuilder)`.
2. [x] Replace both `CollectSquadrons` bodies and both squadron loops.
3. [x] Partial: one delegate per site/zone per tick (shared by ship + squadron tally), was 2 in zones. Full caching skipped: `CaptureSitePresenter.Contains` has an optional `clearance` param, so it needs a lambda; caching per site is not trivial.
4. [x] Extended `AuditSharedOperationsTests`: `SquadronStrength_SkipsDestroyedAndNonSquadronsAndAppliesWeightInsideArea` (destroyed/non-squadron excluded, weight 0.5 applied, `x <= 5` boundary). Not run.

## Verification
- Compile clean. Manual: capture a site with ships only, squadrons only, and a mix; a reinforcement zone flips owner at the same speed as before.
- 2026-09-30: `unity command recompile` → no errors in owned files. Full compile blocked by unrelated WIP (`DebugRangeCircleFactory.cs(21,24) CS1729`). Tests not run.
- TODO: manual capture/zone playtest.
