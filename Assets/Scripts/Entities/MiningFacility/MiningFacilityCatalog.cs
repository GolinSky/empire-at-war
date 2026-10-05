using EmpireAtWar.Models.Factions;
using EmpireAtWar.Mvc;
using UnityEngine;
using Utilities.ScriptUtils.EditorSerialization;

namespace EmpireAtWar.Entities.MiningFacility
{
    /// <summary>Purchase data of every mining facility; factions pick from it.</summary>
    [CreateAssetMenu(fileName = nameof(MiningFacilityCatalog), menuName = "Data/Factions/MiningFacilityCatalog")]
    public class MiningFacilityCatalog : Data
    {
        [SerializeField] private DictionaryWrapper<MiningFacilityType, FactionData> entries;

        public FactionData Get(MiningFacilityType type) => entries.Dictionary[type];
    }
}
