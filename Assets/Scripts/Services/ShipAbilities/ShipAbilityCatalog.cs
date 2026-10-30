using EmpireAtWar.Mvc;
using UnityEngine;
using Utilities.ScriptUtils.EditorSerialization;

namespace EmpireAtWar.Services.ShipAbilities
{
    [CreateAssetMenu(fileName = nameof(ShipAbilityCatalog), menuName = "Data/ShipAbilityCatalog")]
    public sealed class ShipAbilityCatalog : Data
    {
        [SerializeField] private DictionaryWrapper<ShipAbilityId, ShipAbilityDefinition> definitions;
        [SerializeField] private AutoPowerTuning autoPower = new AutoPowerTuning();

        public AutoPowerTuning AutoPower => autoPower;

        public ShipAbilityDefinition Get(ShipAbilityId id) => definitions.Dictionary[id];
    }
}
