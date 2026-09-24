using System;
using EmpireAtWar.Components.Combat;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Services.ShipAbilities.Abilities
{
    [Serializable]
    public sealed class ConcentrateFireSettings : ShipAbilitySettings
    {
        [SerializeField] private CombatStatModifier allyStatModifier;
        [SerializeField] private float commandRadius;

        public CombatStatModifier AllyStatModifier => allyStatModifier;
        public float CommandRadius => commandRadius;

        public override IShipAbility CreateAbility(IInstantiator instantiator) =>
            instantiator.Instantiate<ConcentrateFireAbility>(new object[] { this });
    }
}
