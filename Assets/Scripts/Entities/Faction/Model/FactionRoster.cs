using System.Collections.Generic;
using EmpireAtWar.Entities.DefendPlatform;
using EmpireAtWar.Entities.MiningFacility;
using EmpireAtWar.Entities.Squadrons;
using EmpireAtWar.Entities.SuperWeapons;

namespace EmpireAtWar.Models.Factions
{
    /// <summary>What one player's faction can build, with the shared structures resolved to purchase data.</summary>
    public sealed class FactionRoster
    {
        public FactionDefinition Definition { get; }
        public FactionType FactionType => Definition.FactionType;
        public Dictionary<ShipType, FactionData> Ships => Definition.Ships;
        public Dictionary<SquadronType, FactionData> Squadrons => Definition.Squadrons;
        public Dictionary<ResearchType, ResearchLineData> ResearchLines => Definition.ResearchLines;
        public Dictionary<MiningFacilityType, FactionData> MiningFacilities { get; } = new();
        public Dictionary<DefendPlatformType, FactionData> DefendPlatforms { get; } = new();
        public Dictionary<SuperWeaponType, FactionData> SuperWeapons { get; } = new();

        public FactionRoster(
            FactionDefinition definition,
            MiningFacilityCatalog miningFacilities,
            DefendPlatformCatalog defendPlatforms,
            SuperWeaponCatalog superWeapons)
        {
            Definition = definition;
            foreach (MiningFacilityType type in definition.MiningFacilities)
                MiningFacilities.Add(type, miningFacilities.Get(type));
            foreach (DefendPlatformType type in definition.DefendPlatforms)
                DefendPlatforms.Add(type, defendPlatforms.Get(type));
            foreach (SuperWeaponType type in definition.SuperWeapons)
                SuperWeapons.Add(type, superWeapons.Get(type));
        }
    }
}
