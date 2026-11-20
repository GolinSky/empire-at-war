using UnityEngine;

namespace EmpireAtWar.Services.Camera
{
    /// <summary>
    /// Speeds a held pan up after a delay: the multiplier stays at 1, then eases in
    /// (slow first, faster later) up to the maximum. Releasing the pan resets it.
    /// </summary>
    public sealed class CameraPanBoost
    {
        private float _heldTime;

        public void Reset()
        {
            _heldTime = 0f;
        }

        public float Step(bool isPanning, float delay, float rampDuration, float maxMultiplier, float deltaTime)
        {
            if (!isPanning)
            {
                _heldTime = 0f;
                return 1f;
            }

            _heldTime += deltaTime;
            float progress = Mathf.Clamp01((_heldTime - delay) / rampDuration);
            return Mathf.Lerp(1f, maxMultiplier, progress * progress);
        }
    }
}
