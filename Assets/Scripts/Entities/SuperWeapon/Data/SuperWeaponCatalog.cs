using EmpireAtWar.Models.Factions;
using EmpireAtWar.Mvc;
using UnityEngine;
using Utilities.ScriptUtils.EditorSerialization;

namespace EmpireAtWar.Entities.SuperWeapons
{
    /// <summary>Purchase data of every super weapon; factions pick from it.</summary>
    [CreateAssetMenu(fileName = nameof(SuperWeaponCatalog), menuName = "Data/Factions/SuperWeaponCatalog")]
    public class SuperWeaponCatalog : Data
    {
        [SerializeField] private DictionaryWrapper<SuperWeaponType, FactionData> entries;

        public FactionData Get(SuperWeaponType type) => entries.Dictionary[type];
    }
}
