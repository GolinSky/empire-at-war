using EmpireAtWar.Entities.EnemyFaction.Models.Objectives;
using UnityEngine;

namespace EmpireAtWar.Entities.EnemyFaction.Models
{
    /// <summary>How a zone force carries out its zone's plan (W47, D4) once the allocator has sent it.</summary>
    public static class EnemyZoneTaskRules
    {
        /// <summary>RaidAndLeave: the zone is taken and no armed hostile is left, so every ship leaves; no garrison.</summary>
        public static bool LeavesZone(EnemyZonePlan plan, EnemyZoneObjective zone) =>
            plan == EnemyZonePlan.RaidAndLeave && EnemyZoneAllocator.IsCleared(zone);

        /// <summary>
        /// HoldBackAndCounter: while the zone is owned and quiet (no hostile near, inside or rated in it) its force waits
        /// outside; a retake brings it back in.
        /// </summary>
        public static bool HoldsBack(EnemyZonePlan plan, EnemyZoneObjective zone) =>
            plan == EnemyZonePlan.HoldBackAndCounter && zone.IsOwned && !zone.IsHostileNear && !zone.IsThreatened &&
            zone.Defenders.IsEmpty;

        /// <summary>
        /// Where a zone force with no site left to take stands: the hold-back point while <see cref="HoldsBack"/>, else
        /// the zone centre (a garrison, or the whole force while hostiles approach).
        /// </summary>
        public static Vector3 GetGuardPoint(EnemyZonePlan plan, EnemyZoneObjective zone, Vector3 centre,
            Vector3 holdBack) => HoldsBack(plan, zone) ? holdBack : centre;

        /// <summary>AllyCombined: ships are sent only while allied ships can reach the zone with them.</summary>
        public static bool WaitsForAllies(EnemyZonePlan plan, bool alliesCanReach) =>
            plan == EnemyZonePlan.AllyCombined && !alliesCanReach;
    }
}
