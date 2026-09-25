using EmpireAtWar.Entities.DefendPlatform;
using EmpireAtWar.Entities.MiningFacility;
using EmpireAtWar.Entities.Squadrons;
using EmpireAtWar.Entities.SuperWeapons;
using EmpireAtWar.Models.Factions;

namespace EmpireAtWar.Controllers.Factions
{
    public interface IUnitRequestFactory
    {
        ShipUnitRequest ConstructUnitRequest(FactionData factionData, ShipType shipType);
        SquadronUnitRequest ConstructUnitRequest(FactionData factionData, SquadronType squadronType);
        LevelUnitRequest ConstructUnitRequest(FactionData factionData, int level);
        MiningFacilityUnitRequest ConstructUnitRequest(FactionData factionData, MiningFacilityType miningFacilityType);
        DefendPlatformUnitRequest ConstructUnitRequest(FactionData factionData, DefendPlatformType platformType);
        ResearchUnitRequest ConstructUnitRequest(FactionData factionData, ResearchType researchType);
        SuperWeaponUnitRequest ConstructUnitRequest(FactionData factionData, SuperWeaponType superWeaponType);
    }

    public class UnitRequestFactory : IUnitRequestFactory
    {
        public ShipUnitRequest ConstructUnitRequest(FactionData factionData, ShipType shipType)
        {
            return new ShipUnitRequest(factionData, shipType);
        }

        public SquadronUnitRequest ConstructUnitRequest(FactionData factionData, SquadronType squadronType)
        {
            return new SquadronUnitRequest(factionData, squadronType);
        }

        public LevelUnitRequest ConstructUnitRequest(FactionData factionData, int level)
        {
            return new LevelUnitRequest(factionData, level);
        }

        public MiningFacilityUnitRequest ConstructUnitRequest(FactionData factionData, MiningFacilityType miningFacilityType)
        {
            return new MiningFacilityUnitRequest(factionData, miningFacilityType);
        }

        public DefendPlatformUnitRequest ConstructUnitRequest(FactionData factionData, DefendPlatformType platformType)
        {
            return new DefendPlatformUnitRequest(factionData, platformType);
        }

        public ResearchUnitRequest ConstructUnitRequest(FactionData factionData, ResearchType researchType)
        {
            return new ResearchUnitRequest(factionData, researchType);
        }

        public SuperWeaponUnitRequest ConstructUnitRequest(FactionData factionData, SuperWeaponType superWeaponType)
        {
            return new SuperWeaponUnitRequest(factionData, superWeaponType);
        }
    }
}