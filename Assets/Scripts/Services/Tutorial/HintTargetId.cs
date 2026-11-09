namespace EmpireAtWar.Services.Tutorial
{
    /// <summary>Place in the world a step marks with the ring, chevron and minimap dot.</summary>
    public enum HintTargetId
    {
        None = 0,
        /// <summary>Where the lesson's trigger happened (the hero, the hazard ship, the carrier).</summary>
        Trigger = 1,
        /// <summary>A local movable unit (ship or squadron), nearest the station; same unit while it lives.</summary>
        OwnUnit = 2,
        OwnStation = 3,
        /// <summary>The mining site the player is taking: own unbuilt site first, else the free one nearest the station.</summary>
        MiningSite = 4,
        OwnMine = 5,
        /// <summary>Like <see cref="MiningSite"/>, for satellite pads.</summary>
        SatelliteSite = 6,
        /// <summary>A free mining site near the map edge (not the center), nearest the station: the scout's goal.</summary>
        FoggedMiningSite = 7,
        /// <summary>The local movable unit that arrived last (highest entity id), such as a just deployed scout.</summary>
        NewestUnit = 8,
    }
}
