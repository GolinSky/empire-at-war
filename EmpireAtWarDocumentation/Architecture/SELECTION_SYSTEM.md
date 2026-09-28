# Selection System

- Supports selection and commands for multiple player ships.
- Preserves the single-selection API for older consumers.

## Player Features

- **Single selection:** click or tap a selectable unit.
- **Ship-type selection:** activate the same player ship twice to select every player-owned ship of that `ShipType`.
- **Marquee selection:** drag the mouse more than 5 screen pixels to draw a selection rectangle.
  - Releasing selects all visible player units whose world positions project inside the rectangle.
- **Group movement:** the ship UI move command is sent to every selected entity that provides an `IMoveCommand`.
- **Safe updates:** replacing a selection deselects removed units, avoids duplicates, and automatically removes destroyed entities.

- Mouse drag → marquee selection.
- Touch drag → camera pan.
- Input starting over UI → no selection.

## Design

- Marquee feature: `Assets/Scripts/Components/Selection/Marquee`.
- Responsibilities: MVP.

- `MarqueeSelectionModel` owns drag state and normalized rectangle data without Unity dependencies.
- `IMarqueeSelectionView` / `MarqueeSelectionView` render the box only.
- `MarqueeSelectionPresenter` translates input drag events into model and view updates.
- `MarqueeSelectionUtility` is a generic, pure C# bounding-box filter that can be reused by other systems.

- `SelectionQuery` owns selection world queries.
- `SelectionService` applies click, repeated-tap, and marquee policies.
- `SelectionContext`: `Entities` → full selection.
- `Entity` → first entity; backward-compatible primary selection.

## Verification

- Coverage: rectangle normalization, boundary inclusion, reverse dragging, model lifecycle.
- File: `Assets/Scripts/Tests/Editor/MarqueeSelectionUtilityTests.cs`.
