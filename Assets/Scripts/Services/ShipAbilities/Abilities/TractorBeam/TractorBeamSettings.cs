using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using System;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Ship.Health;
using UnityEngine;

namespace EmpireAtWar.Services.ShipAbilities.Abilities
{
    [Serializable]
    public sealed class TractorBeamSettings : ShipAbilitySettings, IShipAbilityTargetRule
    {
        [SerializeField] private float speedMultiplier = 0.25f;
        [SerializeField] private ShipClass[] targetClasses = { ShipClass.Corvette, ShipClass.Frigate };
        [SerializeField] private WeaponProfile beam;

        public float SpeedMultiplier => speedMultiplier;
        public WeaponProfile Beam => beam;

        public bool CanTarget(ShipClass shipClass) => Array.IndexOf(targetClasses, shipClass) >= 0;

        public bool CanTarget(IShipAbilityFacade caster, IEntity target) =>
            CanTarget(target.HealthModel.ShipClass) && target.TryGetFacade(out ICombatModifiersFacade _);

        public override IShipAbility CreateAbility(ShipAbilityDependencies dependencies) =>
            new TractorBeamAbility(this);
    }
}
