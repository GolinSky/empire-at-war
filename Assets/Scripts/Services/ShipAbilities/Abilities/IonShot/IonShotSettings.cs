using System;
using EmpireAtWar.Components.AttackComponent;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Services.ShipAbilities.Abilities
{
    [Serializable]
    public sealed class IonShotSettings : ShipAbilitySettings
    {
        [SerializeField] private WeaponProfile projectile;
        [SerializeField] private float disableDuration = 3f;

        public WeaponProfile Projectile => projectile;
        public float DisableDuration => disableDuration;

        public override float GetEffectDuration(float activeDuration) => disableDuration;

        public override IShipAbility CreateAbility(IInstantiator instantiator) =>
            instantiator.Instantiate<IonShotAbility>(new object[] { this });
    }
}
