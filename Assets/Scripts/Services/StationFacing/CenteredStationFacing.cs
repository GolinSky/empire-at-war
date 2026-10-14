using EmpireAtWar.Models.Players;
using UnityEngine;

namespace EmpireAtWar.Services.StationFacing
{
    public sealed class CenteredStationFacing : IStationFacingService
    {
        public Quaternion GetRotation(PlayerId owner) => Quaternion.identity;
    }
}
