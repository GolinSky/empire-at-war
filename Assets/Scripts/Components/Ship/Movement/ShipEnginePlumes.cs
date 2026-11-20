using UnityEngine;

namespace EmpireAtWar.Components.Ship.Movement
{
    /// <summary>
    /// Stretches the ship's local-space engine plumes with throttle. Plume width comes from the
    /// instance scale; only the line length changes here.
    /// </summary>
    internal sealed class ShipEnginePlumes
    {
        private const float FULL_WIDTH_LENGTH = 1.96f;
        private const float TIP_LENGTH = 14f;
        private const float THROTTLE_STEP = 0.01f;

        private readonly LineRenderer[] _plumes;
        private float _throttle = -1f;

        public ShipEnginePlumes(LineRenderer[] plumes)
        {
            _plumes = plumes;
        }

        public void SetThrottle(float fraction)
        {
            float throttle = Mathf.Clamp01(fraction);
            bool isOn = throttle > 0f;
            if (isOn == _throttle > 0f && Mathf.Abs(throttle - _throttle) < THROTTLE_STEP) return;
            _throttle = throttle;
            Vector3 fullWidthEnd = new Vector3(0f, 0f, -FULL_WIDTH_LENGTH * throttle);
            Vector3 tip = new Vector3(0f, 0f, -TIP_LENGTH * throttle);
            foreach (LineRenderer plume in _plumes)
            {
                plume.SetPosition(1, fullWidthEnd);
                plume.SetPosition(2, tip);
                plume.enabled = isOn;
            }
        }
    }
}
