using UnityEngine;

namespace EmpireAtWar.Presenters.MiniMap
{
    /// <summary>Minimap mask for a battle without fog: white means the local team sees everywhere.</summary>
    public sealed class RevealedMiniMapFogMask : IMiniMapFogMask
    {
        public Texture Mask => Texture2D.whiteTexture;
    }
}
