namespace EmpireAtWar.Components.Ship.Health
{
    public static class ShipClassExtensions
    {
        /// <summary>Small squadron craft: fighters, bombers and interceptors.</summary>
        public static bool IsStrikecraft(this ShipClass shipClass) =>
            shipClass == ShipClass.Fighter || shipClass == ShipClass.Bomber || shipClass == ShipClass.Interceptor;
    }
}
