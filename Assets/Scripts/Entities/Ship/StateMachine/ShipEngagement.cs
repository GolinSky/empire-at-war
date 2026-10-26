using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Components.Ship.Movement;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Entities.BaseEntity;
using UnityEngine;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;

namespace EmpireAtWar.Entities.Ship.StateMachine
{
    public static class ShipEngagement
    {
        private const float STANDOFF_RANGE_FACTOR = 0.65f;

        public static bool CanEngage(IEntity enemy, IShipMovement movement,
            IWeaponComponent weapon)
        {
            return !enemy.HealthModel.IsDestroyed && enemy.HealthModel.HasUnits &&
                   weapon.HasEnoughRange(movement.GetRange(
                       enemy.GetFacade<IEntityTransformFacade>().Transform.position),
                       ContactDistance(movement, enemy), enemy.HealthModel.ShipClass);
        }

        /// <summary>An enemy the ship's loadout is built for; AI target searches take these before the others.</summary>
        public static bool IsPreferred(IEntity enemy, IWeaponComponent weapon) =>
            weapon.PreferredTarget.Prefers(enemy.HealthModel.ShipClass);

        /// <summary>
        /// The nearest live radar contact the loadout prefers; the nearest other one only while no preferred contact
        /// qualifies. With <paramref name="inRangeOnly"/> only contacts the guns already reach count. Null when none.
        /// </summary>
        public static IEntity FindNearestEnemy(IRadarComponent radar, IShipMovement movement, IWeaponComponent weapon,
            bool inRangeOnly)
        {
            IEntity nearest = null;
            bool hasPreferred = false;
            float nearestDistance = float.PositiveInfinity;
            // Indexed: ObservableList boxes its enumerator.
            for (int i = 0; i < radar.Enemies.Count; i++)
            {
                IEntity enemy = radar.Enemies[i];
                if (enemy.IsCloaked() || enemy.HealthModel.IsDestroyed || !enemy.HealthModel.HasUnits) continue;
                if (inRangeOnly && !CanEngage(enemy, movement, weapon)) continue;

                bool isPreferred = IsPreferred(enemy, weapon);
                if (hasPreferred && !isPreferred) continue;
                float distance = movement.GetRange(enemy.GetFacade<IEntityTransformFacade>().Transform.position);
                if (distance < nearestDistance || isPreferred && !hasPreferred)
                {
                    hasPreferred = isPreferred;
                    nearestDistance = distance;
                    nearest = enemy;
                }
            }

            return nearest;
        }

        /// <summary>
        /// Closest centre distance navigation allows between the ship and the target.
        /// Targets without a footprint (squadrons, platforms) do not block hulls.
        /// </summary>
        public static float ContactDistance(IShipMovement movement, IEntity target)
        {
            float targetRadius = target.TryGetFacade(out INavigationFootprintFacade footprint)
                ? footprint.NavigationRadius
                : 0f;
            return movement.NavigationRadius + targetRadius;
        }

        /// <summary>
        /// Commits to one firing-range approach until arrival or a new target.
        /// </summary>
        public static void Pursue(IShipMovement movement, IWeaponComponent weapon,
            IEntity target, bool newTarget = false)
        {
            if (!newTarget && movement.IsMoving) return;
            movement.MoveToPosition(GetStandoffPosition(movement, weapon, target),
                preserveCourse: !newTarget);
        }

        public static Vector3 GetStandoffPosition(IShipMovement movement,
            IWeaponComponent weapon, IEntity target)
        {
            Vector3 targetPosition = target.GetFacade<IEntityTransformFacade>().Transform.position;
            Vector3 fromTarget = movement.CurrentPosition - targetPosition;
            fromTarget.y = 0f;
            return targetPosition + fromTarget.normalized * Mathf.Max(
                weapon.GetEngagementRange(target.HealthModel.ShipClass) * STANDOFF_RANGE_FACTOR,
                ContactDistance(movement, target));
        }
    }
}
