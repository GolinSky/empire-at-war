using System;
using EmpireAtWar.Components.AttackComponent;
using UnityEngine;

namespace EmpireAtWar.Services.ShipAbilities.Abilities
{
    [Serializable]
    public sealed class ProtonBeamSettings : ShipAbilitySettings
    {
        [SerializeField] private DamageType damageType = DamageType.Beam;

        [SerializeField] private float damage;

        public float Damage => damage;
        public DamageType DamageType => damageType;

        public override IShipAbility CreateAbility(ShipAbilityDependencies dependencies) =>
            new ProtonBeamAbility(this, dependencies.ImpactPresenter, dependencies.WeaponsData);
    }
}
