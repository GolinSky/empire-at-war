using Mirror;

namespace EmpireAtWar.Services.Networking.Online.Messages
{
    /// <summary>Host → client, reliable: sent after that client's baseline spawns; its loading screen may close.</summary>
    public struct BattleBeginMessage : NetworkMessage
    {
    }
}
