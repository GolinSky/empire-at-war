using UnityEngine;

namespace EmpireAtWar.Components.Squadrons.Icon
{
    /// <summary>Critically damped screen-space offset; retargeting keeps position and velocity continuous.</summary>
    internal struct ScreenSpring
    {
        public Vector2 Value;
        public Vector2 Velocity;

        public void Step(Vector2 target, float smoothTime, float deltaTime) =>
            Value = Vector2.SmoothDamp(Value, target, ref Velocity, smoothTime, Mathf.Infinity, deltaTime);
    }
}
