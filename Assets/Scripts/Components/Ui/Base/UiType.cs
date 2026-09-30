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
        ShipBuild = 6,
        PauseMenu = 7,
        Interaction = 8,
        MainMenu = 9,
        ShipGroup = 10,
        UnitOrderFeedback = 11,
        Skirmish = 12,
        Settings = 13,
        Fps = 14,
        Tooltip = 15,
    }
}
