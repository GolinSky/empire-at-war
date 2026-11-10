namespace EmpireAtWar.Services.Tutorial
{
    /// <summary>HUD element a hint highlights. Placed on prefabs through <c>TutorialAnchor</c>.</summary>
    public enum TutorialAnchorId
    {
        None = 0,
        MiniMap = 1,
        BuildPanel = 2,
        BuildQueue = 3,
        Money = 4,
        UnitActions = 5,
        MoveButton = 6,
        AttackMoveButton = 7,
        StopButton = 8,
        GuardButton = 9,
        AbilityBar = 10,
        AutoPowerButton = 11,
        TooltipMatchups = 12,
        ReinforcementButton = 13,
        SuperWeapons = 14,
        Heroes = 15,
        SurvivalHud = 16,
        LevelUpButton = 17,
        ResearchButton = 18,
        BuildShipButton = 19,
        ScoutShipButton = 20,
        AbilityButton = 21,
        PauseButton = 22,
        SpeedButton = 23,
        EconomyResearchButton = 24,
        FleetResearchButton = 25,
        NewShipButton = 26,
        MiningFacilityButton = 27,
        /// <summary>First unit card in the open reinforcement panel.</summary>
        ReinforcementUnit = 28,
        /// <summary>Card of the unit added to reinforcements last (the scout, the facility).</summary>
        ReinforcementNewUnit = 29,
        ReinforcementPanel = 30,
        /// <summary>Card of the mining facilities waiting in the open reinforcement panel.</summary>
        ReinforcementFacility = 31,
        /// <summary>First unit card holding two or more units: the one Shift+drag deploys together.</summary>
        ReinforcementStack = 32,
    }
}
