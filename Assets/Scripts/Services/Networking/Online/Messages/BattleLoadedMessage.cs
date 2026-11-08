using Mirror;

namespace EmpireAtWar.Services.Networking.Online.Messages
{
    /// <summary>Client → host, reliable: the battle scene finished loading and its handlers are registered.</summary>
    public struct BattleLoadedMessage : NetworkMessage
    {
    }
}
