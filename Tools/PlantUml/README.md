# PlantUML Renderer

Local, offline PlantUML for the `diagram_*` Unity CLI commands. No diagram text leaves the machine.

## Pinned Version

| Item | Value |
|---|---|
| Version | `1.2026.10` (GitHub release 2026-11-11) |
| Asset | `plantuml-mit-1.2026.10.jar` (MIT build) |
| SHA-256 | `30a66b81a16127fd3ec560992f31964fe9ae3f1233c614ef4558a4558a21489e` |
| Installed as | `Tools/PlantUml/bin/plantuml.jar` (gitignored) |
| Needs | Java 11+ on `PATH` (machine: Temurin 17) |

## Install

```powershell
pwsh Tools/PlantUml/Install-PlantUml.ps1
```

- Idempotent: exits early when the installed jar matches the pinned hash. `-Force` reinstalls.
- Hash mismatch → the download is deleted and the script throws.

## Verify

```powershell
java -jar Tools/PlantUml/bin/plantuml.jar -testdot
java -jar Tools/PlantUml/bin/plantuml.jar -tsvg path/to/diagram.puml
```

- `-testdot` reports OK on this machine (Graphviz layout available through the jar).
- Several `-t` flags in one call → only the last format is written. Render SVG and PNG in two calls.
- No `@startuml` block → exit code `100`, `No diagram found`.
- If `-testdot` ever fails → add `-Playout=smetana` (pure-Java layout, no Graphviz).

## Update

1. Pick a release on `https://github.com/plantuml/plantuml/releases`.
2. Copy the `plantuml-mit-<version>.jar` SHA-256 from the release asset list (`digest`).
3. Update `$VERSION` and `$SHA256` in `Install-PlantUml.ps1` and the table above.
4. Run `Install-PlantUml.ps1 -Force`, then `-testdot`.

## Usage From Unity

- Commands: `diagram_generate`, `diagram_render`, `diagram_open` (`Assets/Scripts/Editor/Diagrams/DiagramCliCommands.cs`).
- Output: `Logs/Diagrams/<name>/<name>.puml|svg|png` (gitignored, outside `Assets`).
- Preview window: `Tools/Empire At War/Diagrams/Diagram Preview`.
- Public `plantuml.com` server is never used by these commands.
