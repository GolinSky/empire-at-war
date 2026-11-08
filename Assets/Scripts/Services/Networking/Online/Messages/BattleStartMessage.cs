using Mirror;

namespace EmpireAtWar.Services.Networking.Online.Messages
{
    /// <summary>
    /// Host → one client, reliable: the battle setup (<see cref="EmpireAtWar.Entities.Game.BattleSetupSave"/> JSON,
    /// map seed included) and the slot this PC controls. Also sent to a reconnecting player.
    /// </summary>
    public struct BattleStartMessage : NetworkMessage
    {
        public string SetupJson;
        public int LocalSlotIndex;
    }
}
