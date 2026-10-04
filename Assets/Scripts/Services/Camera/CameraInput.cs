using System;
using EmpireAtWar.Services.Input;
using EmpireAtWar.Services.Settings;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Services.Camera
{
    public sealed class CameraInput : ICameraInput, ITickable
    {
        private const float EDGE_SCROLL_THICKNESS = 16f;
        private const float MAX_PAN_DELTA = 10f;

        private readonly IPointerInput _pointerInput;
        private readonly ICameraPreferences _preferences;

        private readonly GameInputActions.CameraActions _camera;
        private readonly GameInputActions.BattleActions _battle;

        public event Action<Vector2> Panned;

        public event Action<float> Zoomed;

        public Vector2 Move
        {
            get
            {
                // Before the first pointer report, or while unfocused, the position is not where the cursor is.
                Vector2 direction = _preferences.EdgeScrolling && _pointerInput.HasPosition && Application.isFocused
                    ? GetEdgeScrollDirection(_pointerInput.Position)
                    : Vector2.zero;
                // Ctrl+A selects units; its A key must not also move the camera.
                if (!_battle.SelectVisible.IsPressed() && !_battle.SelectAll.IsPressed())
                {
                    direction += _camera.Move.ReadValue<Vector2>();
                }

                return Vector2.ClampMagnitude(direction, 1f);
            }
        }

        public CameraInput(IPointerInput pointerInput, ICameraPreferences preferences, InputActionsProvider provider)
        {
            _preferences = preferences;
            _camera = provider.Actions.Camera;
            _battle = provider.Actions.Battle;
            _pointerInput = pointerInput;
        }

        public void Tick()
        {
            if (_camera.DragPan.IsPressed())
            {
                Vector2 dragDelta = _camera.DragDelta.ReadValue<Vector2>();
                Vector2 direction = new Vector2(
                    Mathf.Clamp(dragDelta.x, -MAX_PAN_DELTA, MAX_PAN_DELTA),
                    Mathf.Clamp(dragDelta.y, -MAX_PAN_DELTA, MAX_PAN_DELTA));
                if (direction.sqrMagnitude > Mathf.Epsilon)
                {
                    Panned?.Invoke(direction);
                }
            }

            RaiseZoom(_camera.Zoom.ReadValue<float>());
            RaiseZoom(_camera.ZoomScroll.ReadValue<float>());
        }

        private void RaiseZoom(float direction)
        {
            if (!Mathf.Approximately(direction, 0f))
            {
                Zoomed?.Invoke(direction);
            }
        }

        private static Vector2 GetEdgeScrollDirection(Vector2 position)
        {
            if (position.x < 0f || position.x > Screen.width ||
                position.y < 0f || position.y > Screen.height)
            {
                return Vector2.zero;
            }

            float horizontal = 0f;
            if (position.x <= EDGE_SCROLL_THICKNESS)
            {
                horizontal = -1f;
            }
            else if (position.x >= Screen.width - EDGE_SCROLL_THICKNESS)
            {
                horizontal = 1f;
            }

            float vertical = 0f;
            if (position.y <= EDGE_SCROLL_THICKNESS)
            {
                vertical = -1f;
            }
            else if (position.y >= Screen.height - EDGE_SCROLL_THICKNESS)
            {
                vertical = 1f;
            }

            return new Vector2(horizontal, vertical);
        }
    }
}
