using System;
using EmpireAtWar.Components.AttackComponent;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Services.ShipAbilities.Abilities
{
    [Serializable]
    public sealed class ProtonBeamSettings : ShipAbilitySettings
    {
        [SerializeField] private DamageType damageType = DamageType.Beam;

        [SerializeField] private float damage;

        public float Damage => damage;
        public DamageType DamageType => damageType;

        public override IShipAbility CreateAbility(IInstantiator instantiator) =>
            instantiator.Instantiate<ProtonBeamAbility>(new object[] { this });
    }
}
