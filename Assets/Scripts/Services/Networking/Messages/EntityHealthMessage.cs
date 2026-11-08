using Mirror;

namespace EmpireAtWar.Services.Networking.Messages
{
    /// <summary>Host → all, reliable: a unit's health changed (<see cref="EmpireAtWar.Services.Saves.UnitHealthSave"/> JSON).</summary>
    public struct EntityHealthMessage : NetworkMessage
    {
        public ushort Index;
        public string HealthJson;
    }
}
