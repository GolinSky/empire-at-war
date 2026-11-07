using EmpireAtWar.Models.Health;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using System;
using EmpireAtWar.Components.AttackComponent;
using UnityEngine;

namespace EmpireAtWar.Services.ShipAbilities.Abilities
{
    [Serializable]
    public sealed class IonShotSettings : ShipAbilitySettings, IShipAbilityTargetRule
    {
        [SerializeField] private WeaponProfile projectile;
        [SerializeField] private float disableDuration = 1.5f;

        public WeaponProfile Projectile => projectile;
        public float DisableDuration => disableDuration;

        public override float GetEffectDuration(float activeDuration) => disableDuration;

        public bool CanTarget(IShipAbilityFacade caster, IEntity target)
        {
            if (!target.TryGetFacade(out ICombatModifiersFacade _)) return false;
            foreach (IHardPointModel member in caster.Health.GetShipUnits(HardPointType.Any))
                if (!member.IsDestroyed) return true;
            return false;
        }

        public override IShipAbility CreateAbility(ShipAbilityDependencies dependencies) =>
            new IonShotAbility(this, dependencies.BoltFlights);
    }
}
