using System.Numerics;

namespace EmpireAtWar.Services.InputService
{
    public sealed class PointerGestureState
    {
        private const float DRAG_THRESHOLD = 5f;
        private Vector2 _previousPosition;
        private float _previousMagnitude;

        public bool IsPressed { get; private set; }
        public bool StartedOverUi { get; private set; }
        public bool HasDragged { get; private set; }
        public bool IsMouse { get; private set; }
        public Vector2 PressPosition { get; private set; }

        public void Begin(Vector2 position, bool isMouse, bool overUi)
        {
            IsPressed = true;
            HasDragged = false;
            IsMouse = isMouse;
            PressPosition = position;
            _previousPosition = position;
            StartedOverUi = overUi;
        }

        public Vector2 Move(Vector2 position)
        {
            Vector2 delta = position - _previousPosition;
            _previousPosition = position;
            return delta;
        }

        public bool TryStartDrag(Vector2 position)
        {
            if (HasDragged || Vector2.Distance(PressPosition, position) < DRAG_THRESHOLD) return false;
            HasDragged = true;
            return true;
        }

        public bool TryPinch(float magnitude, out float delta)
        {
            delta = _previousMagnitude - magnitude;
            bool hasPrevious = _previousMagnitude > 0f;
            _previousMagnitude = magnitude;
            return hasPrevious;
        }

        public void ResetPinch() => _previousMagnitude = 0f;

        public void End()
        {
            IsPressed = false;
            HasDragged = false;
            StartedOverUi = false;
            IsMouse = false;
            ResetPinch();
        }
    }
}
