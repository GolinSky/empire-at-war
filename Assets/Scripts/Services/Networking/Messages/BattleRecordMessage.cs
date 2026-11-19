using Mirror;

namespace EmpireAtWar.Services.Networking.Messages
{
    /// <summary>Host → joining client, reliable: battle time and losses so far (<see cref="BattleRecordSnapshot"/> JSON).</summary>
    public struct BattleRecordMessage : NetworkMessage
    {
        public string Json;
    }
}
