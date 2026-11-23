namespace EmpireAtWar.Entities.EnemyFaction.Models
{
    /// <summary>What a force newly sent to a zone does this cycle (W68 scout first).</summary>
    public enum EnemyScoutStep
    {
        /// <summary>The zone's defenders are rated, or the wait ran out: the force goes.</summary>
        Commit = 0,
        /// <summary>Stale intel: the force waits while a scouting wing looks.</summary>
        WaitForWing = 1,
        /// <summary>No sighting after the wing head start: the fastest ship goes alone, the rest still wait.</summary>
        ShipScouts = 2
    }
}
