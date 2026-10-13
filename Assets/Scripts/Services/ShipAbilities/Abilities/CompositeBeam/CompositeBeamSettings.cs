using System;
using UnityEngine;

namespace EmpireAtWar.Services.ShipAbilities.Abilities
{
    [Serializable]
    public sealed class CompositeBeamSettings : ShipAbilitySettings
    {
        [SerializeField] private float damage;

        public float Damage => damage;

        public override IShipAbility CreateAbility(ShipAbilityDependencies dependencies) =>
            new CompositeBeamAbility(this, dependencies.ImpactPresenter, dependencies.WeaponsData);
    }
}
