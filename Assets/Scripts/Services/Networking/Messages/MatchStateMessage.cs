using Mirror;

namespace EmpireAtWar.Services.Networking.Messages
{
    /// <summary>Host → all, reliable: host-owned pause and speed; clients set <c>Time.timeScale</c> to match.</summary>
    public struct MatchStateMessage : NetworkMessage
    {
        public uint Tick;
        public float TimeScale;
    }
}
