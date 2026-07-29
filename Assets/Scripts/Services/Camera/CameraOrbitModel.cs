using System;

namespace EmpireAtWar.Services.Camera
{
    public sealed class CameraOrbitModel
    {
        private readonly float _defaultPitch;
        private readonly float _defaultYaw;
        private readonly float _minPitch;
        private readonly float _maxPitch;

        public float Pitch { get; private set; }
        public float Yaw { get; private set; }

        public CameraOrbitModel(
            float defaultPitch,
            float defaultYaw,
            float minPitch,
            float maxPitch)
        {
            if (minPitch > maxPitch)
            {
                throw new ArgumentException(
                    "Minimum pitch cannot be greater than maximum pitch.",
                    nameof(minPitch));
            }

            _minPitch = minPitch;
            _maxPitch = maxPitch;
            _defaultPitch = Clamp(defaultPitch, minPitch, maxPitch);
            _defaultYaw = NormalizeAngle(defaultYaw);
            Reset();
        }

        public void Rotate(float pitchDelta, float yawDelta)
        {
            Pitch = Clamp(Pitch + pitchDelta, _minPitch, _maxPitch);
            Yaw = NormalizeAngle(Yaw + yawDelta);
        }

        public void Reset()
        {
            Pitch = _defaultPitch;
            Yaw = _defaultYaw;
        }

        private static float Clamp(float value, float min, float max)
        {
            return Math.Max(min, Math.Min(max, value));
        }

        private static float NormalizeAngle(float angle)
        {
            angle %= 360f;
            return angle < 0f ? angle + 360f : angle;
        }
    }
}
