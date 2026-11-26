namespace EmpireAtWar.Entities.EnemyFaction.Models.Combat
{
    /// <summary>Where the hit-and-run cycle of a group stands (<see cref="EnemyStrikeCycleModel"/>).</summary>
    public enum EnemyStrikePhase
    {
        /// <summary>No cycle: every ship follows the state orders of the group.</summary>
        Idle = 0,
        /// <summary>The strike ships attack the target together.</summary>
        Strike = 1,
        /// <summary>The strike ships fly to the safe point together.</summary>
        PullBack = 2,
        /// <summary>The strike ships wait at the safe point for shields (and hull, when repair is available).</summary>
        Recover = 3
    }
}
