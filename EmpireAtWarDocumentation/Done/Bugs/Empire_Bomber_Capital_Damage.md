---
category: Bugs
status: done
acceptance: pending-user
created: 2026-11-23
completed: 2026-11-23
system:
  - balance
  - squadrons
---
# Empire Bomber Capital Damage

## Goal
- User report 2026-11-23: all Empire bombers deal little damage to capitals (MC80).

## Evidence
- Player build = release → no damage CSV. Source: Editor battle `BattleTelemetry/Battle_20261123_182426.csv` (1999 s, Empire vs Rebellion) + `Loading_20261123_182326_squadrons.csv`.
- TIE Bomber: 106 squadrons launched, 549 torpedo hits total (~5/squadron; volley = 6). Median life 13 s after launch (Y-Wing Bomber 11 s).
- Vs MC80 Remonda (hull `40000`, shields `21000`): all TIE Bombers landed 30 torpedoes, 4105 hull in the match.
- What kills TIE Bombers: ship `Laser` + `PointDefense` ≈ 70% of damage taken; Y-Wing Bombers die mostly to TIE fighters/interceptors.
- Torpedoes fire once per attack run (`FighterTorpedoHardPoint.CanStartSalvo`) → damage per pass matters, not reload DPS. TIE Bomber and BTL-A4 have identical run geometry.
- Pass damage before: TIE Bomber `666`, Punisher `888`, BTL-A4 `1258`, NTB-630 `814`. TIE Bomber was the price anchor in [[Done/Bugs/Squadron_Faction_Balance]] and was left unchanged there.

## Decision
- Ship flak vs bombers (`DamageMatrixData`, rows `Laser` 0 and `PointDefense` 1, `bomber` damage): `2.5 → 1.75`, all factions. Effectiveness Laser `2.25 → 1.575`, PD `2.375 → 1.66`; fighters stay lower (`1.4` / `1.5`).
- Torpedo +30% (`WeaponsData`): TIE Bomber `145` `0.7704 → 1.0` (pass `666 → 864`); TIE Punisher `147` `1.028 → 1.336` (pass `888 → 1154`).
- `EmpireFaction.asset` descriptions: volley `332.8 → 432.0`, `444.1 → 577.2` (description convention, 72 base).
- Why: survival is the main loss; volley raise was the user's explicit request ("fix all issues"). Departs from the 2026-11-19 price-anchor rule for Empire bombers only.
- AI orders: no change. Guarding hangar bombers already join the ship their carrier fights (`SquadronTargetSelector.SelectGuardTarget`); idle Guard time is when the carrier is not fighting. Bomber strike range stays with [[TODOs/Bugs/Enemy_AI_Audit_Fixes]] SQ2.

## Verification
- Unity refresh + reserialize of the 3 assets; console clean. Live readback: TIE Bomber torpedo `144`, Punisher `192.4`; flak values above.
- Tests reviewed: `WeaponTaxonomyTests` (flak > 1 vs strikecraft; bomber effectiveness > fighter) still holds; others build their own matrices. Tests not run (local override).
- Reusable guidance: none beyond this note; torpedo cadence = attack runs is recorded above.

## Files
- `Assets/Settings/Data/Models/Weapon/DamageMatrixData.asset`
- `Assets/Settings/Data/Models/Weapon/WeaponsData.asset`
- `Assets/Settings/Data/Factions/Empire/EmpireFaction.asset`
