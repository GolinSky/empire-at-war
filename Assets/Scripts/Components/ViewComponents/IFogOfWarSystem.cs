using UnityEngine;

namespace ViewComponents
{
    /// <summary>Draws the local team's vision. Visibility queries go through <c>IVisionService</c>.</summary>
    public interface IFogOfWarSystem
    {
        /// <summary>Shows the current vision at once instead of fading it in.</summary>
        void RevealImmediately();

        /// <summary>The live fog mask: red is 1 where the local team sees, 0 under fog.</summary>
        Texture Mask { get; }

        /// <summary>Mask UV rectangle covering the world XZ rectangle; width or height is negative when the mask is mirrored.</summary>
        Rect GetMaskUvRect(Vector2 worldMin, Vector2 worldMax);
    }
}
