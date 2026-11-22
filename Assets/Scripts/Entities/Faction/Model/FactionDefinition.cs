using System.Collections.Generic;
using EmpireAtWar.Entities.DefendPlatform;
using EmpireAtWar.Entities.MiningFacility;
using EmpireAtWar.Entities.Ship.Data;
using EmpireAtWar.Entities.Squadrons;
using EmpireAtWar.Entities.SuperWeapons;
using EmpireAtWar.Mvc;
using UnityEngine;

namespace EmpireAtWar.Models.Factions
{
    /// <summary>Everything one faction owns: its units, research and the shared structures it may build.</summary>
    [CreateAssetMenu(fileName = nameof(FactionDefinition), menuName = "Data/Factions/FactionDefinition")]
    public class FactionDefinition : Data
    {
        [SerializeField] private FactionType factionType;
        [SerializeField] private FactionDataWrapper ships;
        [SerializeField] private SquadronFactionDataWrapper squadrons;
        [SerializeField] private FactionResearchWrapper research;

        [Header("Shared Buildables")]
        [SerializeField] private MiningFacilityType[] miningFacilities;
        [SerializeField] private DefendPlatformType[] defendPlatforms;
        [SerializeField] private SuperWeaponType[] superWeapons;

        [Header("Ship Roles")]
        [Tooltip("What the whole fleet shares against the other factions (e.g. Slow, Dps); ship flags compare within the faction.")]
        [SerializeField] private ShipRole shipMarks;

        public FactionType FactionType => factionType;
        public Dictionary<ShipType, FactionData> Ships => ships.Dictionary;
        public Dictionary<SquadronType, FactionData> Squadrons => squadrons.Dictionary;
        public Dictionary<ResearchType, ResearchLineData> ResearchLines => research.Dictionary;
        public IReadOnlyList<MiningFacilityType> MiningFacilities => miningFacilities;
        public IReadOnlyList<DefendPlatformType> DefendPlatforms => defendPlatforms;
        public IReadOnlyList<SuperWeaponType> SuperWeapons => superWeapons;
        public ShipRole ShipMarks => shipMarks;
    }
}
