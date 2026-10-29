using System;
using EmpireAtWar.Components.AttackComponent;
using UnityEngine;

namespace EmpireAtWar.Services.ShipAbilities.Abilities
{
    [Serializable]
    public sealed class IonShotSettings : ShipAbilitySettings
    {
        [SerializeField] private WeaponProfile projectile;
        [SerializeField] private float disableDuration = 1.5f;

        public WeaponProfile Projectile => projectile;
        public float DisableDuration => disableDuration;

        public override float GetEffectDuration(float activeDuration) => disableDuration;

        public override IShipAbility CreateAbility(ShipAbilityDependencies dependencies) =>
            new IonShotAbility(this, dependencies.BoltFlights);
    }
}
