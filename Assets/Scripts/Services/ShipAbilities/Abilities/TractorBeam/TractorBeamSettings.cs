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
        [SerializeField] private WeaponProfile beam;

        public float SpeedMultiplier => speedMultiplier;
        internal override HardPointType RequiredSubsystem => HardPointType.TractorBeam;
        public WeaponProfile Beam => beam;

        /// <summary>Only ships of a lower class than the caster; strikecraft and structures are immune.</summary>
        public bool CanTarget(ShipClass caster, ShipClass target) =>
            target.HullTier() > 0 && target.HullTier() < caster.HullTier();

        public bool CanTarget(IShipAbilityFacade caster, IEntity target) =>
            CanTarget(caster.Entity.HealthModel.ShipClass, target.HealthModel.ShipClass) &&
            target.TryGetFacade(out ICombatModifiersFacade _);

        public override IShipAbility CreateAbility(ShipAbilityDependencies dependencies) =>
            new TractorBeamAbility(this);
    }
}
