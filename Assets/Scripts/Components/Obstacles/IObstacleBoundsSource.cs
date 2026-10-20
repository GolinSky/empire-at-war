using UnityEngine;

namespace EmpireAtWar.Components.Obstacles
{
    public interface IObstacleBoundsSource
    {
        Bounds WorldBounds { get; }
    }
}
