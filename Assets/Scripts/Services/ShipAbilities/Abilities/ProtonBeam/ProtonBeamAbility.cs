using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using UnityEngine;

namespace EmpireAtWar.Services.ShipAbilities.Abilities
{
    public sealed class ProtonBeamAbility : SustainedBeamAbility
    {
        public ProtonBeamAbility(ProtonBeamSettings settings,
            ImpactEffectPresenter impactPresenter, WeaponsData weaponsData)
            : base(impactPresenter, weaponsData.GetProfile(WeaponType.LaserBeam), settings.DamageType, settings.Damage)
        {
        }

        protected override Transform GetMuzzle(IShipAbilityFacade caster) =>
            caster.Entity.GetFacade<IEntityTransformFacade>().Transform;
    }
}
