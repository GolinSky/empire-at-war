namespace EmpireAtWar.Services.ShipSpawning
{
    /// <summary>Result counters of one forward spawn search around own units.</summary>
    public struct ForwardSpawnAttempt
    {
        /// <summary>Index of the unit whose ring held the returned point; -1 when none did.</summary>
        public int UnitIndex;
        public int Points;
        /// <summary>Outside the map, on a hazard, unseen or inside a hostile block circle.</summary>
        public int NotOpen;
        public int NotClear;
    }
}
