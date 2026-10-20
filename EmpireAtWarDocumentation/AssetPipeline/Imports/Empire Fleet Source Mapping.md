---
type: source-mapping
system:
  - asset-pipeline
  - units
design_state: proposed
source: GameDesign/Proposals/Empire Fleet Import
reviewed: 2026-10-20
---
# Empire Fleet Source Mapping

## Rules

- Source: [[GameDesign/Proposals/Empire Fleet Import]]; user-supplied proposal recorded 2026-10-14.
- Source paths, model identities and weapon placements below are proposed/unverified; they do not establish completed conversion or current registration.
- Execution: [[TODOs/Features/Empire_Fleet_Import]]; completed individual imports retain their own dated evidence.

## 1. Procursator-class Star Destroyer

### ALO Mapping

- Unit: Procursator
- ALO: `Empire_Procursator.ALO`
- Build Menu: `Empire_Capital_Shipyard`

**Source Path:**

`D:\SteamLibrary\steamapps\workshop\content\32470\1770851727\Data\Art\Models\Empire_Procursator.ALO`

### Weapon Placement

- 3x Quad Heavy Turbolaser: Dorsal centerline.
- 2x Medium Dual Turbolaser: Port and starboard.
- 8x Light Triple Turbolaser: Dorsal and side batteries.
- 2x Medium Ion Cannon: Forward-facing sections.

Placement must be checked against the imported model.

---

## 2. Allegiance-class Battlecruiser

### ALO Mapping

- Unit: `Allegiance_Star_Destroyer_MP`
- ALO: `Empire_Allegiance_Star_Destroyer_Hangar.alo`
- Build Menu: `Empire_Capital_Shipyard`

**Source Path:**

`D:\SteamLibrary\steamapps\workshop\content\32470\1770851727\Data\Art\Models\Empire_Allegiance_Star_Destroyer_Hangar.alo`

### Weapon Placement

- 3x Dual Heavy Turbolaser: Dorsal centerline.
- 6x Quad Heavy Turbolaser: 3 port / 3 starboard.
- 6x Medium Dual Turbolaser: 3 port / 3 starboard.
- 6x Medium Ion Cannon: Along trench sections.

These groupings are adapted from the documented axial, side and trench battery arrangement. The precise weapon types are gameplay choices.

## 3. Titan-class Battlecruiser

### ALO Mapping

- Unit: `Titan_Star_Destroyer_MP`
- ALO: `Empire_Titan_Star_Destroyer.ALO`
- Build Menu: `Empire_Capital_Shipyard`

**Source Path:**

`D:\SteamLibrary\steamapps\workshop\content\32470\1770851727\Data\Art\Models\Empire_Titan_Star_Destroyer.ALO`

### Weapon Placement

- 12x Quad Heavy Turbolaser: Main dorsal turret positions.
- 6x Medium Dual Turbolaser: Secondary broadside mounts.
- 4x Heavy Ion Cannon: Heavy support batteries.
- 2x Point Defense Laser: Defensive coverage near the superstructure.

## 4. Tartan-class Patrol Cruiser

### ALO Mapping

- Unit: `Tartan_Patrol_Cruiser`
- ALO: `Empire_Tartan.alo`

**Source Path:**

`D:\SteamLibrary\steamapps\workshop\content\32470\1770851727\Data\Art\Models\Empire_Tartan.alo`

### Weapon Placement

- 4x Large Repeating Laser Cannon: Distributed around the hull.
- 1x Heavy Laser Cannon: Main forward battery.
- 4x Point Defense Laser: Forward, port, starboard and rear coverage.

Placement must be adjusted to available model geometry.

---
