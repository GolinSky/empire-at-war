namespace EmpireAtWar.Services.Networking.Online
{
    /// <summary>One PC in the lobby. The host is connection 0. <see cref="SlotIndex"/> is -1 until the battle starts.</summary>
    public struct LobbyMember
    {
        public int ConnectionId;
        public string Name;
        public bool Ready;
        public int SlotIndex;
        public bool Connected;
    }
}
