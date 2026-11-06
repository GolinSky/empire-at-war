using UnityEngine;

namespace EmpireAtWar.Entities.MiniMap
{
    /// <summary>Map-aligned visibility mask. Red is 1 where the overlay is transparent.
    /// UV (0, 0) is the map's minimum X/Z corner and (1, 1) its maximum.</summary>
    public interface IMiniMapOverlaySource
    {
        Texture Mask { get; }
    }
}
