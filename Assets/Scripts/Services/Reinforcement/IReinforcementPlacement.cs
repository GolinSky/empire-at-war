using EmpireAtWar.Entities.Reinforcement;
using UnityEngine;

namespace EmpireAtWar.Services.Reinforcement
{
    /// <summary>The rules for placing one kind of reinforcement: its preview, where it may go and how it spawns.</summary>
    public interface IReinforcementPlacement
    {
        /// <summary>False for structures, which keep their fixed spawn rotation and ignore any heading.</summary>
        bool IsRotatable { get; }

        UnitSpawnView CreatePreview();

        /// <summary>Default heading at a point: toward the enemy spawn. Only valid when <see cref="IsRotatable"/>.</summary>
        Quaternion GetAutoFacing(Vector3 position);

        bool IsPositionValid(Vector3 position, Quaternion rotation);

        void Spawn(Vector3 position, Quaternion rotation);
    }
}
