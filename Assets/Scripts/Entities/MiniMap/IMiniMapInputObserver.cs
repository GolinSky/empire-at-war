using System;

namespace EmpireAtWar.Entities.MiniMap
{
    /// <summary>Reports minimap input the player completed; read-only.</summary>
    public interface IMiniMapInputObserver
    {
        event Action CameraMoved;
        event Action MoveOrdered;
        bool TryGetWorldPoint(UnityEngine.Vector2 screenPoint, out UnityEngine.Vector3 worldPoint);
        UnityEngine.Vector2 WorldToScreenPoint(UnityEngine.Vector3 worldPoint);
    }
}
