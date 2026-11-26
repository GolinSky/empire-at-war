namespace EmpireAtWar.Entities.Units
{
    public static class ShipClassExtensions
    {
        /// <summary>Small squadron craft: fighters, bombers, interceptors and hero strikecraft.</summary>
        public static bool IsStrikecraft(this ShipClass shipClass) =>
            shipClass == ShipClass.Fighter || shipClass == ShipClass.Bomber || shipClass == ShipClass.Interceptor ||
            shipClass == ShipClass.HeroStrikecraft;

        /// <summary>Hull size order, Corvette 1 to Dreadnought 7. Strikecraft and structures have no tier (0).</summary>
        public static int HullTier(this ShipClass shipClass) => shipClass switch
        {
            ShipClass.Corvette => 1,
            ShipClass.Frigate => 2,
            ShipClass.Cruiser => 3,
            ShipClass.HeavyCruiser => 4,
            ShipClass.Capital => 5,
            ShipClass.BattleCruiser => 6,
            ShipClass.Dreadnought => 7,
            _ => 0
        };
    }
}
