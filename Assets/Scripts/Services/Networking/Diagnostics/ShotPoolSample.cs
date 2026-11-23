namespace EmpireAtWar.Services.Networking.Diagnostics
{
    /// <summary>One projectile pool that fired shots without a visual, or still grows for them, in one window.</summary>
    public readonly struct ShotPoolSample
    {
        public ShotPoolSample(string prefab, bool isFighters, int pending, int skipped, int inUse, int total,
            int runtimeCreated)
        {
            Prefab = prefab;
            IsFighters = isFighters;
            Pending = pending;
            Skipped = skipped;
            InUse = inUse;
            Total = total;
            RuntimeCreated = runtimeCreated;
        }

        public string Prefab { get; }
        public bool IsFighters { get; }
        /// <summary>Visuals still wanted by shots that fired undrawn.</summary>
        public int Pending { get; }
        /// <summary>Shots fired without a visual in this window.</summary>
        public int Skipped { get; }
        public int InUse { get; }
        public int Total { get; }
        public int RuntimeCreated { get; }
    }
}
