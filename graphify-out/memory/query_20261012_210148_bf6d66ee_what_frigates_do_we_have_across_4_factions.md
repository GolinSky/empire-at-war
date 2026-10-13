---
type: "query"
date: "2026-10-12T21:01:48.423623+00:00"
question: "what frigates do we have across 4 factions"
contributor: "graphify"
outcome: "useful"
source_nodes: ["Frigate", "ShipData", "WeaponsData"]
---

# Q: what frigates do we have across 4 factions

## Answer

Expanded vocabulary: Frigate ShipClass ShipData WeaponsData WeaponCaliber FactionDefinition. Live serialized assets verify 11 class-3 frigates: Republic Acclamator and Arquitens; Empire AcclamatorAssault and ArquitensImperialCruiser; Rebellion NebulonB, MC30c, QuasarFire, AssaultFrigateMkII; Separatist Munificent, Captor, Dispatcher. PatrolFrigate is class-2 Corvette; Recusant and HeavyDreadnought are class-8 Cruiser. Weapon calibers must be read from WeaponsData definitions rather than inferred from enum names. ShieldRegenerateValue is per ShieldRegenerateDelay interval, not per second. Ship balance is unmodified.

## Outcome

- Signal: useful

## Source Nodes

- Frigate
- ShipData
- WeaponsData