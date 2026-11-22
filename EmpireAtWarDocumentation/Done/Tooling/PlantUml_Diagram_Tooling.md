---
category: Tooling
status: done
created: 2026-11-21
completed: 2026-11-22
acceptance: pending-user
tags:
  - tooling
  - diagrams
  - agents
---
# PlantUML Diagram Tooling

## Goal

- Agents generate and render UML from the CLI: existing code, a proposed system, or a change preview.
- User views the result in Rider, in a Unity Editor window, or in a web browser.
- Vendor package lives in `Assets/ThirdParty`; diagram output lives outside `Assets` and never reaches a build.
- Research and sources → [[Done/Tooling/PlantUml_Diagram_Tooling - Research|Research]].

## Outcome (2026-11-22)

- All 6 phases implemented. D1–D5 confirmed by the user's "read and execute" request (2026-11-22).
- Working guide → [[Tooling/Diagrams]]; agent rules → `AGENTS.md` `## Diagrams`.
- Verification: Unity recompile 0 errors; `diagram_generate` on `Assets/Scripts/Editor/Diagrams` → `.puml` + SVG + PNG (7 scripts, 4 types, 3 parser warnings); PNG read back and correct.
- Failure paths: missing path → exit `6`; bad name `../x` → exit `2`; `.puml` without `@startuml` → exit `6` (PlantUML exit `100`); `diagram_open` unknown name → exit `6`.
- `diagram_open --target editor` → `DiagramPreviewWindow` loaded the PNG (checked via `script eval`).
- Rider target resolves `CodeEditor.CurrentEditorInstallation` = Rider 2026.1.2; Rider and browser launch not exercised (user gate).
- Tests: `DiagramToolingTests` (`Core`) added; not run (local override).
- Reusable guidance extracted → [[Tooling/Diagrams]], `Tools/PlantUml/README.md`.

## Current State (verified 2026-11-21, before work)

- Package: `Assets/ClassDiagramGenerator`, Asset Store "Class Diagram Generator" 2.3.1 (JulesTools), 25 C# files, 2441 lines.
- Assembly: `ClassDiagramGenerator.Editor`, Editor-only, `autoReferenced: false`.
- Entry point: `DiagramGenerationService.Generate(GenerateDiagramRequest)` → `PlantUmlText`. Pure C#, no Unity types.
- Output: `Assets/ClassDiagramGeneratorOutput/ClassDiagram.puml` + `.meta`, both tracked in git.
- `Assets/ClassDiagramGenerator/Resources/` held 3 PNGs → shipped in the player build.
- Machine: Java 17 on `PATH`; no `plantuml`; no Graphviz `dot` on `PATH` (yet `-testdot` passes with the jar).

## Decisions

- **D1 Output folder** — `Logs/Diagrams/<name>/`. Avoid: `Assets/...`, a new root folder.
- **D2 Renderer** — local `plantuml.jar` (MIT build) with installed Java. Avoid: public server.
- **D3 Jar storage** — `Tools/PlantUml/Install-PlantUml.ps1` → gitignored `Tools/PlantUml/bin/plantuml.jar`, SHA-256 verified. Pinned `plantuml-mit-1.2026.10.jar` (GitHub release 2026-11-11), SHA-256 `30a66b81…1489e`.
- **D4 In-Editor preview** — `EditorWindow` (UI Toolkit) showing the rendered PNG.
- **D5 Assembly access** — `autoReferenced: true` on `ClassDiagramGenerator.Editor.asmdef`.
- **D6 Window default output** (added) — vendor window writes only under `Assets/` → defaults changed to `Assets/Sandbox/Diagrams/...`.

## Implementation

### Phase 1 — Move the package

- [x] `Assets/ClassDiagramGenerator` → `Assets/ThirdParty/ClassDiagramGenerator` via `unity command asset move` (GUIDs kept).
- [x] `Resources/` → `Editor/Resources/`.
- [x] Deleted `Assets/ClassDiagramGeneratorOutput/`.
- [x] `autoReferenced: true`.
- [x] Window defaults → `Assets/Sandbox/Diagrams/ClassDiagram.puml`, `Assets/Sandbox/Diagrams/GeneratedScripts`.
- [x] `THIRD_PARTY.md`: source, 2.3.1, MIT per README, local changes.
- [x] Verified: compile clean; no `ClassDiagram*` path under `Assets/` root. Window icons not opened visually (user gate).

### Phase 2 — Renderer

- [x] `Tools/PlantUml/Install-PlantUml.ps1` + `README.md`.
- [x] `/Tools/PlantUml/bin/` in `.gitignore`.
- [x] `-testdot` → OK; `-tsvg` and `-tpng` each render. Several `-t` flags in one call → only the last format.
- [x] Smetana fallback not needed.

### Phase 3 — CLI adapters

- [x] `Assets/Scripts/Editor/Diagrams/`: `DiagramSourceCollector`, `PlantUmlRenderer`, `DiagramOutputPaths`, `DiagramOpener`, `DiagramOpenTarget`, `DiagramCliCommands`.
- [x] `diagram_generate`, `diagram_render`, `diagram_open` registered (group `global`); real runs + failure exit codes checked.
- Rider target uses `CodeEditor.OSOpenFile(CurrentEditorInstallation, path)` — Rider's `OpenProject` rejects non-script extensions.
- `[MenuItem]` + `[EditorToolInfo]` live on `DiagramPreviewWindow` (Editor Hub registration).

### Phase 4 — In-Editor preview window

- [x] `DiagramPreviewWindow`: list of `Logs/Diagrams/*`, PNG, zoom (slider / Ctrl+wheel), drag pan, `Refresh` / `Open in Rider` / `Open in Browser` / `Reveal`.
- [x] Menu `Tools/Empire At War/Workflow/Diagrams/Diagram Preview`, Editor Hub via `[EditorToolInfo]`.
- [x] Read `UI_UX_GUIDELINES` (game uGUI rules, not applicable to Editor windows) and `ui-imgui` skill; chose UI Toolkit to match `EditorHubWindow`, `BalanceEditorWindow`.

### Phase 5 — Agent workflow and docs

- [x] `AGENTS.md` `## Diagrams`.
- [x] Vault [[Tooling/Diagrams]] + link from [[Tooling/00_Tooling]].
- [x] Change convention `<<new>>`/`<<changed>>`/`<<removed>>` documented.
- [x] Change preview flow and PNG self-check documented.

### Phase 6 — Tests

- [x] Read `Rules/TEST_GUIDELINES`.
- [x] `Assets/Scripts/Tests/Editor/Core/DiagramToolingTests.cs` (`Core`): folder expansion, dedupe, missing path, non-script, empty folder, split, output paths, unsafe names, open-target parsing.
- [x] No test launches Java or a browser (temp folder only).
- [x] `Tools/Jenkins/TEST_INVENTORY.md` updated (313 fixtures, Core 27). Tests not run (local override).

## Edge Cases

- Regex parser misses partial classes, nested types, enums, and some files with escaped quotes / `'\\'` char literals → reported in `warnings`.
- `typeCount` > ~40 → narrow `--paths`.
- Java or jar missing → explicit error with install command; no public-server fallback.
- `Logs/Diagrams` invisible in Rider's Unity view → open by path or `File System` view.

## Files

- `Assets/ThirdParty/ClassDiagramGenerator/` (moved), `THIRD_PARTY.md`
- `Assets/Scripts/Editor/Diagrams/*.cs`
- `Assets/Scripts/Tests/Editor/Core/DiagramToolingTests.cs`
- `Tools/PlantUml/Install-PlantUml.ps1`, `Tools/PlantUml/README.md`
- `.gitignore`, `AGENTS.md`, `Tools/Jenkins/TEST_INVENTORY.md`
- Vault: [[Tooling/Diagrams]], [[Tooling/00_Tooling]]

## Acceptance (user)

- [ ] Diagram opens in Rider (plantuml4idea preview), in the Editor window, and in the browser; vendor window icons still load.
- [ ] Run `Core` EditMode category when authorized.
