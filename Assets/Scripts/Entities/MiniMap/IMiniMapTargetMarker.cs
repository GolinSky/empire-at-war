using UnityEngine;

namespace EmpireAtWar.Entities.MiniMap
{
    /// <summary>Marks one world point on the minimap (station, capture site, ship) until cleared.</summary>
    public interface IMiniMapTargetMarker
    {
        void ShowTarget(Vector3 worldPoint);

        void HideTarget();
    }
}
