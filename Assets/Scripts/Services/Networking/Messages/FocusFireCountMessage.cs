using Mirror;

namespace EmpireAtWar.Services.Networking.Messages
{
    /// <summary>Host → one client: its team's focus fire holders on one target.</summary>
    public struct FocusFireCountMessage : NetworkMessage
    {
        public long TargetId;
        public int Ships;
        public int Squadrons;
    }
}
