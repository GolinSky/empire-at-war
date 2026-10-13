---
type: "query"
date: "2026-10-12T09:54:09.086528+00:00"
question: "Investigate TooltipUi per-frame ForceRebuildLayoutImmediate, verify it is bad, and propose a solution"
contributor: "graphify"
outcome: "useful"
source_nodes: ["TooltipUi", "TooltipService", "TooltipAnchor"]
---

# Q: Investigate TooltipUi per-frame ForceRebuildLayoutImmediate, verify it is bad, and propose a solution

## Answer

Graph TooltipUi, TooltipService and TooltipAnchor verified against live source and installed uGUI 2.0.0 LayoutRebuilder. Place unconditionally forces a complete four-phase layout traversal. LateUpdate invokes Place every visible frame. TooltipUiController maps AnchorChanged to Place; WorldTooltipPresenter.Tick updates cursor anchors, so cursor motion can cause a second forced layout pass in that frame. Show performs Render (which invokes Place), then explicit Place, then visible LateUpdate; changed content Render also adds a pass. TooltipModel.Refresh skips unchanged content; content refresh interval is 0.1 s. Actual active TooltipUi prefab clone in owned temporary preview scene, loaded settings/icons and sample content, view callback disabled but layout active, warmup32 then 3x300 calls per measurement: simple Place mean0.1412733333ms vs positioning-only0.000961ms; detailed content6stats4matchups2requirements Place0.7385734444ms vs positioning-only0.0018765556ms; simple after detailed with hidden pooled rows Place0.206401ms vs0.0011753333ms. LayoutOnly timings track current Place and all measurements allocated0managedbytes. All45 differential position-and-size checks matched for Cursor/UiRect/HudFixed anchors at four corners and center through simple-rich-simple content size changes, width450UIunits, height138.36 to592.259949 to138.36. Benchmarks exclude Canvas batching, initial creation/text initialization and gameplay workload; demonstrate unnecessary steady CPU, not captured gameplay-spike attribution. Minimum solution: move the one ForceRebuildLayoutImmediate(panel) call from Place into Render after rows/text updates and before Place(_anchor); keep LateUpdate position-only for viewport/Canvas changes. No dirty flag, size cache or model change needed for current fixed-width prefab. MarkLayoutForRebuild alone would defer dimensions needed for immediate edge clamping. Generated proposed patch passes git apply --check and preserves source CRLF. No production source or prefab changes applied. Probe/results/proposed patch in output/TooltipLayoutAudit. Final preview objects0 and current scene isDirtyfalse.

## Outcome

- Signal: useful

## Source Nodes

- TooltipUi
- TooltipService
- TooltipAnchor