using UnityEngine;

namespace EmpireAtWar.Services.Enemy
{
    public interface IEnemyStructurePlacementService
    {
        bool IsRecovering { get; }

        bool TryGetPosition(out Vector3 position);

        /// <summary>Nearest spot hidden by fog and free of blockers; a scout sent there can reveal build space.</summary>
        bool TryGetScoutTarget(out Vector3 position);

        void RecordPlacement(string request, Vector3 position);

        void RecordDeferred(string request, float refund);

        void RecordStationBuilt(Vector3 position);

        void RecordDestroyedPosition(Vector3 position);

        void Reset();
    }
}
