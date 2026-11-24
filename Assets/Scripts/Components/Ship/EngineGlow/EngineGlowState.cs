namespace EmpireAtWar.Components.Ship.EngineGlow
{
    /// <summary>Engine glow brightness the GPU fades towards. Values are the shader's 2-bit state field.</summary>
    public enum EngineGlowState
    {
        Thrust = 0,
        Idle = 1,
        Off = 2
    }
}
