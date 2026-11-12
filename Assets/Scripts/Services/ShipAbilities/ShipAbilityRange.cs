using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Utils;
using UnityEngine;

namespace EmpireAtWar.Services.ShipAbilities
{
    /// <summary>
    /// Targeted ability range between centers. Hulls whose navigation footprints touch always count as in range:
    /// a large caster (Malevolence) cannot get closer than that to a station, so a shorter range would never be met.
    /// </summary>
    public static class ShipAbilityRange
    {
        // Navigation parks slightly outside exact footprint contact.
        private const float CONTACT_RANGE_FACTOR = 1.15f;

        public static bool IsInRange(Vector3 casterPosition, float casterRadius, IEntity target, float range)
        {
            Vector3 targetPosition = target.GetFacade<IEntityTransformFacade>().Transform.position;
            float contactRange = (casterRadius + FootprintRadius(target)) * CONTACT_RANGE_FACTOR;
            return PlanarGeometry.Distance(casterPosition, targetPosition) <= Mathf.Max(range, contactRange);
        }

        public static bool IsInRange(IShipAbilityFacade caster, IEntity target, float range) =>
            IsInRange(caster.WorldPosition, FootprintRadius(caster.Entity), target, range);

        private static float FootprintRadius(IEntity entity) =>
            entity.TryGetFacade(out INavigationFootprintFacade footprint) ? footprint.NavigationRadius : 0f;
    }
}
