using EmpireAtWar.Mvc;
using UnityEngine;
using Utilities.ScriptUtils.EditorSerialization;

namespace EmpireAtWar.Services.ShipAbilities
{
    [CreateAssetMenu(fileName = nameof(ShipAbilityCatalog), menuName = "Data/ShipAbilityCatalog")]
    public sealed class ShipAbilityCatalog : Data
    {
        [SerializeField] private DictionaryWrapper<ShipAbilityId, ShipAbilityDefinition> definitions;

        public ShipAbilityDefinition Get(ShipAbilityId id) => definitions.Dictionary[id];
    }
}
