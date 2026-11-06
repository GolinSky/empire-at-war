using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.Ship.Abilities;
using UnityEngine;

namespace EmpireAtWar.Services.ShipAbilities.Abilities
{
    public sealed class CompositeBeamAbility : SustainedBeamAbility
    {
        public CompositeBeamAbility(CompositeBeamSettings settings,
            ImpactEffectPresenter impactPresenter, WeaponsData weaponsData)
            : base(impactPresenter, weaponsData.GetProfile(WeaponType.CompositeBeam), DamageType.CompositeBeam, settings.Damage)
        {
        }

        protected override Transform GetMuzzle(IShipAbilityFacade caster) =>
            caster.Entity.GetFacade<ICompositeBeamFacade>().Muzzle;
    }
}
