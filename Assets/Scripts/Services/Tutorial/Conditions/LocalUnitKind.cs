namespace EmpireAtWar.Services.Tutorial
{
    public enum LocalUnitKind
    {
        /// <summary>A unit whose faction data is a hero.</summary>
        Hero = 0,
        /// <summary>A movable ship with a hangar that launches squadrons.</summary>
        Carrier = 1,
        /// <summary>A ship with at least one ability.</summary>
        AbilityShip = 2,
    }
}
