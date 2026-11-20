using EmpireAtWar.Models.Players;
using UnityEngine;

namespace EmpireAtWar.Services.StationFacing
{
    public sealed class CenteredStationFacing : IStationFacingService
    {
        public Quaternion GetRotation(PlayerId owner) => Quaternion.identity;

        // Survival waves have no enemy spawn, so reinforcements keep the station's fixed heading.
        public Quaternion GetEnemyFacing(PlayerId owner, Vector3 position) => Quaternion.identity;
    }
}
