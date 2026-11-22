---
type: reference
system:
  - tooling
tags:
  - tooling
  - diagrams
  - agents
updated: 2026-11-22
---
# Diagrams

## Rules

- Agents draw UML for existing structure, proposed systems and change previews across 3+ types.
- Render locally only: `Tools/PlantUml/bin/plantuml.jar` (MIT `1.2026.10`). Never use the public `plantuml.com` server (leaks type names).
- Output: `Logs/Diagrams/<name>/<name>.puml|svg|png`. Outside `Assets` → no import, no `.meta`, gitignored, never in a build.
- Keeping a diagram: copy the `.puml` beside its vault note by hand.
- Read the PNG before showing a diagram to the user.

## Commands

| Command | Parameters | Result |
|---|---|---|
| `diagram_generate` | `--paths`, `--name`, `--associations` (true), `--open` (none) | code → `.puml` → SVG + PNG; returns paths, `scriptCount`, `typeCount`, `warnings` |
| `diagram_render` | `--puml`, `--open` (none) | renders any `.puml`; a file outside `Logs/Diagrams/<file name>/` is copied there first |
| `diagram_open` | `--name`, `--target` (editor) | reopens a rendered diagram |

- `--paths`: folders or `.cs` files, separated by `,` or `;`, relative to the project root.
- `--name`: `[A-Za-z0-9_-]+`; anything else → exit `2`.
- Targets: `none`, `rider` (configured external editor + `plantuml4idea` preview), `editor` (`DiagramPreviewWindow`), `browser` (`file:///…svg`).
- Example: `unity command diagram_generate --paths "Assets/Scripts/Services/Enemy" --name enemy_ai --open rider`.
- Preview window: `Tools/Empire At War/Workflow/Diagrams/Diagram Preview` (also in Editor Hub). Ctrl+wheel or slider zooms; drag pans.

## Change Preview Flow

1. `diagram_generate` on the affected folder.
2. Copy the `.puml` to a new name; mark types:
   - new → `class Foo <<new>> #palegreen`
   - changed → `<<changed>> #orange`
   - removed → `<<removed>> #lightgray`
3. `diagram_render --puml <copy> --open rider`.
4. Read the PNG; then show it.

## Edge Cases

- Parser is the vendor regex parser (`Assets/ThirdParty/ClassDiagramGenerator`), one file at a time → partial classes, nested types and enums are missing; label diagrams approximate (the generated `.puml` starts with that note).
- Some files are skipped with `No class/interface found` (seen: files with escaped quotes or `'\\'` char literals) → listed in `warnings`; add those types by hand if needed.
- `typeCount` > ~40 → unreadable image; narrow `--paths`.
- Missing path → exit `6` `Diagram source path not found`. Jar missing → exit `6` with the install command. Java missing → exit `6` `Java is not on PATH`.
- `.puml` without `@startuml` → PlantUML exit `100` → command exit `6`.
- PlantUML writes only the last `-t` format per call → renderer calls Java twice (SVG, PNG).
- `-testdot` passes on this machine; if it fails, add `-Playout=smetana`.
- `Logs/Diagrams` is hidden in Rider's Unity view → files still open by path; use the `File System` explorer view.

## Files

- `Assets/Scripts/Editor/Diagrams/` — `DiagramCliCommands`, `DiagramSourceCollector`, `DiagramOutputPaths`, `PlantUmlRenderer`, `DiagramOpener`, `DiagramOpenTarget`, `DiagramPreviewWindow`.
- `Assets/ThirdParty/ClassDiagramGenerator/` — vendor package 2.3.1, MIT; `THIRD_PARTY.md`.
- `Tools/PlantUml/Install-PlantUml.ps1`, `Tools/PlantUml/README.md` — pinned jar, SHA-256, update steps.
- Tests: `Assets/Scripts/Tests/Editor/Core/DiagramToolingTests.cs` (`Core`).
- Plan: [[Done/Tooling/PlantUml_Diagram_Tooling|PlantUML Diagram Tooling]].
