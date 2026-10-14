using UnityEngine;

namespace EmpireAtWar.Services.Camera
{
    /// <summary>
    /// EaW-style dolly zoom: input moves a clamped target height in view-ray distance units,
    /// and the camera follows it with a critically damped spring so new input redirects smoothly.
    /// </summary>
    public sealed class CameraZoom
    {
        private float _velocity;

        public float TargetHeight { get; private set; }

        public void Reset(float height)
        {
            TargetHeight = height;
            _velocity = 0f;
        }

        /// <param name="viewDistance">Distance along the view ray; positive moves the camera away.</param>
        /// <param name="pitchSine">Height gained per unit of view-ray distance.</param>
        public void Dolly(float viewDistance, float pitchSine, float minHeight, float maxHeight)
        {
            TargetHeight = Mathf.Clamp(TargetHeight + viewDistance * pitchSine, minHeight, maxHeight);
        }

        public float Step(float currentHeight, float smoothTime, float deltaTime)
        {
            return Mathf.SmoothDamp(currentHeight, TargetHeight, ref _velocity, smoothTime, Mathf.Infinity, deltaTime);
        }
    }
}
