using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.Ship.Abilities;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Vision;
using UnityEngine;

namespace EmpireAtWar.Services.ShipAbilities.Abilities
{
    public sealed class SuperlaserAbility : SustainedBeamAbility
    {
        private readonly SuperlaserSettings _settings;

        public SuperlaserAbility(SuperlaserSettings settings,
            ImpactEffectPresenter impactPresenter, WeaponsData weaponsData, IVisionService vision, IMatchSession session)
            : base(impactPresenter, weaponsData.GetProfile(WeaponType.LaserBeam).WithColor(settings.BeamColor),
                settings.DamageType, settings.Damage, vision, session)
        {
            _settings = settings;
        }

        protected override float GetTargetDamageMultiplier(IEntity target) => _settings.GetDamageMultiplier(target);

        // The ship's bound beam muzzle (Ship.compositeBeamMuzzle) sits on the spinal cannon's fire point.
        protected override Transform GetMuzzle(IShipAbilityFacade caster) =>
            caster.Entity.GetFacade<ICompositeBeamFacade>().Muzzle;
    }
}
