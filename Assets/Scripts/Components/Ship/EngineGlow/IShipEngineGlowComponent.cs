namespace EmpireAtWar.Components.Ship.EngineGlow
{
    public interface IShipEngineGlowComponent
    {
        /// <summary>Starts a GPU fade from the brightness on screen towards <paramref name="state"/>.</summary>
        void SetState(EngineGlowState state);

        /// <summary>
        /// A destroyed engine hardpoint fades its own glows off and keeps them off whatever the ship's state;
        /// other engines' glows are unaffected.
        /// </summary>
        void SetEngineDestroyed(int engineHardPointIndex, bool destroyed);
    }
}
