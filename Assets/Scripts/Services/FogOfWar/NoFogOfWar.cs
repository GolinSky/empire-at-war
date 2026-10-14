namespace EmpireAtWar.Services.FogOfWar
{
    /// <summary>Stands in for the fog when the battle has none: the whole map is already visible.</summary>
    public sealed class NoFogOfWar : IFogOfWarSystem
    {
        public void InitializeArea(float scale) { }

        public void RevealImmediately() { }
    }
}
