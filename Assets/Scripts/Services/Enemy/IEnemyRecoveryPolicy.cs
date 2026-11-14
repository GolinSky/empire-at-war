using System.Collections.Generic;
using UnityEngine;

namespace EmpireAtWar.Services.Enemy
{
    public interface IEnemyRecoveryPolicy
    {
        bool IsActive { get; }
        string DeferredReason { get; }
        bool TryGetBuildAnchor(out Vector3 position);
        bool IsPlacementSafe(Vector3 position, float clearance);
        float GetNearestHostileDistance(Vector3 position);
        void CopySafeAlliedZoneBounds(List<Bounds> bounds);
        void OnStationBuilt(Vector3 position);
    }
}
