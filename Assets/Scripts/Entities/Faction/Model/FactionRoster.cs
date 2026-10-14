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
            IReadOnlyList<MiningFacilityCatalog> miningFacilities,
            DefendPlatformCatalog defendPlatforms,
            IReadOnlyList<SuperWeaponCatalog> superWeapons,
            EmpireAtWar.Entities.Game.BattleFeatures features = EmpireAtWar.Entities.Game.BattleFeatures.All)
        {
            Definition = definition;
            if ((features & EmpireAtWar.Entities.Game.BattleFeatures.MiningSites) != 0)
                foreach (MiningFacilityCatalog catalog in miningFacilities)
                    foreach (MiningFacilityType type in definition.MiningFacilities)
                        MiningFacilities.Add(type, catalog.Get(type));
            foreach (DefendPlatformType type in definition.DefendPlatforms)
                DefendPlatforms.Add(type, defendPlatforms.Get(type));
            if ((features & EmpireAtWar.Entities.Game.BattleFeatures.SuperWeapons) != 0)
            foreach (SuperWeaponCatalog catalog in superWeapons)
                foreach (SuperWeaponType type in definition.SuperWeapons)
                    SuperWeapons.Add(type, catalog.Get(type));
        }
    }
}
