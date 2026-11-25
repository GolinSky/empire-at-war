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

        public FactionData GetShipFactionData(ShipType shipType) =>
            TryGetShipFactionData(shipType, out FactionData data)
                ? data
                : throw new KeyNotFoundException($"No faction data for ship {shipType}.");

        /// <summary>False for a ship no roster builds, such as one retired from the game but still in old saves.</summary>
        public bool TryGetShipFactionData(ShipType shipType, out FactionData data)
        {
            foreach (FactionDefinition faction in factions)
                if (faction.Ships.TryGetValue(shipType, out data)) return true;
            data = null;
            return false;
        }

        /// <summary>
        /// The faction this ship belongs to: <paramref name="owner"/> when its roster builds the ship
        /// (ships such as the Tector sit in several rosters), otherwise the first faction that builds it.
        /// </summary>
        public FactionType GetShipFaction(ShipType shipType, FactionType owner)
        {
            if (Get(owner).Ships.ContainsKey(shipType)) return owner;
            foreach (FactionDefinition faction in factions)
                if (faction.Ships.ContainsKey(shipType)) return faction.FactionType;
            throw new KeyNotFoundException($"No faction builds ship {shipType}.");
        }

        /// <summary>The faction this squadron belongs to; same rule as <see cref="GetShipFaction"/>.</summary>
        public FactionType GetSquadronFaction(SquadronType squadronType, FactionType owner)
        {
            if (Get(owner).Squadrons.ContainsKey(squadronType)) return owner;
            foreach (FactionDefinition faction in factions)
                if (faction.Squadrons.ContainsKey(squadronType)) return faction.FactionType;
            throw new KeyNotFoundException($"No faction fields squadron {squadronType}.");
        }

        public FactionData GetSquadronFactionData(SquadronType squadronType) =>
            TryGetSquadronFactionData(squadronType, out FactionData data)
                ? data
                : throw new KeyNotFoundException($"No faction data for squadron {squadronType}.");

        /// <summary>False for a squadron no roster fields; same rule as <see cref="TryGetShipFactionData"/>.</summary>
        public bool TryGetSquadronFactionData(SquadronType squadronType, out FactionData data)
        {
            foreach (FactionDefinition faction in factions)
                if (faction.Squadrons.TryGetValue(squadronType, out data)) return true;
            data = null;
            return false;
        }
    }
}
