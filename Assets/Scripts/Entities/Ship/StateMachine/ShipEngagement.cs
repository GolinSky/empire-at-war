using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Components.Ship.Movement;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.SpaceStation;
using EmpireAtWar.Utils;
using System.Collections.Generic;
using UnityEngine;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;

namespace EmpireAtWar.Entities.Ship.StateMachine
{
    public static class ShipEngagement
    {
        private const float STANDOFF_RANGE_FACTOR = 0.65f;
        private const float SIEGE_CLEARANCE_MARGIN = 1f;

        public static bool CanEngage(IEntity enemy, IShipMovement movement,
            IWeaponComponent weapon)
        {
            return !enemy.HealthModel.IsDestroyed && enemy.HealthModel.HasUnits &&
                   IsInRange(movement, weapon, enemy, enemy.GetFacade<IEntityTransformFacade>().Transform.position);
        }

        public static bool IsInRange(IShipMovement movement, IWeaponComponent weapon, IEntity target,
            Vector3 targetPosition)
        {
            float distance = movement.GetRange(targetPosition);
            if (UsesCloseSiegeApproach(movement, weapon, target))
                return distance <= SiegeStandoffDistance(movement, target, targetPosition) + SIEGE_CLEARANCE_MARGIN;

            return weapon.HasEnoughRange(distance, ContactDistance(movement, target), target.HealthModel.ShipClass);
        }

        /// <summary>Full-reach test: an ordered ship holds while this is true and resumes only when it fails.</summary>
        public static bool IsInReach(IShipMovement movement, IWeaponComponent weapon, IEntity target,
            Vector3 targetPosition)
        {
            float distance = movement.GetRange(targetPosition);
            if (UsesCloseSiegeApproach(movement, weapon, target))
                return distance <= SiegeStandoffDistance(movement, target, targetPosition) + SIEGE_CLEARANCE_MARGIN;

            return weapon.IsWithinReach(distance, ContactDistance(movement, target), target.HealthModel.ShipClass);
        }

        /// <summary>An enemy the ship's loadout is built for; AI target searches take these before the others.</summary>
        public static bool IsPreferred(IEntity enemy, IWeaponComponent weapon) =>
            weapon.PreferredTarget.Prefers(enemy.HealthModel.ShipClass);

        /// <summary>
        /// The nearest live radar contact, best tier first: the previous target's class, then what the loadout
        /// prefers, then anything. With <paramref name="inRangeOnly"/> only contacts the guns already reach count.
        /// Null when none qualifies.
        /// </summary>
        public static IEntity FindNearestEnemy(IRadarComponent radar, IShipMovement movement, IWeaponComponent weapon,
            bool inRangeOnly, ShipClass? previousClass = null)
        {
            IEntity best = null;
            int bestTier = -1;
            float bestDistance = float.PositiveInfinity;
            // Indexed: ObservableList boxes its enumerator.
            for (int i = 0; i < radar.Enemies.Count; i++)
            {
                IEntity enemy = radar.Enemies[i];
                if (inRangeOnly && !CanEngage(enemy, movement, weapon)) continue;
                Offer(enemy, movement, weapon, previousClass, ref best, ref bestTier, ref bestDistance);
            }

            return best;
        }

        /// <summary>Same choice as the radar overload, over an explicit candidate list (team contacts).</summary>
        public static IEntity FindNearestEnemy(IReadOnlyList<IEntity> candidates, IShipMovement movement,
            IWeaponComponent weapon, ShipClass? previousClass = null)
        {
            IEntity best = null;
            int bestTier = -1;
            float bestDistance = float.PositiveInfinity;
            for (int i = 0; i < candidates.Count; i++)
                Offer(candidates[i], movement, weapon, previousClass, ref best, ref bestTier, ref bestDistance);
            return best;
        }

        private static void Offer(IEntity enemy, IShipMovement movement, IWeaponComponent weapon,
            ShipClass? previousClass, ref IEntity best, ref int bestTier, ref float bestDistance)
        {
            if (enemy.IsCloaked() || enemy.HealthModel.IsDestroyed || !enemy.HealthModel.HasUnits) return;
            int tier = previousClass.HasValue && enemy.HealthModel.ShipClass == previousClass.Value ? 2 :
                IsPreferred(enemy, weapon) ? 1 : 0;
            if (tier < bestTier) return;
            float distance = movement.GetRange(enemy.GetFacade<IEntityTransformFacade>().Transform.position);
            if (tier == bestTier && distance >= bestDistance) return;
            best = enemy;
            bestTier = tier;
            bestDistance = distance;
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
            IWeaponComponent weapon, IEntity target, Vector3 formationOffset = default)
        {
            Vector3 targetPosition = target.GetFacade<IEntityTransformFacade>().Transform.position;
            float distance = UsesCloseSiegeApproach(movement, weapon, target)
                ? SiegeStandoffDistance(movement, target, targetPosition)
                : Mathf.Max(weapon.GetEngagementRange(target.HealthModel.ShipClass) * STANDOFF_RANGE_FACTOR,
                    ContactDistance(movement, target));
            Vector3 fromTarget = movement.CurrentPosition - targetPosition;
            fromTarget.y = 0f;
            return targetPosition + PlanarGeometry.NormalizeDirection(formationOffset, fromTarget) * distance;
        }

        // A footprint larger than nominal weapon reach makes the normal contact slack a substantial loss
        // of mount coverage. Only ships that are already contact-limited need to close against structures.
        private static bool UsesCloseSiegeApproach(IShipMovement movement, IWeaponComponent weapon, IEntity target) =>
            weapon.CanUseSiegeApproach(target) &&
            !weapon.HasEnoughRange(movement.NavigationRadius, 0f, target.HealthModel.ShipClass);

        private static float SiegeStandoffDistance(IShipMovement movement, IEntity target, Vector3 center)
        {
            // The station's map generation keep-out radius can be much larger than its live navigation obstacle.
            if (target.TryGetFacade(out IStationHullFacade hull))
                return movement.NavigationRadius + hull.GetHullRadius(center) + SIEGE_CLEARANCE_MARGIN;

            return ContactDistance(movement, target) + SIEGE_CLEARANCE_MARGIN;
        }
    }
}
