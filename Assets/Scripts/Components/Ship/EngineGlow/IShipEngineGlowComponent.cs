namespace EmpireAtWar.Components.Ship.EngineGlow
{
    public interface IShipEngineGlowComponent
    {
        /// <summary>Starts a GPU fade from the brightness on screen towards <paramref name="state"/>.</summary>
        void SetState(EngineGlowState state);
    }
}
