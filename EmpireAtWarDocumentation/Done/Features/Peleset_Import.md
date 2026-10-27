---
category: Features
status: done
updated: 2026-10-27
completed: 2026-10-27
---
# Peleset Import

## Goal

- Add `TU_Ordance_Cruiser` as Peleset-class Cruiser to Separatist/CIS roster.
- Complete source model, two attached turrets, gameplay data, 15 targetable mounts, preview, wreck, icon and registrations.

## Decision

- Hull `7000`; shields `3000`; cruiser regeneration `90 shields/s`.
- All 13 weapon mounts Medium: six dual turbolasers, two point defense, two ions, two concussion launchers, one proton launcher.
- Weapon HP `400`; two turret weapons `500`; engine and shield HP `800` each. User approved per-mount HP override; zero retains existing type defaults.
- Shared weapon-family profiles supply combat balance. Source range/burst/recharge remain provenance; armor strings and planet requirements omitted.
- Cost `6000`; build `60 s`; level `4`; population `4`; provisional max count `10`.
- Provisional length `124.6333` units (normalized Recusant size), speed `19 units/s`, turn `30 degrees/s`.

## Implementation

- [x] Audit source XML, binary models, exact texture references and SHA-256 hashes.
- [x] Convert hull + both separate turrets; retain source bones/material assignments; verify FBX round trip.
- [x] Build centered visuals, fitted shields/collision/selection, gameplay hardpoints, preview, wreck, icon and CIS registrations.
- [x] Extend live health and simulator mount data for optional HP overrides; retain upgrade/save/restore behavior.
- [x] Verify persisted references, initialized HP, weapon profiles, imports, Console and renders.

## Important Values

- Ship enum `Peleset = 110`; shared `MediumPointDefense = 97`.
- Source hull `104` bones; turrets `5` each. Maximum raw bone position error `0.000119` source units; UV round-trip error `0`.
- Source normal: `SeV_Techno_Union_Tambor_Cruiser_N.dds` (binary reference).
- Turret sockets `T01` / `T02`; right PD socket `FP_B-R_Bone`.
- Final visual bounds `23.4646 × 24.6928 × 124.6333`; bow `+Z`; root identity.

## Edge Cases

- Source collision/shadow helpers retained in transport files; runtime helper renderers removed.
- Source blue paint supports project team palette; icon, placement and wreck rendered and inspected.
- Compilation and saved-asset checks passed; no new Console errors since baseline cursor `460`. MainMenuScene remains clean.
- Focused health/simulator regression cases authored; automated tests and Play Mode were not run. In-battle balance/feel remains unverified.

## Files

- Source `D:/SteamLibrary/steamapps/workshop/content/32470/1129810972/Data/XML/Units_Space_cis_technounion_cruiser.xml`.
- Workflow [[AssetPipeline/Guides/ALO_MODEL_IMPORT_GUIDE]].
- Tools and persistent evidence `Tools/Blender/Peleset/`; editable conversion output and renders `Temp/PelesetImport/`.
- Ship assets `Assets/Prefabs/Models/Ships/Separatist/PelesetShipView.prefab`, `Assets/Settings/Data/Ship/Separatist/PelesetShipData.asset`.
