using Mirror;

namespace EmpireAtWar.Services.Networking
{
    /// <summary>One entry per received host tick; drives the client render timeline.</summary>
    public struct TickTimelineSnapshot : Snapshot
    {
        public double remoteTime { get; set; }
        public double localTime { get; set; }

        public TickTimelineSnapshot(double remoteTime, double localTime)
        {
            this.remoteTime = remoteTime;
            this.localTime = localTime;
        }
    }
}
