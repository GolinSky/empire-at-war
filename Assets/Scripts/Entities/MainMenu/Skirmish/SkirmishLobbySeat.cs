namespace EmpireAtWar.Entities.MainMenu.Skirmish
{
    /// <summary>One online lobby member as the setup screen needs it.</summary>
    public readonly struct SkirmishLobbySeat
    {
        public SkirmishLobbySeat(int memberId, string name, bool ready, ulong steamId)
        {
            MemberId = memberId;
            Name = name;
            Ready = ready;
            SteamId = steamId;
        }

        public int MemberId { get; }
        public string Name { get; }
        public bool Ready { get; }
        /// <summary>0 without Steam.</summary>
        public ulong SteamId { get; }
    }
}
