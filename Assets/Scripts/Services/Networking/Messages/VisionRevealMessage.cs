using EmpireAtWar.Models.Players;
using Mirror;

namespace EmpireAtWar.Services.Networking.Messages
{
    /// <summary>Host → clients, reliable: firing exposes a ship to the attacked player's team.</summary>
    public struct VisionRevealMessage : NetworkMessage
    {
        public PlayerId Viewer;
        public long EntityId;
        public bool Active;
    }
}
