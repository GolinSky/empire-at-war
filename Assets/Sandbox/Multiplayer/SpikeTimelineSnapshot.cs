using Mirror;

namespace EmpireAtWar.Sandbox.Multiplayer
{
    // One entry per received host tick; drives the shared client render timeline via Mirror SnapshotInterpolation.
    public struct SpikeTimelineSnapshot : Snapshot
    {
        public double remoteTime { get; set; }
        public double localTime { get; set; }

        public SpikeTimelineSnapshot(double remoteTime, double localTime)
        {
            this.remoteTime = remoteTime;
            this.localTime = localTime;
        }
    }
}
