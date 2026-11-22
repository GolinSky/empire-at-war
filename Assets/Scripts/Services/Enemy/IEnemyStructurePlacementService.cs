using UnityEngine;

namespace EmpireAtWar.Services.Enemy
{
    public interface IEnemyStructurePlacementService
    {
        bool IsRecovering { get; }

        /// <summary>A miner may be bought: own base standing, no hostile near it, no turtle after a base loss.</summary>
        bool CanPlaceMiner => true;

        bool TryGetPosition(out Vector3 position);

        /// <summary>A held zone without a defend platform; <paramref name="allowExtra"/> also accepts a defended zone below its platform limit.</summary>
        bool TryGetDefensePosition(bool allowExtra, out Vector3 position) => TryGetPosition(out position);

        /// <summary>
        /// As <see cref="TryGetDefensePosition(bool, out Vector3)"/>; with <paramref name="firstOnly"/> only the first
        /// platform of the station ring or of a held zone is placed.
        /// </summary>
        bool TryGetDefensePosition(bool allowExtra, bool firstOnly, out Vector3 position) =>
            TryGetDefensePosition(allowExtra, out position);

        bool TryGetSecondStationPosition(out Vector3 position)
        {
            position = default;
            return false;
        }

        /// <summary>Nearest spot hidden by fog and free of blockers; a scout sent there can reveal build space.</summary>
        bool TryGetScoutTarget(out Vector3 position);

        void RecordPlacement(string request, Vector3 position);

        void RecordDeferred(string request, float refund);

        void RecordStationBuilt(Vector3 position);

        void RecordDestroyedPosition(Vector3 position);

        void Reset();
    }
}
