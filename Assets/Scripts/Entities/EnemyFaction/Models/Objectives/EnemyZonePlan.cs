namespace EmpireAtWar.Entities.EnemyFaction.Models.Objectives
{
    /// <summary>What the AI does with a zone it takes (W47, decision D4). Picked from the zone's caution level.</summary>
    public enum EnemyZonePlan
    {
        /// <summary>
        /// Take the zone; the capturing force stays while the site construction runs and until reinforcements could
        /// arrive (<see cref="EnemyZoneAllocator.GARRISON_DWELL_SECONDS"/>), and all of it while hostiles approach. Then
        /// the zone keeps only its garrison.
        /// </summary>
        TakeAndHold = 0,
        /// <summary>
        /// The zone was retaken and hostiles can drop beside it: the garrison waits just outside the zone
        /// (<c>EnemyStrategicContextBuilder.GetPoolHoldBackPoint</c>) and hits the retake.
        /// </summary>
        HoldBackAndCounter = 1,
        /// <summary>
        /// An important zone after repeated failed captures: take it only together with an allied AI. Flag only; the
        /// combined attack is not executed yet.
        /// </summary>
        AllyCombined = 2,
        /// <summary>The garrison it needs is worth more than the zone: take it and leave no ship behind.</summary>
        RaidAndLeave = 3,
        /// <summary>"Not now": the zone is denied for <see cref="EnemyZoneCaution.SKIP_SECONDS"/>.</summary>
        Skip = 4
    }
}
