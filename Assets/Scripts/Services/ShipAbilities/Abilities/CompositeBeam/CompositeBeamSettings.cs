using System;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Services.ShipAbilities.Abilities
{
    [Serializable]
    public sealed class CompositeBeamSettings : ShipAbilitySettings
    {
        [SerializeField] private float damage;

        public float Damage => damage;

        public override IShipAbility CreateAbility(IInstantiator instantiator) =>
            instantiator.Instantiate<CompositeBeamAbility>(new object[] { this });
    }
}
