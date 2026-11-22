namespace EmpireAtWar.Services.Networking.Diagnostics
{
    /// <summary>One projectile pool with a salvo waiting, in one network status window.</summary>
    public readonly struct ShotPoolSample
    {
        public ShotPoolSample(string prefab, bool isFighters, int waiting, int inUse, int total, int runtimeCreated,
            float oldestWaitSeconds, float windowMaxWaitSeconds)
        {
            Prefab = prefab;
            IsFighters = isFighters;
            Waiting = waiting;
            InUse = inUse;
            Total = total;
            RuntimeCreated = runtimeCreated;
            OldestWaitSeconds = oldestWaitSeconds;
            WindowMaxWaitSeconds = windowMaxWaitSeconds;
        }

        public string Prefab { get; }
        public bool IsFighters { get; }
        public int Waiting { get; }
        public int InUse { get; }
        public int Total { get; }
        public int RuntimeCreated { get; }
        public float OldestWaitSeconds { get; }
        public float WindowMaxWaitSeconds { get; }
    }
}
