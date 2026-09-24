using System;
using EmpireAtWar.Components.Combat;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Services.ShipAbilities.Abilities
{
    [Serializable]
    public sealed class BoostWeaponPowerSettings : ShipAbilitySettings
    {
        [SerializeField] private CombatStatModifier statModifier;

        public CombatStatModifier StatModifier => statModifier;

        public override IShipAbility CreateAbility(IInstantiator instantiator) =>
            instantiator.Instantiate<BoostWeaponPowerAbility>(new object[] { this });
    }
}
