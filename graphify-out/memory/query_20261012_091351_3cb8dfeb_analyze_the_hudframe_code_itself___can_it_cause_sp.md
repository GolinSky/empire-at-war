---
type: "query"
date: "2026-10-12T09:13:51.366875+00:00"
question: "analyze the HudFrame code itself - can it cause spikes - be cpu heavy"
contributor: "graphify"
outcome: "useful"
source_nodes: ["HudFrame"]
---

# Q: analyze the HudFrame code itself - can it cause spikes - be cpu heavy

## Answer

Graph labels HudFrame and its OnPopulateMesh/Ring/Polygon/Point methods verified against live source and installed com.unity.ugui 2.0.0 source. Normal mesh: 50 vertices, 48 triangles, fixed loops. No Update/LateUpdate or self-dirtying. Rect, Vector3, Vector2 and Color constructions are structs. Graphic retains a static VertexHelper; lists are pooled and cleared without releasing capacity. Isolated Unity Editor benchmark on actual loaded OnPopulateMesh using a bound delegate, cached active non-pixel-perfect screen-space Canvas, inactive 400x200 frame, warmup 256 then 3 samples of 5000 calls per mode: populate mean 0.00954062 ms, sample means 0.00946362 to 0.00967618 ms, 0 managed allocated bytes across 15000 calls; populate plus VertexHelper.FillMesh mean 0.0136771533 ms, sample means 0.01365816 to 0.01370076 ms, 0 managed allocated bytes across 15000 calls. These exclude full Canvas layout, batching, CanvasRenderer.SetMesh, material/clipping and gameplay workload. Low direct spike risk; frequent resizing/color changes/enable transitions or many cloned faction cards can scale cost and cause larger UI work. Tiny inefficiencies: transparent overlays still build fill (9 vertices/8 triangles), accent rectangle uses polygon fan (9 vertices/8 triangles rather than a quad), duplicate GetPixelAdjustedRect. Stronger related concern: TooltipUi.cs:L83 LateUpdate calls Place each visible frame; Place at L65 unconditionally calls ForceRebuildLayoutImmediate(panel), so unchanged tooltip content still incurs layout traversal. No source/prefab changes. First benchmark failed before measurement due overloaded reflection method; temporary object and owned preview scene were cleaned, typed lookup succeeded, final temporary object count 0 and MainMenuScene isDirty false. No gameplay spike attribution was measured.

## Outcome

- Signal: useful

## Source Nodes

- HudFrame