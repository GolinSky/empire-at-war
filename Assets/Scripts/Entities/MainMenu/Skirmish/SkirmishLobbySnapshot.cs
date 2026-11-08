using System;

namespace EmpireAtWar.Entities.MainMenu.Skirmish
{
    /// <summary>The host's Skirmish setup screen, sent to every lobby client as JSON.</summary>
    [Serializable]
    public sealed class SkirmishLobbySnapshot
    {
        public SkirmishLobbySlotSnapshot[] slots;
        public int planet;
        public int mapSize;
        public int victoryCondition;
        public float startingMoney;
        public bool superWeapons;
    }
}
