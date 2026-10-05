using UnityEngine;

namespace EmpireAtWar.Services.Reinforcement
{
    /// <summary>The single physical clearance rule for placing a structure, shared by players and AI.</summary>
    public interface IStructureSpawnClearance
    {
        /// <summary>Planar radius that encloses every placeable structure's footprint.</summary>
        float Radius { get; }

        /// <summary>True when no unit or obstacle collider lies within <see cref="Radius"/> of the point.</summary>
        bool IsClear(Vector3 position);
    }
}
