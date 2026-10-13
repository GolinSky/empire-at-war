---
type: "query"
date: "2026-10-12T07:13:12.568278+00:00"
question: "HudFrame is utility? if so - move it to utility; also check what this does"
contributor: "graphify"
outcome: "useful"
source_nodes: ["HudFrame"]
---

# Q: HudFrame is utility? if so - move it to utility; also check what this does

## Answer

Queried the exact graph label HudFrame. Graph evidence: Assets/Scripts/Components/Ui/HudFrame.cs:L6 inherits MaskableGraphic; OnPopulateMesh at L15 calls Polygon, Ring and Inset. Verified against live source and Assets/Scripts/Utils/README.md: HudFrame is a reusable UI rendering component, so it remains under Components/Ui, matching the placement of SafeAreaHelper and DrawCircle. It generates a filled beveled panel, two 1 px borders (inner border inset 4 px with 60 percent alpha), and a 2 px top accent, default width 80 px and bevel 10 px. Live prefab GUID search finds 29 components across 10 UI prefabs. No C# callers were found. No gameplay logic and no source or prefab changes.

## Outcome

- Signal: useful

## Source Nodes

- HudFrame