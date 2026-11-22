---
category: Tooling
status: done
created: 2026-11-21
tags:
  - tooling
  - diagrams
  - research
completed: 2026-11-22
---
# PlantUML Diagram Tooling - Research

Plan → [[Done/Tooling/PlantUml_Diagram_Tooling|PlantUML Diagram Tooling]]. Web research dated 2026-11-21.

## Package

- Asset Store: "Class Diagram Generator" id `323124`, version 2.3.1 (2026-05-04), free, JulesTools.
- Docs: `https://julestools.gitbook.io/julestools-docs/documentation/tools/class-diagram-generator`.
- Features: C# → PlantUML, PlantUML → C# skeleton, export as `.puml` or public-server URL.
- Layers: `Application` (services, requests), `Domain/Models`, `Infrastructure` (regex parser, exporter, importer, skeleton generator, URL encoder), `Editor` (window).
- Reusable without the window:
  ```csharp
  var service = new DiagramGenerationService(new RegexClassDiagramParser(), new PlantUmlExporter());
  GenerateDiagramResult result = service.Generate(new GenerateDiagramRequest
  {
      SelectedScriptPaths = paths,
      IncludeAssociations = true
  });
  ```
- `PlantUmlUrlEncoder.ConvertTextToUrl` → `https://www.plantuml.com/plantuml/uml/<deflate>`; sends the diagram text to a public server.

## Rendering

| Option | Needs | Verdict |
|---|---|---|
| Local `plantuml.jar` | Java (installed: 17) | Chosen |
| Public `plantuml.com` URL | Internet | Opt-in only; leaks type names |
| `plantuml.jar -picoweb:8000:127.0.0.1` | Java, running process | Not needed; a static SVG file opens in a browser without a server |
| Kroki / Docker server | Docker | Too heavy |

- Graphviz: class diagrams normally need `dot`. Not installed here.
- Confirmed (PlantUML docs): `!pragma layout smetana` or `-Playout=smetana` renders without Graphviz; available since 1.2021.5; marked experimental.
- Confirmed (PlantUML docs): ELK layout ships inside the jar since 1.2024.6.
- Likely, not verified: Windows jars embed a minimal Graphviz. Check with `java -jar plantuml.jar -testdot`.
- Likely, not verified: `-tsvg` and `-tpng` combine with `-Playout=smetana`.
- Version: news page lists `V1.2026.9` (2026-10-17); download page still says `v1.2026.6`. GitHub latest (checked 2026-11-22): `v1.2026.10` (2026-11-11), asset `plantuml-mit-1.2026.10.jar` → pinned.
- Verified 2026-11-22: `-testdot` OK with the jar (no `dot` on `PATH`); multiple `-t` flags → only the last format written.
- License: download page offers GPL, LGPL, Apache, EPL and MIT jars. Use the MIT jar.
- Java requirement: 11+ (secondary source: FreeBSD port note).

## Viewing

- **Rider**: `plantuml4idea` found in `%APPDATA%/JetBrains/Rider2026.1/plugins`. Opens `.puml` with a live preview pane. It bundles its own PlantUML, so viewing needs no project jar.
- **Browser**: open the rendered `.svg` with `Application.OpenURL("file:///...")`. Zoom and text search work natively.
- **Unity Editor**:
  - Unity `6000.4.7f1` has the built-in `com.unity.modules.vectorgraphics` module (SVG → UI Toolkit `VectorImage` / `Texture2D`).
  - Known issues: SVG preview and artifact bugs open on 6000.3–6000.6. Likely, not verified: weak `<text>` support.
  - Decision → render PNG with PlantUML, load with `Texture2D.LoadImage`.
- **Obsidian**: needs a community plugin → Obsidian configuration change, out of scope.

## Build Exclusion

- `Assets/**/Resources/` always ships in the player. `Editor/Resources/` loads through `Resources.Load` in the Editor and is stripped from builds.
- Folders outside `Assets/` are never imported or built.
- `.gitignore:7` `[Ll]ogs/` already covers `Logs/Diagrams`.
- `includePlatforms: ["Editor"]` keeps the package code out of the player.

## CLI Pattern

- Existing example: `Assets/Scripts/Editor/BattleAttributionRun.cs` — `[CliCommand("battle_attribution_run", "...")] public static object Start()`.
- Commands land in group `global`; return an anonymous object; throw on failure → exit code `6`.
- `Assets/Scripts/Editor` has no asmdef → `Assembly-CSharp-Editor`.

## Sources

- https://plantuml.com/smetana02
- https://plantuml.com/layout-engines
- https://plantuml.com/graphviz-dot
- https://plantuml.com/elk
- https://plantuml.com/picoweb
- https://plantuml.com/download
- https://plantuml.com/news
- https://github.com/plantuml/plantuml
- https://assetstore.unity.com/packages/tools/utilities/class-diagram-generator-323124
- https://julestools.gitbook.io/julestools-docs/documentation/tools/class-diagram-generator
- https://blog.jetbrains.com/dotnet/2020/10/06/create-uml-diagrams-using-plantuml/
- https://docs.unity3d.com/Packages/com.unity.vectorgraphics@3.0/manual/index.html
- https://issuetracker.unity3d.com/issues/svgs-imported-as-vector-image-dont-show-a-static-preview-in-the-project-browser
