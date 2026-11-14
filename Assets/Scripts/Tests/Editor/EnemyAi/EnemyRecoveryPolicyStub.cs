using System.Collections.Generic;
using EmpireAtWar.Services.Enemy;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    internal sealed class EnemyRecoveryPolicyStub : IEnemyRecoveryPolicy
    {
        public bool IsActive { get; set; }
        public string DeferredReason => "noAnchor";
        public bool TryGetBuildAnchor(out Vector3 position) { position = default; return false; }
        public bool IsPlacementSafe(Vector3 position, float clearance) => true;
        public float GetNearestHostileDistance(Vector3 position) => float.PositiveInfinity;
        public void CopySafeAlliedZoneBounds(List<Bounds> bounds) => bounds.Clear();
        public void OnStationBuilt(Vector3 position) { }
    }
}
