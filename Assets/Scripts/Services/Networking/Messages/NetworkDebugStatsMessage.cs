using Mirror;

namespace EmpireAtWar.Services.Networking.Messages
{
    /// <summary>
    /// Host → all, reliable, 1 Hz: host numbers for the client's network status overlay and CSV.
    /// <see cref="SendTime"/> is host <c>NetworkTime.time</c>, the clock keyframes carry, so the client can
    /// tell how far the reliable stream trails the unreliable one.
    /// </summary>
    public struct NetworkDebugStatsMessage : NetworkMessage
    {
        public double SendTime;
        public float HostFps;
        public float TickRate;
        public float ShotsPerSecond;
        public float KeyframeChunksPerSecond;
        public int EntityCount;
    }
}
