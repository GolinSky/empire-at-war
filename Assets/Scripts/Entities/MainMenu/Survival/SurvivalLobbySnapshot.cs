using System;

namespace EmpireAtWar.Entities.MainMenu.Survival
{
    /// <summary>The host's Survival setup screen, sent to every lobby client as JSON. Ally -1 = none.</summary>
    [Serializable]
    public sealed class SurvivalLobbySnapshot
    {
        public int playerFaction;
        public int allyFaction;
        public int enemyFaction;
        public int mapSize;
        public int difficulty;
        public bool superWeapons;
    }
}
