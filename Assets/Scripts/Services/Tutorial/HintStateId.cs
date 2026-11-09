namespace EmpireAtWar.Services.Tutorial
{
    /// <summary>
    /// A live game state a step needs (prerequisite) or is shown only in (skip unless). Read every frame; never latches.
    /// </summary>
    public enum HintStateId
    {
        None = 0,
        UnitSelected = 1,
        StationSelected = 2,
        AbilityUnitSelected = 3,
        AutoPowerOn = 4,
        AbilityNeedsMove = 5,
        AbilityNeedsTarget = 6,
        AbilityInstant = 7,
        /// <summary>A built mining facility waits in reinforcements or is already placed.</summary>
        MiningFacilityReady = 8,
        /// <summary>A built scout (squadron, fighter, interceptor) waits in reinforcements.</summary>
        ScoutReady = 9,
    }
}
