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

# Duplication — Coordinates and population

> [!info] Implementation update
> D1, optional D2, and D3 implemented. The follow-up request authorized the two capture-system call-site changes. ShipPopulation.CountShips shares containment/faction counting; the other agent's weighted-squadron behavior remains in each system.
> See [[Done/Refactoring/Codebase Audit 2026-09-27/09 Implementation Results|implementation results and verification]]. Evidence/line numbers below describe the original audit snapshot unless marked implemented.

- [[Done/Refactoring/Codebase Audit 2026-09-27/00 Overview|← Audit overview]]

## D1 — Formation-coordinate adapters are repeated

- **Priority:** P3 · **Confidence:** confirmed.

- **Evidence:** identical `ToPoint(Vector3)` and ground-plane `ToVector(FormationPoint)` helpers occur in [Assets/Scripts/Entities/Ship/Orders/ShipOrderRunner.cs:221](file:///F:/Private/empire-at-war/Assets/Scripts/Entities/Ship/Orders/ShipOrderRunner.cs#L221) and [Assets/Scripts/Entities/Squadron/Squadron.cs:311](file:///F:/Private/empire-at-war/Assets/Scripts/Entities/Squadron/Squadron.cs#L311).
- Additional direct conversions appear in UnitOrderService and PlayerOrderInputHandler.

- **Solution:** add one Unity-bound formation-coordinate adapter under `Components/Utils` or the formation integration boundary.
- Keep `FormationPoint` free of Unity references.
- Make the Y-height choice explicit: some callers force zero, whereas UnitOrderService preserves ship height.

- **Future verification:** X/Z round trips and preservation of each caller's current height convention.
- Do not replace existing numerics conversion helpers with unrelated formation logic.

## D2 — Segment-distance geometry is implemented twice

- **Priority:** P3 · **Confidence:** confirmed small duplication.

- **Evidence:** [Assets/Scripts/Entities/Map/Generation/MapGeometry.cs:37](file:///F:/Private/empire-at-war/Assets/Scripts/Entities/Map/Generation/MapGeometry.cs#L37) computes planar distance to a segment; [Assets/Scripts/Components/Ship/Movement/ShipAvoidancePlanner.cs:232](file:///F:/Private/empire-at-war/Assets/Scripts/Components/Ship/Movement/ShipAvoidancePlanner.cs#L232) computes squared distance.
- Both return distance to the start point for zero-length segments.
- The original note overstated the significance of their tiny-segment arithmetic difference.

- **Solution:** extract a small planar segment-distance utility, preferably with squared distance as the primitive.
- Keep MapGeometry's map/polyline convenience methods near map generation.
- Implemented one DistanceToSegmentSquared primitive; MapGeometry takes its square root.

- **Reuse:** map lane clearance and ship avoidance.
- Do not force Burst/job code through a managed helper or add conversions solely for uniformity.

- **Future verification:** zero-length/tiny segments, endpoints, perpendicular projection and points beyond either endpoint.

## D3 — Capture sites and reinforcement zones repeat ship counting

- **Priority:** P2 · **Confidence:** confirmed.

- **Evidence:** [Assets/Scripts/Services/CaptureSites/CaptureSitesSystem.cs:286](file:///F:/Private/empire-at-war/Assets/Scripts/Services/CaptureSites/CaptureSitesSystem.cs#L286) and [Assets/Scripts/Services/ReinforcementZones/ReinforcementZonesSystem.cs:108](file:///F:/Private/empire-at-war/Assets/Scripts/Services/ReinforcementZones/ReinforcementZonesSystem.cs#L108) each enumerate all ships, check containment and count Player/Opponent ownership.

- **Solution:** extract a narrowly scoped ship-population query service with an explicit containment contract.
- Both systems supply their area; the service returns the two counts.
- Keep capture/construction ownership rules in their existing models.

- **Benefit:** one ownership/filtering rule.
- A shared helper alone does not improve the current areas × ships complexity.
- Add a spatial index only after measurement justifies it.

- **Verification:** the shared helper is covered for boundary inclusion, neutral units, empty areas, and membership changes.
- It counts registered ships only; existing squadron weighting remains outside the helper.
