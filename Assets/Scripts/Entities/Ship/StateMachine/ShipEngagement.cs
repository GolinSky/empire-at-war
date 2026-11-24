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
        /// <summary>
        /// A ship already holding on a target keeps holding while the target is within this multiple of its reach;
        /// without the margin a target drifting across the reach edge restarted the approach every few frames.
        /// </summary>
        public const float HELD_REACH_FACTOR = 1.1f;
        /// <summary>Team-vision search radius for threats; reach is checked afterwards per contact.</summary>
        public const float TEAM_THREAT_RADIUS = 1500f;
        private const int STATION_ATTACKER_RANK = 2;
        private const int SELF_ATTACKER_RANK = 1;

        public static bool CanEngage(IEntity enemy, IShipMovement movement,
            IWeaponComponent weapon)
        {
            return !enemy.HealthModel.IsDestroyed && enemy.HealthModel.HasUnits &&
                   IsInReach(movement, weapon, enemy, enemy.GetFacade<IEntityTransformFacade>().Transform.position);
        }

        /// <summary>
        /// <see cref="CanEngage(IEntity, IShipMovement, IWeaponComponent)"/> with the leave margin when the ship
        /// already holds on the enemy.
        /// </summary>
        public static bool CanEngage(IEntity enemy, IShipMovement movement, IWeaponComponent weapon, bool isHolding)
        {
            return !enemy.HealthModel.IsDestroyed && enemy.HealthModel.HasUnits &&
                   IsInReach(movement, weapon, enemy, enemy.GetFacade<IEntityTransformFacade>().Transform.position,
                       isHolding ? HELD_REACH_FACTOR : 1f);
        }

        /// <summary>A mobile ship of corvette size or bigger: no strikecraft, platform, satellite or mine.</summary>
        public static bool IsWarship(IEntity enemy) =>
            enemy.HealthModel.ShipClass.HullTier() > 0 && enemy.TryGetFacade(out IUnitOrderObserverFacade _);

        /// <summary>2 = attacks a team station, 1 = attacks this ship, 0 = neither.</summary>
        public static int ThreatRank(IEntity enemy, IShipThreatSense threats) =>
            threats.IsAttackingTeamStation(enemy) ? STATION_ATTACKER_RANK :
            threats.IsAttackingSelf(enemy) ? SELF_ATTACKER_RANK : 0;

        /// <summary>
        /// A ship built for ships fights ships and structures before any strikecraft, whatever the threat: its heavy
        /// guns skip strikecraft while a ship is near, so lasers and point defense handle squadrons.
        /// </summary>
        public static bool DefersStrikecraft(IEntity enemy, IWeaponComponent weapon) =>
            weapon.PreferredTarget == TargetPreference.Ships && enemy.HealthModel.ShipClass.IsStrikecraft();

        /// <summary>
        /// Main-target choice for ships without an attack order: for a ship built for ships any non-strikecraft enemy
        /// first (<see cref="DefersStrikecraft"/>); then attacker of a team station, then attacker of this ship, then
        /// what the loadout prefers, then the biggest hull class, then the nearest. Dead or cloaked candidates are
        /// skipped; null when none is left.
        /// </summary>
        public static IEntity SelectThreat(IReadOnlyList<IEntity> candidates, IShipMovement movement,
            IWeaponComponent weapon, IShipThreatSense threats)
        {
            IEntity best = null;
            int bestRank = -1;
            bool bestDeferred = false;
            bool bestPreferred = false;
            int bestTier = -1;
            float bestDistance = float.PositiveInfinity;
            for (int i = 0; i < candidates.Count; i++)
            {
                IEntity enemy = candidates[i];
                if (enemy.IsCloaked() || enemy.HealthModel.IsDestroyed || !enemy.HealthModel.HasUnits) continue;
                bool deferred = DefersStrikecraft(enemy, weapon);
                if (best != null && deferred && !bestDeferred) continue;
                // A first non-deferred enemy replaces a deferred pick whatever its rank.
                bool replacesDeferred = best != null && bestDeferred && !deferred;
                int rank = ThreatRank(enemy, threats);
                if (!replacesDeferred && rank < bestRank) continue;
                bool preferred = IsPreferred(enemy, weapon);
                int tier = enemy.HealthModel.ShipClass.HullTier();
                float distance = movement.GetRange(enemy.GetFacade<IEntityTransformFacade>().Transform.position);
                if (!replacesDeferred && rank == bestRank)
                {
                    if (bestPreferred && !preferred) continue;
                    if (preferred == bestPreferred)
                    {
                        if (tier < bestTier) continue;
                        if (tier == bestTier && distance >= bestDistance) continue;
                    }
                }

                best = enemy;
                bestRank = rank;
                bestDeferred = deferred;
                bestPreferred = preferred;
                bestTier = tier;
                bestDistance = distance;
            }

            return best;
        }

        /// <summary>
        /// Whether a fresh <see cref="SelectThreat"/> pick should replace the held target: a ship over deferred
        /// strikecraft (<see cref="DefersStrikecraft"/>), a higher threat rank, or the same rank and a preferred enemy
        /// over one the loadout is not built for.
        /// </summary>
        public static bool Outranks(IEntity candidate, IEntity held, IWeaponComponent weapon,
            IShipThreatSense threats)
        {
            bool candidateDeferred = DefersStrikecraft(candidate, weapon);
            bool heldDeferred = DefersStrikecraft(held, weapon);
            if (candidateDeferred != heldDeferred) return heldDeferred;
            int candidateRank = ThreatRank(candidate, threats);
            int heldRank = ThreatRank(held, threats);
            if (candidateRank != heldRank) return candidateRank > heldRank;
            return IsPreferred(candidate, weapon) && !IsPreferred(held, weapon);
        }

        public static bool IsInRange(IShipMovement movement, IWeaponComponent weapon, IEntity target,
            Vector3 targetPosition)
        {
            float distance = movement.GetRange(targetPosition);
            if (UsesCloseSiegeApproach(movement, weapon, target))
                return distance <= SiegeStandoffDistance(movement, target, targetPosition) + SIEGE_CLEARANCE_MARGIN;

            return weapon.HasEnoughRange(distance, ContactDistance(movement, target), target.HealthModel.ShipClass);
        }

        /// <summary>
        /// Full-reach test: an ordered ship holds while this is true and resumes only when it fails.
        /// <paramref name="reachFactor"/> scales every reach (<see cref="HELD_REACH_FACTOR"/> for a held ship).
        /// </summary>
        public static bool IsInReach(IShipMovement movement, IWeaponComponent weapon, IEntity target,
            Vector3 targetPosition, float reachFactor = 1f)
        {
            float distance = movement.GetRange(targetPosition) / reachFactor;
            if (UsesCloseSiegeApproach(movement, weapon, target))
                return distance <= SiegeStandoffDistance(movement, target, targetPosition) + SIEGE_CLEARANCE_MARGIN;

            if (weapon is IArtilleryCapability artillery)
            {
                float artilleryRange = artillery.GetArtilleryRange(target.HealthModel.ShipClass);
                if (artilleryRange > 0f && distance <= artilleryRange) return true;
            }

            return weapon.IsWithinReach(distance, ContactDistance(movement, target), target.HealthModel.ShipClass);
        }

        /// <summary>
        /// Hold-or-close-in test for a ship engaging <paramref name="enemy"/>: <see cref="CanEngage(IEntity, IShipMovement, IWeaponComponent, bool)"/>
        /// and the forward main batteries reach it bow-on. Mains sit well aft of the hull centre, so a centre distance
        /// inside reach can leave them short; the ship then held and swung broadside to use one side only.
        /// The main battery test has no held margin: when the nearest target hardpoints die, mains just past their
        /// range would otherwise hold silent; failing it only makes the ship close in.
        /// </summary>
        public static bool CanHoldOn(IEntity enemy, IShipMovement movement, IWeaponComponent weapon, bool isHolding) =>
            CanEngage(enemy, movement, weapon, isHolding) &&
            weapon.MainBatteryReaches(enemy, ContactDistance(movement, enemy), 1f);

        /// <summary>Whether the ship's guns would reach the target from <paramref name="origin"/>.</summary>
        public static bool IsInReachFrom(Vector3 origin, IShipMovement movement, IWeaponComponent weapon,
            IEntity target)
        {
            float distance = PlanarGeometry.Distance(origin,
                target.GetFacade<IEntityTransformFacade>().Transform.position);
            if (weapon is IArtilleryCapability artillery)
            {
                float artilleryRange = artillery.GetArtilleryRange(target.HealthModel.ShipClass);
                if (artilleryRange > 0f && distance <= artilleryRange) return true;
            }

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
