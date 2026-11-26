using System;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using UnityEngine;
using EmpireAtWar.Entities.Units;

namespace EmpireAtWar.Services.ShipAbilities.Abilities
{
    /// <summary>Automatic use: on the auto-power target (a warship or structure in range) whenever Luke can fire.</summary>
    [Serializable]
    public sealed class LuckyShotSettings : ShipAbilitySettings, IShipAbilityTargetRule, IShipAbilityAutoUseRule
    {
        [SerializeField] private float damage = 8000f;
        [SerializeField] private float windup = 1.25f;
        [SerializeField] private WeaponProfile projectile;

        public float Damage => damage;
        public float Windup => windup;
        public WeaponProfile Projectile => projectile;

        public bool CanTarget(ShipClass shipClass) => shipClass is ShipClass.Corvette or ShipClass.Frigate or
            ShipClass.Cruiser or ShipClass.HeavyCruiser or ShipClass.Capital or ShipClass.BattleCruiser or
            ShipClass.Dreadnought or ShipClass.Structure;

        public bool CanTarget(IShipAbilityFacade caster, IEntity target)
        {
            return CanTarget(target.HealthModel.ShipClass) && target.TryGetFacade(out IHealthFacade _) &&
                   IsLukeAlive(caster);
        }

        public bool ShouldAutoActivate(ShipAbilityDependencies dependencies, IShipAbilityFacade caster) =>
            IsLukeAlive(caster);

        private static bool IsLukeAlive(IShipAbilityFacade caster)
        {
            var members = caster.Health.GetShipUnits(HardPointType.Any);
            return members.Length > 0 && members[0].Id == 0 && !members[0].IsDestroyed;
        }

        public override IShipAbility CreateAbility(ShipAbilityDependencies dependencies) =>
            new LuckyShotAbility(this, dependencies.Entities, dependencies.ImpactPresenter, dependencies.Vision,
                dependencies.Session);
    }
}
