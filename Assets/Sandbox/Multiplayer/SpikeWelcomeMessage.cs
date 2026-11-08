using Mirror;

namespace EmpireAtWar.Sandbox.Multiplayer
{
    // Host → joining client, reliable. Stands in for spawn events: side assignment + unit roster.
    public struct SpikeWelcomeMessage : NetworkMessage
    {
        public byte Side;
        public SpikeSyncMode Mode;
        public byte[] UnitSides;
        public SpikeUnitKind[] UnitKinds;
    }
}
