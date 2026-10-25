using System;
using System.Collections.Generic;
using EmpireAtWar.Models.Players;
using UnityEngine;

namespace EmpireAtWar.Services.ReinforcementZones
{
    public interface IReinforcementZonesSystem
    {
        event Action OwnershipChanged;

        bool IsPositionInAnyZone(Vector3 position, float clearance = 0f);

        void CopyOwnedCapturableZoneBounds(PlayerId owner, List<Bounds> destination);

        int GetOwnedCapturableZoneCount(PlayerId owner);

        bool TryGetDefaultZoneCenter(PlayerId owner, out Vector3 position);

        bool TryGetDefaultZoneExitPosition(
            PlayerId owner,
            Vector3 shipPosition,
            float shipRadius,
            out Vector3 position);

        bool TryGetCaptureTarget(PlayerId owner, Vector3 origin, out Vector3 position);
    }
}
