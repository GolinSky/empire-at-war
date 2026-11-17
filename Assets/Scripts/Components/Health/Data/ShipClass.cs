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
        HeavyCruiser = 10,
        Capital = 4,
        BattleCruiser = 5,
        Dreadnought = 11,
        Structure = 6,
    }
}
