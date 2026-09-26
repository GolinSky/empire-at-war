---
type: "query"
date: "2026-09-26T14:49:12.302957+00:00"
question: "Trace an intermittent Invalid AABB canvas error during combat after spawning test ships."
contributor: "graphify"
outcome: "useful"
source_nodes: ["HealthOverlayView", "HealthOverlayPresenter"]
---

# Q: Trace an intermittent Invalid AABB canvas error during combat after spawning test ships.

## Answer

Expanded graph vocabulary: health overlay spawn canvas. Graph led to health and UI mesh paths; live source identified MPImageHelper.GetDrawingDimensions dividing zero sprite padding by rounded sprite-free rect dimensions. Widths or heights from 0 to 0.5 exclusive round to zero and yield NaN. CaptureSiteView.Render changes the MPImage ProgressFill rect width through anchorMax, so small nonzero capture or construction progress reaches this path. Fixed sprite-free dimensions to return pixel-adjusted rect bounds directly. Original log does not identify the emitting graphic; exact combat incident not reproduced. Added unrun MPImageMeshTests; static diagnostics clean, Unity compilation blocked by concurrent unrelated hardpoint-order test-double interface errors.

## Outcome

- Signal: useful

## Source Nodes

- HealthOverlayView
- HealthOverlayPresenter