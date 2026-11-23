using System;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.Units;
using UnityEngine;

namespace EmpireAtWar.Services.ShipAbilities.Abilities
{
    /// <summary>Spinal superlaser: one sustained beam from the bound muzzle against frigates and larger.</summary>
    [Serializable]
    public sealed class SuperlaserSettings : ShipAbilitySettings, IShipAbilityTargetRule, IShipAbilityTargetPriority
    {
        [SerializeField] private DamageType damageType = DamageType.Beam;
        [SerializeField] private float damage;

        public DamageType DamageType => damageType;
        public float Damage => damage;

        public bool CanTarget(IShipAbilityFacade caster, IEntity target) =>
            target.HealthModel.ShipClass is ShipClass.Frigate or ShipClass.Cruiser or ShipClass.HeavyCruiser or
                ShipClass.Capital or ShipClass.BattleCruiser or ShipClass.Dreadnought ||
            target.IsPlayerBase();

        // Automatic order: Capital > BattleCruiser > Dreadnought > station, then any other valid ship.
        public int GetTargetPriority(IEntity target)
        {
            if (target.IsPlayerBase()) return 1;
            return target.HealthModel.ShipClass switch
            {
                ShipClass.Capital => 4,
                ShipClass.BattleCruiser => 3,
                ShipClass.Dreadnought => 2,
                _ => 0
            };
        }

        public override IShipAbility CreateAbility(ShipAbilityDependencies dependencies) =>
            new SuperlaserAbility(this, dependencies.ImpactPresenter, dependencies.WeaponsData,
                dependencies.Vision, dependencies.Session);
    }
}
