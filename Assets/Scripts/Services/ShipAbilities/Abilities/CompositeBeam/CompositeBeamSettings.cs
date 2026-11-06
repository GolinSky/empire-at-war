using EmpireAtWar.Entities.Units;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using System;
using UnityEngine;

namespace EmpireAtWar.Services.ShipAbilities.Abilities
{
    [Serializable]
    public sealed class CompositeBeamSettings : ShipAbilitySettings, IShipAbilityTargetRule
    {
        [SerializeField] private float damage;

        public float Damage => damage;

        public bool CanTarget(IShipAbilityFacade caster, IEntity target) =>
            target.HealthModel.ShipClass == ShipClass.Frigate ||
            target.HealthModel.ShipClass == ShipClass.Cruiser ||
            target.HealthModel.ShipClass == ShipClass.Capital ||
            target.HealthModel.ShipClass == ShipClass.HeavyCapital ||
            target.IsPlayerBase();

        public override IShipAbility CreateAbility(ShipAbilityDependencies dependencies) =>
            new CompositeBeamAbility(this, dependencies.ImpactPresenter, dependencies.WeaponsData);
    }
}
