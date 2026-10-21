using System;
using EmpireAtWar.Entities.SuperWeapons;
using EmpireAtWar.Entities.UnitActions.Model;
using EmpireAtWar.Services.ShipAbilities;
using EmpireAtWar.Utils;
using UnityEngine;
using VContainer;
using EmpireAtWar.Lifecycle;

namespace EmpireAtWar.Services.Input
{
    public sealed class PointerGestures : IPointerGestures, ITickable
    {
        private readonly IPointerInput _pointerInput;
        private readonly IUiHitTest _uiHitTest;
        private readonly IShipAbilityTargeting _abilities;
        private readonly UnitActionTargetingModel _targeting;
        private readonly SuperWeaponTargetingModel _superWeapons;

        private readonly MouseDragState _drag = new MouseDragState();

        private readonly GameInputActions.BattleActions _battle;

        public event Action<Vector2> WorldPressed;

        public event Action<Vector2> WorldClicked;

        public event Action<Vector2> WorldCommanded;

        public event Action<Vector2> DragStarted;

        public event Action<Vector2> DragChanged;

        public event Action<Vector2> DragEnded;
        public event Action DragCanceled;

        public PointerGestures(IPointerInput pointerInput, IUiHitTest uiHitTest, InputActionsProvider provider,
            IShipAbilityTargeting abilities, UnitActionTargetingModel targeting, SuperWeaponTargetingModel superWeapons)
        {
            _battle = provider.Actions.Battle;
            _pointerInput = pointerInput;
            _uiHitTest = uiHitTest;
            _abilities = abilities;
            _targeting = targeting;
            _superWeapons = superWeapons;
        }

        public void Tick()
        {
            if (!_battle.Select.enabled)
            {
                CancelDrag();
                return;
            }

            Vector2 position = _pointerInput.Position;
            if (_abilities.IsWaitingForTarget || _targeting.Pending != null || _superWeapons.Pending != null)
            {
                CancelDrag();
                if (_battle.Command.WasReleasedThisFrame() && !_uiHitTest.IsOverUi(position))
                    WorldCommanded?.Invoke(position);
                return;
            }
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
            _drag.Begin(position.ToNumerics(), isOverUi);
            if (!isOverUi)
            {
                WorldPressed?.Invoke(position);
            }
        }

        private void CancelDrag()
        {
            bool wasDragging = _drag.HasDragged;
            _drag.End();
            if (wasDragging) DragCanceled?.Invoke();
        }

        private void Track(Vector2 position)
        {
            System.Numerics.Vector2 movement = _drag.Move(position.ToNumerics());
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
            if (!_drag.TryStartDrag(position.ToNumerics()))
            {
                return false;
            }

            DragStarted?.Invoke(new Vector2(_drag.PressPosition.X, _drag.PressPosition.Y));
            return true;
        }
    }
}
