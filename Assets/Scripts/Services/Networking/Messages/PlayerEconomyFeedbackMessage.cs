using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Audio;
using EmpireAtWar.Controllers.Factions;
using Mirror;

namespace EmpireAtWar.Services.Networking.Messages
{
    /// <summary>A live, accepted player event. Baseline snapshots never contain announcements.</summary>
    public struct PlayerEconomyFeedbackMessage : NetworkMessage
    {
        public PlayerId Owner;
        public FactionFeedbackKind Kind;
        public UnitRequestKind RequestKind;
        public int RequestKey;
        public int RequestTier;
        public int UnitCapacity;
        public int StationLevel;
    }
}
