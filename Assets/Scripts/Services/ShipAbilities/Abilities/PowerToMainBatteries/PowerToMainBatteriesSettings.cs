using System;
using EmpireAtWar.Components.Combat;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Services.ShipAbilities.Abilities
{
    [Serializable]
    public sealed class PowerToMainBatteriesSettings : ShipAbilitySettings
    {
        [SerializeField] private float fireDelayMultiplier = 0.33f;
        [SerializeField] private CombatStatModifier statModifier = new CombatStatModifier(1f, 1f, 0.25f, 0f, 1f);

        public float FireDelayMultiplier => fireDelayMultiplier;
        public CombatStatModifier StatModifier => statModifier;

        public override IShipAbility CreateAbility(IInstantiator instantiator) =>
            instantiator.Instantiate<PowerToMainBatteriesAbility>(new object[] { this });
    }
}
