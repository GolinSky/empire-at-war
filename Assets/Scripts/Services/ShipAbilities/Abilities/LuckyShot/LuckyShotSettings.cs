using System;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using UnityEngine;

namespace EmpireAtWar.Services.ShipAbilities.Abilities
{
    [Serializable]
    public sealed class LuckyShotSettings : ShipAbilitySettings, IShipAbilityTargetRule
    {
        [SerializeField] private float damage = 8000f;
        [SerializeField] private float windup = 1.25f;
        [SerializeField] private WeaponProfile projectile;

        public float Damage => damage;
        public float Windup => windup;
        public WeaponProfile Projectile => projectile;

        public bool CanTarget(ShipClass shipClass) => shipClass is ShipClass.Corvette or ShipClass.Frigate or
            ShipClass.Cruiser or ShipClass.Capital or ShipClass.HeavyCapital or ShipClass.Structure;

        public bool CanTarget(IShipAbilityFacade caster, IEntity target)
        {
            if (!CanTarget(target.HealthModel.ShipClass) || !target.TryGetFacade(out IHealthFacade _))
                return false;
            var members = caster.Health.GetShipUnits(HardPointType.Any);
            return members.Length > 0 && members[0].Id == 0 && !members[0].IsDestroyed;
        }

        public override IShipAbility CreateAbility(ShipAbilityDependencies dependencies) =>
            new LuckyShotAbility(this, dependencies.Entities, dependencies.BoltFlights,
                dependencies.ImpactPresenter, dependencies.Vision, dependencies.Session);
    }
}
