namespace EmpireAtWar.Sandbox.Multiplayer
{
    public enum SpikeSyncMode : byte
    {
        // Every unit every tick (naive baseline).
        FullSnapshot = 0,
        // Keyframe only when host-side dead-reckoning error exceeds the threshold, or the refresh interval passes.
        DeadReckoning = 1
    }
}
