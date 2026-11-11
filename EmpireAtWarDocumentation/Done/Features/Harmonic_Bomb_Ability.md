---
category: Features
status: done
updated: 2026-11-11
completed: 2026-11-11
acceptance: pending-user
---
# Seismic Charge — Shared Slave I Ability

## Goal

- Rework Boba and Jango Fett’s shared ability as Seismic Charge; preserve `ShipAbilityId.HarmonicBomb` (`54`) and existing integration.
- User-approved tuning: `360` base damage, fighter multiplier `2`, radius `210` world units, stationary `1.5 s` fuse and `20 s` activation cooldown.

## Implementation

1. [x] Rename player-facing ability and replace instant squadron kill with one spherical enemy AoE. Source verified: inclusive 210-unit sphere; 360 base per health entity; 720 per live squadron member (including bombers/interceptors/heroes); friendly immunity, no falloff. Existing HypervelocityGun damage rules apply.
2. [x] Replace the white line ring with a pooled white-blue core and two flat additive Shader Graph shockwaves; match visual and damage radii.
3. [x] Import/save assets, verify source and Unity compilation/bindings, and extract current rules into [[GameDesign/Abilities_And_Superweapons]].
4. [x] Close with user-only skirmish/visual/listening gates in [[TODOs/User_Acceptance_Queue]]. Tests remain disabled by checkout policy.

## Important Values

- Cooldown: `1.5 s` active fuse + `18.5 s` recovery = `20 s` from activation. Remaining fuse + recovery survives save/load; the pending bomb does not.
- Snapshot damage candidates before callbacks; recheck entity registration and member identity/generation before application. No Physics queries; bomb timer records and views pooled.
- VFX: core `0–0.15 s`; main ring begins `0.08 s`, reaches radius at `0.60 s`, fades by `0.80 s`; secondary ring begins `0.14 s`.
- Core `#E8FFFF`; ring `#43DFFF`; secondary `#008CFF`; no realtime lights or particle system for the main wave.

## Decision

- Reopened `2026-11-11` for the requested gameplay/VFX rework. Earlier completion: `2026-11-06`, with user acceptance pending.
- Earlier documented implementation used radius `35`, falloff `120/72/30` and a `0.35 s` white ring. Live-source inspection found radius `210`, instant ordinary-squadron destruction and hero immunity; historical notes did not describe current behavior.
- Preserve battle-owned dropped charges across caster movement/death, local visibility and original detonation audio.

## Files

- `Assets/Scripts/Services/ShipAbilities/Abilities/HarmonicBomb/` — shared settings, activation, damage/timer service and pooled effects.
- `Assets/Scripts/Components/ViewComponents/Weapon/HarmonicBombView.cs`; `Assets/Prefabs/Vfx/Projectiles/HarmonicBomb.prefab` — presentation.
- `Assets/Settings/Data/Models/ShipAbilities/ShipAbilityCatalog.asset` — shared definition/settings.
- Original source provenance → [[Done/Features/Skirmish_Hero_Fighters - Research]].

## Audio Source

- `PROJECTILES.XML` → `Proj_Harmonic_Bomb_Slave_I.Death_SFXEvent_Start_Die` → `SFX_Sizemic_Detonation`; `SFXEVENTSDEATHS.XML` → `EXP_Sizemic_Charge_2.wav`.
- Source WAV: mono 16-bit PCM, `44,100 Hz`, `132,877` samples (`3.013 s`). Original event predelay `1000 ms`; project triggers at fuse expiry with no extra predelay.
- Pilot activation voices: Jango `JANGO_SHIP_ATTACK_06_ENG`; Boba `U000_BFT1612_ENG`.
- Historical `2026-11-06` verification: source/imported explosion SHA-256 matched; Unity read back mono `132,877` samples, `3.013084 s`, pilot clips and both ID `54` slots. Tests were not run.

## Verification

- `2026-11-11`: Unity compilation finished (`compiling=false`, `failed=false`); scoped `git diff --check` passed. Tests not run under checkout policy.
- Saved prefab has three children with verified mesh, renderer and shared material bindings. Shader Graph/material imported and saved; all shader passes compiled with zero messages.
- Isolated renders verified core flash at `0.04 s` and cyan main/blue secondary flat rings at `0.35 s`: `Logs/SeismicCharge/core-preview.png`, `Logs/SeismicCharge/shockwave-preview.png`. Previews omit battle Bloom. Corrected a UV varying error; latest Console check (cursor `786`) had zero new errors.
- Catalog and faction assets explicitly reserialized and saved. Existing battle camera has HDR/post-processing enabled; global battle Volume Bloom intensity `1`, threshold `1`. Performant quality’s existing HDR-disabled policy retained.
- Reusable gameplay rules extracted to [[GameDesign/Abilities_And_Superweapons#Seismic Charge (Harmonic Bomb ID 54)]]. Rendering: shared mesh/material, cached property IDs and reusable MaterialPropertyBlock; no per-frame material creation, mesh deformation, lights or Physics query. MPB renderers forgo SRP Batcher compatibility; no performance gain claimed.
- In-battle gameplay, visual and listening acceptance remain user-only gates in [[TODOs/User_Acceptance_Queue]]. Implementation complete; `acceptance: pending-user`.
