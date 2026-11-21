using UnityEngine;

namespace EmpireAtWar.Services.Enemy
{
    /// <summary>When an AI ship keeps the attack target it has instead of taking the force's new one.</summary>
    public static class EnemyAttackTargetRules
    {
        /// <summary>A target farther than this many times the ship's reach is out of reach.</summary>
        public const float REACH_FACTOR = 1.5f;

        /// <summary>
        /// The ship keeps its target until it is destroyed, cloaked or farther than <see cref="REACH_FACTOR"/> times
        /// <paramref name="reach"/> (weapon range plus the target's radius). No reach means nothing to keep.
        /// </summary>
        public static bool KeepsTarget(bool isDestroyed, bool isCloaked, Vector3 shipPosition, Vector3 targetPosition,
            float reach)
        {
            if (isDestroyed || isCloaked || reach <= 0f) return false;
            float x = shipPosition.x - targetPosition.x;
            float z = shipPosition.z - targetPosition.z;
            float keep = REACH_FACTOR * reach;
            return x * x + z * z <= keep * keep;
        }
    }
}
