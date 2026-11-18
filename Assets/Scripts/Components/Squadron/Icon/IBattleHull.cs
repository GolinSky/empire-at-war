using UnityEngine;

namespace EmpireAtWar.Components.Squadrons.Icon
{
    /// <summary>A world hull box that squadron icon grids must not cover, so the ship stays clickable.</summary>
    public interface IBattleHull
    {
        bool IsActive { get; }
        Transform Transform { get; }
        Bounds LocalBounds { get; }
    }
}
