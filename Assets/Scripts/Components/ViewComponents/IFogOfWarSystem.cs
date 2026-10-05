namespace ViewComponents
{
    /// <summary>Draws the local team's vision. Visibility queries go through <c>IVisionService</c>.</summary>
    public interface IFogOfWarSystem
    {
        /// <summary>Shows the current vision at once instead of fading it in.</summary>
        void RevealImmediately();
    }
}
