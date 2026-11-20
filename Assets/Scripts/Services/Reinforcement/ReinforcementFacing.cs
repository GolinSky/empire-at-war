using UnityEngine;

namespace EmpireAtWar.Services.Reinforcement
{
    /// <summary>World heading of reinforcement previews on the battle plane. It follows the automatic heading
    /// until the player turns it; a manual heading survives moving the preview and later placements until reset.</summary>
    public sealed class ReinforcementFacing
    {
        private const float STEP_DEGREES = 15f;
        // A held turn key starts sweeping after this delay, so a tap stays one exact step.
        private const float HOLD_DELAY_SECONDS = 0.25f;
        private const float HOLD_DEGREES_PER_SECOND = 90f;
        private const float FULL_TURN_DEGREES = 360f;

        private float? _manualYaw;
        private float _holdSeconds;

        public float GetYaw(float autoYaw) => _manualYaw ?? autoYaw;

        /// <summary>Turns toward a ground point; inside the dead zone the heading is kept,
        /// so a right-click without a drag changes nothing.</summary>
        public void Aim(Vector3 anchor, Vector3 target, float deadZone)
        {
            Vector3 direction = target - anchor;
            direction.y = 0f;
            if (direction.sqrMagnitude < deadZone * deadZone) return;
            _manualYaw = Quaternion.LookRotation(direction, Vector3.up).eulerAngles.y;
        }

        /// <summary>-1 turns left, +1 right: one step on the press, then a steady sweep while held.</summary>
        public void Turn(int direction, bool wasPressed, float deltaTime, float autoYaw)
        {
            if (direction == 0)
            {
                _holdSeconds = 0f;
                return;
            }

            float degrees;
            if (wasPressed)
            {
                _holdSeconds = 0f;
                degrees = STEP_DEGREES;
            }
            else
            {
                _holdSeconds += deltaTime;
                if (_holdSeconds < HOLD_DELAY_SECONDS) return;
                degrees = HOLD_DEGREES_PER_SECOND * deltaTime;
            }

            _manualYaw = Mathf.Repeat(GetYaw(autoYaw) + direction * degrees, FULL_TURN_DEGREES);
        }

        public void Reset()
        {
            _manualYaw = null;
            _holdSeconds = 0f;
        }
    }
}
