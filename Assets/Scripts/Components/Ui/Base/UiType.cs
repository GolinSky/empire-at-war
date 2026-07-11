using System;

namespace EmpireAtWar.Ui.Base
{
    [Serializable]
    public enum UiType
    {
        Reinforcement = 0,
        Faction = 1,
        Economy = 2,
        CoreGame = 3,
        MiniMap = 4,
        Ship = 5,
        // Navigation = 6, //deleted
        Menu = 7,
        Interaction = 8,
    }
}