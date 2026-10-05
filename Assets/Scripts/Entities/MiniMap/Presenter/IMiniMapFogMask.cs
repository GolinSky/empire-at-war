using UnityEngine;

namespace EmpireAtWar.Presenters.MiniMap
{
    /// <summary>The local team's fog over the map area: red is 1 where the team sees, 0 under fog.
    /// UV (0, 0) is the map's minimum X/Z corner and (1, 1) its maximum.</summary>
    public interface IMiniMapFogMask
    {
        Texture Mask { get; }
    }
}
