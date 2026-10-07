using UnityEngine;

namespace EmpireAtWar.Services.Enemy
{
    public interface IEnemyStructurePlacementService
    {
        bool TryGetPosition(out Vector3 position);

        /// <summary>Nearest spot hidden by fog and free of blockers; a scout sent there can reveal build space.</summary>
        bool TryGetScoutTarget(out Vector3 position);

        void RecordDestroyedPosition(Vector3 position);

        void Reset();
    }
}
