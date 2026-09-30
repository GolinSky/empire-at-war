using System.Collections.Generic;
using UnityEngine;

namespace EmpireAtWar.Services.Graphics
{
    public interface IDisplayOptions
    {
        /// <summary>Distinct supported output sizes of the current monitor, largest first.</summary>
        IReadOnlyList<Vector2Int> Resolutions { get; }

        Vector2Int DesktopResolution { get; }
    }
}
