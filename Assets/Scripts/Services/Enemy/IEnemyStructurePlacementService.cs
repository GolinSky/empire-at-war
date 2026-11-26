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

        /// <summary>
        /// As <see cref="TryGetDefensePosition(bool, bool, out Vector3)"/>; a <paramref name="bigPlatform"/> (Golan III,
        /// Empress) also needs every known hostile beyond the big-platform stand-off range (W50).
        /// </summary>
        bool TryGetDefensePosition(bool allowExtra, bool firstOnly, bool bigPlatform, out Vector3 position) =>
            TryGetDefensePosition(allowExtra, firstOnly, out position);

        /// <summary>Beside an own armed platform that no own repairer covers yet.</summary>
        bool TryGetRepairPlatformPosition(out Vector3 position)
        {
            position = default;
            return false;
        }

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
