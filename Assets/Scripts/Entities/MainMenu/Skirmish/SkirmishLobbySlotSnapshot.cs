using System;

namespace EmpireAtWar.Entities.MainMenu.Skirmish
{
    /// <summary>One row of <see cref="SkirmishLobbySnapshot"/>.</summary>
    [Serializable]
    public sealed class SkirmishLobbySlotSnapshot
    {
        public int occupant;
        public int faction;
        public int team;
        public int color;
        public int memberId;
        public string name;
        public bool ready;
    }
}
