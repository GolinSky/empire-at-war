using System.Collections.Generic;
using EmpireAtWar.Entities.Squadrons;
using EmpireAtWar.Mvc;
using UnityEngine;

namespace EmpireAtWar.Models.Factions
{
    /// <summary>All playable factions. A new faction is one <see cref="FactionDefinition"/> added here.</summary>
    [CreateAssetMenu(fileName = nameof(FactionCatalog), menuName = "Data/Factions/FactionCatalog")]
    public class FactionCatalog : Data
    {
        [SerializeField] private FactionDefinition[] factions;

        public IReadOnlyList<FactionDefinition> Factions => factions;

        public FactionDefinition Get(FactionType factionType)
        {
            foreach (FactionDefinition faction in factions)
                if (faction.FactionType == factionType) return faction;
            throw new KeyNotFoundException($"No faction definition for {factionType}.");
        }

        public FactionData GetShipFactionData(ShipType shipType)
        {
            foreach (FactionDefinition faction in factions)
                if (faction.Ships.TryGetValue(shipType, out FactionData data)) return data;
            throw new KeyNotFoundException($"No faction data for ship {shipType}.");
        }

        public FactionData GetSquadronFactionData(SquadronType squadronType)
        {
            foreach (FactionDefinition faction in factions)
                if (faction.Squadrons.TryGetValue(squadronType, out FactionData data)) return data;
            throw new KeyNotFoundException($"No faction data for squadron {squadronType}.");
        }
    }
}
