using System;
using EmpireAtWar.Entities.SuperWeapons;
using EmpireAtWar.Entities.UnitActions.Model;
using EmpireAtWar.Entities.UnitActions;
using EmpireAtWar.Services.ShipAbilities;
using EmpireAtWar.Utils;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;
using EmpireAtWar.Lifecycle;

namespace EmpireAtWar.Services.Input
{
    public sealed class PointerGestures : IPointerGestures, IInitializable, ITickable, ILateDisposable
    {
        private readonly IPointerInput _pointerInput;
        private readonly IUiHitTest _uiHitTest;
        private readonly IShipAbilityTargeting _abilities;
        private readonly UnitActionTargetingModel _targeting;
        private readonly SuperWeaponTargetingModel _superWeapons;

        private readonly MouseDragState _drag = new MouseDragState();

        private readonly GameInputActions.BattleActions _battle;
        private bool _commandPressedInWorld;
        private Vector2 _selectPressPosition;
        private Vector2 _selectReleasePosition;

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

        public void Initialize()
        {
            _battle.Select.performed += CaptureSelectPress;
            _battle.Select.canceled += CaptureSelectRelease;
        }

        public void LateDispose()
        {
            _battle.Select.performed -= CaptureSelectPress;
            _battle.Select.canceled -= CaptureSelectRelease;
        }

        // A single input update can process a whole drag before the gameplay tick.
        private void CaptureSelectPress(InputAction.CallbackContext context) =>
            _selectPressPosition = EventPosition(context);

        private void CaptureSelectRelease(InputAction.CallbackContext context) =>
            _selectReleasePosition = EventPosition(context);

        private Vector2 EventPosition(InputAction.CallbackContext context) =>
            context.control.device is Pointer pointer ? pointer.position.ReadValue() : _pointerInput.Position;

        public void Tick()
        {
            if (!_battle.Select.enabled)
            {
                _commandPressedInWorld = false;
                CancelDrag();
                return;
            }

            Vector2 position = _pointerInput.Position;
            if (_battle.Command.WasPressedThisFrame())
                _commandPressedInWorld = !_uiHitTest.IsOverUi(position);
            bool commandReleased = _battle.Command.WasReleasedThisFrame();
            bool commandWorld = commandReleased && _commandPressedInWorld && !_uiHitTest.IsOverUi(position);
            if (commandReleased) _commandPressedInWorld = false;
            if (_abilities.IsWaitingForTarget || _targeting.Pending != null || _superWeapons.Pending != null)
            {
                CancelDrag();
                if ((_abilities.IsWaitingForTarget || _targeting.Pending == UnitActionId.AttackMove) &&
                    _battle.Select.WasPressedThisFrame() &&
                    !_uiHitTest.IsOverUi(_selectPressPosition))
                {
                    if (_abilities.IsWaitingForTarget) _abilities.CancelTargeting();
                    if (_targeting.Pending == UnitActionId.AttackMove) _targeting.Cancel();
                    return;
                }
                if (commandWorld)
                    WorldCommanded?.Invoke(position);
                return;
            }
            if (_battle.Select.WasPressedThisFrame())
            {
                Press(_selectPressPosition);
            }

            bool selectReleased = _battle.Select.WasReleasedThisFrame();
            if (_drag.IsPressed && !_drag.StartedOverUi && !selectReleased)
            {
                Track(position);
            }

            if (selectReleased)
            {
                Release(_selectReleasePosition);
            }

            if (commandWorld)
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
                else if (!_uiHitTest.IsOverUi(position))
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
