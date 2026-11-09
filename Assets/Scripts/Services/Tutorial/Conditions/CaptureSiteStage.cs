namespace EmpireAtWar.Services.Tutorial
{
    public enum CaptureSiteStage
    {
        /// <summary>The site is revealed to the player.</summary>
        Revealed = 0,
        /// <summary>The local player owns it, built on or not.</summary>
        Captured = 1,
        /// <summary>The local player started a facility on it.</summary>
        Building = 2,
        /// <summary>A local mining facility is running.</summary>
        MiningOperational = 3,
    }
}
