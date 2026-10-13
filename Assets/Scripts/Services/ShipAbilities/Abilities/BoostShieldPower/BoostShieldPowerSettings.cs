using System;
using EmpireAtWar.Components.Combat;
using UnityEngine;

namespace EmpireAtWar.Services.ShipAbilities.Abilities
{
    [Serializable]
    public sealed class BoostShieldPowerSettings : ShipAbilitySettings
    {
        [SerializeField] private CombatStatModifier statModifier;

        public CombatStatModifier StatModifier => statModifier;

        public override IShipAbility CreateAbility(ShipAbilityDependencies dependencies) =>
            new BoostShieldPowerAbility(this);
    }
}
