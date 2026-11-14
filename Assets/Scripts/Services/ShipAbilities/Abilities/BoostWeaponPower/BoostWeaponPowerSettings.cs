using System;
using EmpireAtWar.Components.Combat;
using UnityEngine;

namespace EmpireAtWar.Services.ShipAbilities.Abilities
{
    [Serializable]
    public sealed class BoostWeaponPowerSettings : ShipAbilitySettings
    {
        [SerializeField] private CombatStatModifier statModifier;
        [SerializeField] private float shotIntervalMultiplier = 0.1f;

        public CombatStatModifier StatModifier => statModifier;
        public float ShotIntervalMultiplier => shotIntervalMultiplier;

        public override IShipAbility CreateAbility(ShipAbilityDependencies dependencies) =>
            new BoostWeaponPowerAbility(this);
    }
}
