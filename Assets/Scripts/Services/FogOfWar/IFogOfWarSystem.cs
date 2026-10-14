namespace EmpireAtWar.Services.FogOfWar
{
    /// <summary>Draws the local team's vision. Visibility queries go through <c>IVisionService</c>.</summary>
    public interface IFogOfWarSystem
    {
        /// <summary>Sizes the fog to the generated map; <paramref name="scale"/> is map side / authored side.</summary>
        void InitializeArea(float scale);

        /// <summary>Shows the current vision at once instead of fading it in.</summary>
        void RevealImmediately();
    }
}
