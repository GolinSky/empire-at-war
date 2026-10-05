using System.Collections.Generic;
using EmpireAtWar.Entities.DefendPlatform;
using EmpireAtWar.Entities.MiningFacility;
using EmpireAtWar.Entities.Squadrons;
using EmpireAtWar.Entities.SuperWeapons;
using UnityEngine;

namespace EmpireAtWar.Models.Factions
{
    /// <summary>Everything one faction owns: its units, research and the shared structures it may build.</summary>
    [CreateAssetMenu(fileName = nameof(FactionDefinition), menuName = "Data/Factions/FactionDefinition")]
    public class FactionDefinition : ScriptableObject
    {
        [SerializeField] private FactionType factionType;
        [SerializeField] private FactionDataWrapper ships;
        [SerializeField] private SquadronFactionDataWrapper squadrons;
        [SerializeField] private FactionResearchWrapper research;

        [Header("Shared Buildables")]
        [SerializeField] private MiningFacilityType[] miningFacilities;
        [SerializeField] private DefendPlatformType[] defendPlatforms;
        [SerializeField] private SuperWeaponType[] superWeapons;

        public FactionType FactionType => factionType;
        public Dictionary<ShipType, FactionData> Ships => ships.Dictionary;
        public Dictionary<SquadronType, FactionData> Squadrons => squadrons.Dictionary;
        public Dictionary<ResearchType, ResearchLineData> ResearchLines => research.Dictionary;
        public IReadOnlyList<MiningFacilityType> MiningFacilities => miningFacilities;
        public IReadOnlyList<DefendPlatformType> DefendPlatforms => defendPlatforms;
        public IReadOnlyList<SuperWeaponType> SuperWeapons => superWeapons;
    }
}
