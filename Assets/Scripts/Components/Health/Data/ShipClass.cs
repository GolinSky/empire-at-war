namespace EmpireAtWar.Components.Ship.Health
{
    public enum ShipClass
    {
        Fighter = 0,
        Bomber = 1,
        Interceptor = 7,
        /// <summary>Hero strikecraft: resists ship guns and ordinary squadrons; hero lasers hit it normally.</summary>
        HeroStrikecraft = 9,
        Corvette = 2,
        Frigate = 3,
        Cruiser = 8,
        Capital = 4,
        HeavyCapital = 5,
        Structure = 6,
    }
}
