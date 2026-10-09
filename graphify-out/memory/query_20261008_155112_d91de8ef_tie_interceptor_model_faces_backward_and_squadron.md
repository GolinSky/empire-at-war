---
type: "query"
date: "2026-10-08T15:51:12.254270+00:00"
question: "TIE Interceptor model faces backward and squadron/faction icons are wrong"
contributor: "graphify"
outcome: "useful"
source_nodes: ["SquadronIconComponent"]
---

# Q: TIE Interceptor model faces backward and squadron/faction icons are wrong

## Answer

Expanded graph vocabulary: squadron icon sprite direction rotation view faction interceptor. SquadronIconComponent binds distinct iconImage and silhouetteImage fields. Live prefab inspection confirmed iconImage still used TIEFighterIcon while the faction and selected-unit mappings already used TIEInterceptorIcon. Geometry had an incorrect 180-degree Y rotation. Corrected geometry to +Z, recentered it, rebound all 32 laser mounts on eight fighters, regenerated front-facing icon/silhouette and assigned the HUD image. Unity readback verified all eight fighters, muzzle anchors, eight inherited placement models, and both faction/selected-unit mappings. Updated ISDI builder orientation and registration to preserve the fix; both scripts compile. No console errors.

## Outcome

- Signal: useful

## Source Nodes

- SquadronIconComponent