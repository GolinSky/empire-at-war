using EmpireAtWar.Services.Camera;
using UnityEngine;

namespace EmpireAtWar.Services.Tutorial
{
    /// <summary>Met after the camera was moved by keys, screen edge or drag for long enough to count as a deliberate pan.</summary>
    public sealed class CameraPannedHintCondition : IPolledHintCondition
    {
        private const float REQUIRED_SECONDS = 1f;
        private const float DRAG_FRAME_SECONDS = 1f / 60f;

        private readonly ICameraInput _input;
        private float _seconds;

        public bool IsMet => _seconds >= REQUIRED_SECONDS;

        public CameraPannedHintCondition(ICameraInput input)
        {
            _input = input;
            _input.Panned += HandlePanned;
        }

        public void Poll(float deltaTime)
        {
            if (_input.Move.sqrMagnitude > Mathf.Epsilon) _seconds += deltaTime;
        }

        private void HandlePanned(Vector2 delta) => _seconds += DRAG_FRAME_SECONDS;

        public void Dispose() => _input.Panned -= HandlePanned;
    }
}
