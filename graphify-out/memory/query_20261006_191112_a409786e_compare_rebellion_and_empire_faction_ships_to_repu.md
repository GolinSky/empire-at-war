---
type: "query"
date: "2026-10-06T19:11:12.573652+00:00"
question: "compare rebellion and empire faction ships to republic/separatist (cis) price stats build duration"
contributor: "graphify"
outcome: "useful"
source_nodes: ["ShipData", "FactionDefinition", "ProductionQueueItem"]
---

# Q: compare rebellion and empire faction ships to republic/separatist (cis) price stats build duration

## Answer

Expanded query vocabulary: ShipData, FactionDefinition, Price, BuildTime, Hull, Shields, Speed. Verified against live faction catalogs and ShipsData GUID-linked assets. 31 buildable ships and 18 squadron entries. MC80 costs 4500 credits, takes 30 s, has 8500 hull and 2500 shields; Venator is 5800/55s/5000+3000, Providence is 6000/60s/5000+3000. Imperial II is 25000/500s/21000+18000; Imperial III is 26000/520s/28000+18000. Shield recovery is periodic: average rate is ShipData ShieldRegenerateValue divided by ShieldRegenerateDelay, so Lucrehulk is 198 HP every 3s = 66 HP/s. Victory I Advanced and Imperial I Advanced tooltip hull, shield and speed numbers disagree with linked assets. No gameplay data changed. Interactive comparison saved outside Assets in the current chat visualization directory.

## Outcome

- Signal: useful

## Source Nodes

- ShipData
- FactionDefinition
- ProductionQueueItem