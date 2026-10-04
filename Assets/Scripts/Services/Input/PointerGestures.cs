using System;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Services.Input
{
    public sealed class PointerGestures : IPointerGestures, ITickable
    {
        private readonly IPointerInput _pointerInput;
        private readonly IUiHitTest _uiHitTest;

        private readonly MouseDragState _drag = new MouseDragState();

        private readonly GameInputActions.BattleActions _battle;

        public event Action<Vector2> WorldPressed;

        public event Action<Vector2> WorldClicked;

        public event Action<Vector2> WorldCommanded;

        public event Action<Vector2> DragStarted;

        public event Action<Vector2> DragChanged;

        public event Action<Vector2> DragEnded;

        public PointerGestures(IPointerInput pointerInput, IUiHitTest uiHitTest, InputActionsProvider provider)
        {
            _battle = provider.Actions.Battle;
            _pointerInput = pointerInput;
            _uiHitTest = uiHitTest;
        }

        public void Tick()
        {
            if (!_battle.enabled)
            {
                _drag.End();
                return;
            }

            Vector2 position = _pointerInput.Position;
            if (_battle.Select.WasPressedThisFrame())
            {
                Press(position);
            }

            if (_drag.IsPressed && !_drag.StartedOverUi)
            {
                Track(position);
            }

            if (_battle.Select.WasReleasedThisFrame())
            {
                Release(position);
            }

            if (_battle.Command.WasReleasedThisFrame() && !_uiHitTest.IsOverUi(position))
            {
                WorldCommanded?.Invoke(position);
            }
        }

        private void Press(Vector2 position)
        {
            bool isOverUi = _uiHitTest.IsOverUi(position);
            _drag.Begin(ToNumerics(position), isOverUi);
            if (!isOverUi)
            {
                WorldPressed?.Invoke(position);
            }
        }

        private void Track(Vector2 position)
        {
            System.Numerics.Vector2 movement = _drag.Move(ToNumerics(position));
            TryStartDrag(position);
            if (_drag.HasDragged && movement.LengthSquared() > Mathf.Epsilon)
            {
                DragChanged?.Invoke(position);
            }
        }

        private void Release(Vector2 position)
        {
            if (_drag.IsPressed && !_drag.StartedOverUi)
            {
                if (TryStartDrag(position))
                {
                    DragChanged?.Invoke(position);
                }

                if (_drag.HasDragged)
                {
                    DragEnded?.Invoke(position);
                }
                else
                {
                    WorldClicked?.Invoke(position);
                }
            }

            _drag.End();
        }

        private bool TryStartDrag(Vector2 position)
        {
            if (!_drag.TryStartDrag(ToNumerics(position)))
            {
                return false;
            }

            DragStarted?.Invoke(new Vector2(_drag.PressPosition.X, _drag.PressPosition.Y));
            return true;
        }

        private static System.Numerics.Vector2 ToNumerics(Vector2 position)
        {
            return new System.Numerics.Vector2(position.x, position.y);
        }
    }
}
