using System;
using EmpireAtWar.Entities.EnemyFaction.Models;

namespace EmpireAtWar.Entities.Ship.Mediator
{
    public enum ShipAiDecision
    {
        Idle = 0,
        Navigate = 1,
        Attack = 2,
        Flee = 3
    }

    public readonly struct ShipAiSnapshot
    {
        public ShipAiSnapshot(
            bool isDestroyed,
            bool hasShields,
            float shieldPercentage,
            int nearbyEnemyCount,
            bool hasAssignedTarget,
            bool isAssignedTargetAvailable,
            bool isMoving)
        {
            IsDestroyed = isDestroyed;
            HasShields = hasShields;
            ShieldPercentage = shieldPercentage;
            NearbyEnemyCount = nearbyEnemyCount;
            HasAssignedTarget = hasAssignedTarget;
            IsAssignedTargetAvailable = isAssignedTargetAvailable;
            IsMoving = isMoving;
        }

        public bool IsDestroyed { get; }
        public bool HasShields { get; }
        public float ShieldPercentage { get; }
        public int NearbyEnemyCount { get; }
        public bool HasAssignedTarget { get; }
        public bool IsAssignedTargetAvailable { get; }
        public bool IsMoving { get; }
    }

    public sealed class ShipAiDecisionModel
    {
        public ShipAiDecision Evaluate(ShipAiSnapshot snapshot, EnemyAiDifficulty difficulty)
        {
            if (snapshot.NearbyEnemyCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(snapshot.NearbyEnemyCount));
            }

            EnemyAiDifficultyProfile profile = EnemyAiDifficultyProfile.Get(difficulty);
            if (snapshot.IsDestroyed)
            {
                return ShipAiDecision.Idle;
            }

            if (snapshot.HasShields && snapshot.ShieldPercentage < profile.RetreatShieldThreshold)
            {
                return ShipAiDecision.Flee;
            }

            if (snapshot.NearbyEnemyCount > profile.OutnumberedRetreatCount)
            {
                return ShipAiDecision.Flee;
            }

            if (snapshot.HasAssignedTarget && snapshot.IsAssignedTargetAvailable)
            {
                return ShipAiDecision.Attack;
            }

            return snapshot.IsMoving ? ShipAiDecision.Navigate : ShipAiDecision.Idle;
        }
    }
}
