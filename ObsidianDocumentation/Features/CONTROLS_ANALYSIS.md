# Empire at War - Controls & Input System Analysis

This document provides a clean, functional reference of all control dynamics, input mappings, selection logic, movement vectoring, subsystem targeting, and ability hotkeys from *Star Wars: Empire at War*.

---

## 1. Spatial Navigation & Viewport Control

### Viewport Coordinate Systems
* **Ground Combat**: Constrained top-down 2.5D perspective with planar panning and elevation bounds.
* **Space Combat**: Fully 3D spatial operational plane allowing full camera rotation around engagement centers.
* **Galactic Conquest**: Macro-level galaxy map with planetary focus zooming.

### Panning & Elevation
* **Planar Panning**: Move mouse to display borders (Edge Scroll) or use Arrow Keys.
* **Elevation Zoom**: 
  * **Mouse**: Scroll Wheel forward (zoom in) / backward (zoom out).
  * **Keyboard**: Keypad 8 (Zoom In) / Keypad 2 (Zoom Out).
  * **Galactic Conquest**: Zooming in over a planet transitions viewport directly to planetary base/orbital view.

### Camera Rotation
* **Numpad Step Rotation**: Keypad 4 (Rotate Left) / Keypad 6 (Rotate Right).
* **Middle Mouse Drag**: Hold Middle Mouse Button (MMB) + Drag (or Ctrl + RMB/MMB Drag) for dynamic pitch and yaw.
* **Dual Mouse Button Rotation**: Hold Left Mouse Button (LMB) + Right Mouse Button (RMB) + Drag for high-sensitivity 360-degree rotation (requires setting toggle in menu).

### Camera Locks & Resets
* **Camera Reset**: Press Keypad 5 or Double-Click MMB to snap-reset to default north-facing isometric elevation.
* **Unit Tracking Lock**: Select any friendly/enemy unit + Press `C` to anchor camera to its center of mass.
* **HUD Suppression**: Press `I` to toggle UI overlays (minimap, status rings, command card) on/off.
* **Cinematic Camera**: Click Clapperboard Icon on interface to track active combat dogfights.

### Galactic Map Cycling
* **Planet Focus Cycling**: `F` (Next Planet) / `D` (Previous Planet) to step focus through controlled systems without manual panning.

---

## 2. Selection Architecture & Unit Filtering

### Primary Selection Mechanics
* **Single Focus**: Left-Click (LMB) single allied entity.
* **Bounding Box**: Hold LMB + Drag. Selects all allied units within box coordinates. Hostile entities are strictly excluded.
* **Additive Selection**: Hold `Shift` + Click / Drag box to append units to active selection.

### Multi-Selection & Categorical Filtering
* **Global Select All (`Ctrl + A`)**: Highlights all operational friendly units on the map.
* **Select Like on Screen (`Ctrl + Q`)**: Selects all on-screen units matching current highlighted unit type.
* **Exact Type Match (`Ctrl + LMB` or Double-Click)**: Selects all on-screen friendly units of that exact unit identity (e.g., all Tartan Patrol Cruisers).
* **Broad Class Match (`Ctrl + Double-Click`)**: Selects all on-screen friendly units within broad functional class (e.g., all bomber squadrons regardless of sub-type).

### Control Groups & Unit Cycling
* **Bind Control Group**: `Ctrl + [0-9]` registers selection set.
* **Recall Control Group**: Press `[0-9]` to recall group. Double-tap `[0-9]` centers camera on group geometric midpoint.
* **Unit Hierarchy Cycling**: `F` (Next Unit) / `D` (Previous Unit) to step selection focus through queued units.

---

## 3. Tactical Movement Vectors & Combat Interactions

### Movement & Vector Commands
* **Standard Move**: Right-Click (RMB) or `M` key. Direct transit; units ignore hostiles en route.
* **Attack-Move**: `Ctrl + RMB` or `A` key. Advances toward destination and automatically engages hostiles in weapon range.
* **Directional Vector Facing (Space)**: Hold RMB at target location + Drag directional arrow before releasing. Sets final facing heading upon arrival to shield vulnerable hull arcs/engines.
* **Facing Lock**: Double-tap `S` (Stop) after setting vector to lock unit into assigned facing angle.
* **Waypoint Arrays**: Hold `W` key + click consecutive spatial nodes to queue multi-point movement paths.
* **Stop Action**: `S` key instantly cancels queued movement, ability activation, and targeting orders.
* **Guard Perimeter**: `G` key + RMB target commands unit to form defensive escort screen around allied target.

### Targeting Logic & Sub-system Hardpoints
* **Target Inspection**: Left-click hostile entity to display health, shield capacity, and operational stats in HUD.
* **Focus Fire**: Right-click hostile entity to order selected units to concentrate fire.
* **Capital Ship & Space Station Hardpoints**: Capital ships feature discrete targetable sub-systems:
  * **Shield Generators**: Neutralizes shields, exposing hull to direct weapon damage.
  * **Hangar Bays**: Halts automatic deployment of fighter/bomber reinforcement waves.
  * **Engines**: Disables ship movement and locks facing orientation.
  * **Weapon Systems**: Destroys specific turbolasers, ion cannons, or missile launchers to reduce enemy damage output.
  * **Utility Hardpoints**: Disables tractor beam emitters or gravity well generators to enable tactical hyperspace jumps.

---

## 4. Tactical Pause & Unit Ability Controls

### Active Tactical Pause
* **Toggle Pause**: `Spacebar` or `Pause` key.
* **Behavior**: Freezes game simulation while keeping camera navigation, unit selection, vector plotting, control grouping, and hardpoint targeting fully functional. Commands execute simultaneously upon unpausing.

### Unit Ability Hotkeys

#### Space Combat Abilities
* `S`: Shield Power to Engines (Fighter craft)
* `O`: Boost Engine Power
* `A`: All-Out Barrage (Artillery ships)
* `R`: Redirect All Firepower (Capital ships)
* `W`: Lock S-Foils / Wings (Fighters)
* `T`: Engage Tractor Beam
* `L`: Deploy Lure Flares

#### Land Combat Abilities
* `D`: Deploy / Undeploy Defensive Stance
* `C`: Take Cover
* `T`: Drop Thermal Detonators
* `J`: Jetpack Jump (Infantry/Vehicles)
* `E`: Eject Crew / Capture Vehicle
* `B`: Deploy Sensor Ping

#### Hero Force Powers
* `C`: Force Crush
* `F`: Force Push
* `L`: Force Lightning
* `H`: Force Heal
* `P`: Force Protect

---

## 5. Input Control Reference Tables

### Viewport & Navigation Controls

| Category | Command / Action | Input Binding | Operational Context & Details |
| :--- | :--- | :--- | :--- |
| **Panning** | Edge Scroll / Directional Pan | Mouse Edge / Arrow Keys | Pan camera across tactical map plane |
| **Zoom** | Elevation Zoom | Scroll Wheel / Keypad 8 & 2 | Elevation control; planetary transition on GC map |
| **Rotation** | Horizontal Axis Step Turn | Keypad 4 / Keypad 6 | Fixed step rotation left/right |
| **Rotation** | Orbital Drag Pitch/Yaw | Hold MMB + Drag | Drag mouse for dynamic camera pitch & yaw |
| **Rotation** | Dual Button 360° Rotation | Hold LMB + RMB + Drag | High-sensitivity 360° rotation (menu toggle) |
| **Camera Focus** | Unit Center Lock | Select Unit + `C` | Lock camera to unit's center of mass |
| **Camera Reset** | Reset Isometric View | Keypad 5 / Double-Click MMB | Snap-reset view to default north-facing isometric |
| **UI Toggle** | HUD Suppression Mode | `I` Key | Toggle visibility of all UI frames, HUD, status rings |
| **UI Toggle** | Cinematic Camera | Clapperboard UI Icon | Auto-track active engagements and dogfights |
| **Galactic Map** | Planet Focus Step | `D` (Prev) / `F` (Next) | Cycle camera focus through controlled planets |

### Unit Selection & Grouping Controls

| Selection Logic | Function | Input Binding | Selection Filter Behavior |
| :--- | :--- | :--- | :--- |
| **Single Unit** | Primary Focus | Left Mouse Button (LMB) | Select single friendly unit or structure |
| **Bounding Box** | Area Drag Select | Hold LMB + Drag Box | Select all friendly units inside screen box (excludes hostiles) |
| **Additive** | Append Selection | `Shift` + LMB / Drag | Add targeted unit/group to current selection set |
| **Global Select** | Select All Operational Units | `Ctrl + A` | Select every active friendly unit on the map |
| **Screen Select** | Select Matching Type on Screen | `Ctrl + Q` | Select all visible units matching highlighted type |
| **Type Match** | Exact Identity Match | `Ctrl + LMB` / Double-Click | Select all visible units of exact identical type |
| **Class Match** | Broad Archetype Match | `Ctrl + Double-Click` | Select all visible units matching broad class |
| **Group Control** | Assign Control Group | `Ctrl + [0-9]` | Register active selection to numeric group slot |
| **Group Control** | Recall Group / Center Camera | `[0-9]` / Double-Tap `[0-9]` | Recall group (single tap) or center view on midpoint (double tap) |
| **Unit Cycling** | Step Unit Queue | `F` (Next) / `D` (Prev) | Cycle selection focus through unit hierarchy |

### Tactical Vector & Target Controls

| Category | Action | Input Binding | Behavior & Tactical Effect |
| :--- | :--- | :--- | :--- |
| **Movement** | Standard Move | `M` Key or Right-Click | Direct movement without stopping for hostiles |
| **Attack-Move** | Combat Advance | `A` Key or `Ctrl + RMB` | Move to destination and auto-engage enemy hostiles in range |
| **Orientation** | Vector Facing | Hold RMB + Drag Arrow | Set explicit ship heading vector upon destination arrival |
| **Facing Lock** | Lock Heading Angle | Double-Tap `S` after vector | Prevent ship rotation drift after vector placement |
| **Stance** | Guard Escort Screen | `G` Key + RMB Target | Form defensive escort perimeter around target |
| **Interrupt** | Stop Order | `S` Key | Instantly cancel movement, abilities, and targeting |
| **Pathing** | Waypoint Queue | Hold `W` + Click Nodes | Plot multi-node movement path |
| **Hostile Target**| Hardpoint Sub-system Focus | Click Specific Sub-system | Focus fire on hardpoint (engine, shield, hangar, weapon) |
| **Simulation** | Active Tactical Pause | `Spacebar` / `Pause` Key | Pause simulation while queueing orders/vectors/targeting |
