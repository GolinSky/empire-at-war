using EmpireAtWar.Models.Factions;
using EmpireAtWar.Mvc;
using UnityEngine;
using Utilities.ScriptUtils.EditorSerialization;

namespace EmpireAtWar.Entities.DefendPlatform
{
    /// <summary>Purchase data of every defend platform; factions pick from it.</summary>
    [CreateAssetMenu(fileName = nameof(DefendPlatformCatalog), menuName = "Data/Factions/DefendPlatformCatalog")]
    public class DefendPlatformCatalog : Data
    {
        [SerializeField] private DictionaryWrapper<DefendPlatformType, FactionData> entries;

        public FactionData Get(DefendPlatformType type) => entries.Dictionary[type];
    }
}
