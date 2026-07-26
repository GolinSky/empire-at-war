---
type: "query"
date: "2026-07-26T10:28:56.863763+00:00"
question: "How should ReinforcementZonesSystem reference map zone views for Coruscant and Kamino?"
contributor: "graphify"
---

# Q: How should ReinforcementZonesSystem reference map zone views for Coruscant and Kamino?

## Answer

ReinforcementZonesSystem now consumes an explicit serialized ReinforcementZoneView array. Coruscant and Kamino each use a dedicated ReinforcementZones root prefab that owns four assigned zone views, and each scene references its matching prefab.