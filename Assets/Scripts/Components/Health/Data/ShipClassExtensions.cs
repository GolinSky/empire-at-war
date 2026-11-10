namespace EmpireAtWar.Components.Ship.Health
{
    public static class ShipClassExtensions
    {
        /// <summary>Small squadron craft: fighters, bombers, interceptors and hero strikecraft.</summary>
        public static bool IsStrikecraft(this ShipClass shipClass) =>
            shipClass == ShipClass.Fighter || shipClass == ShipClass.Bomber || shipClass == ShipClass.Interceptor ||
            shipClass == ShipClass.HeroStrikecraft;
    }
}
