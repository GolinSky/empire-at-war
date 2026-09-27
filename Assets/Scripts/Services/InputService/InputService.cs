using System;
using EmpireAtWar.Mvc;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Zenject;
using TouchPhase = UnityEngine.TouchPhase;

namespace EmpireAtWar.Services.InputService
{
    public class InputService : Service, IInputService, ITickable, IInitializable, IDisposable
    {
        private const float EDGE_SCROLL_THICKNESS = 16f;
        private const float MAX_SWIPE_DELTA = 10f;

        public event Action<Vector2> OnSwipe;
        public event Action<Vector2> OnCameraPan;
        public event Action<Vector2> OnPrimaryDragStarted;
        public event Action<Vector2> OnPrimaryDragChanged;
        public event Action<Vector2> OnPrimaryDragEnded;
        public event Action OnLeftMousePressed;
        public event Action OnEscapePressed;
        public event Action OnWaypointModifierReleased;
        public event Action OnSelectAllUnitsPressed;
        public event Action OnSelectVisibleUnitsPressed;
        public event Action<float> OnZoom;
        public event Action<Vector2> OnEndDrag;
        public event Action<bool> OnBlocked;
        public event Action<InputType, TouchPhase, Vector2> OnInput;

        private readonly InputComponent_Generated _inputComponentGenerated;
        private InputComponent_Generated.TouchMapActions MapActions => _inputComponentGenerated.TouchMap;

        private bool _isBlocked;
        private readonly IUiHitTest _uiHitTest;
        private readonly PointerGestureState _gesture = new PointerGestureState();
        private bool _wasWaypointModifierPressed;

        public TouchPhase CurrentTouchPhase { get; private set; }
        public Vector2 TouchPosition => MapActions.PrimaryPosition.ReadValue<Vector2>();
        public bool SupportsHover => Mouse.current != null;
        public bool IsWaypointModifierPressed => Keyboard.current != null &&
                                                 Keyboard.current.altKey.isPressed;
        public Vector2 SecondaryTouchPosition => MapActions.SecondaryPosition.ReadValue<Vector2>();
        public Vector2 CameraMove
        {
            get
            {
                if (_isBlocked)
                {
                    return Vector2.zero;
                }

                Vector2 direction = GetEdgeScrollDirection();
                if (Keyboard.current == null ||
                    !Keyboard.current.ctrlKey.isPressed ||
                    !Keyboard.current.aKey.isPressed)
                {
                    direction += MapActions.CameraMove.ReadValue<Vector2>();
                    direction += GetKeyboardMoveDirection();
                }

                return Vector2.ClampMagnitude(direction, 1f);
            }
        }
        public int TapCount => Mathf.Max(1, MapActions.TouchCount.ReadValue<int>());

        public InputService(IUiHitTest uiHitTest)
        {
            _uiHitTest = uiHitTest;
            _inputComponentGenerated = new InputComponent_Generated();
        }

        public void Initialize()
        {
            _inputComponentGenerated.Enable();
            EnhancedTouchSupport.Enable();

            MapActions.PrimaryContact.started += OnPointerPressed;
            MapActions.PrimaryContact.canceled += OnPointerReleased;
            MapActions.SecondaryPosition.performed += OnSecondaryTouchPerformed;
            MapActions.Scroll.performed += OnScrollPerformed;
        }

        public void Dispose()
        {
            MapActions.PrimaryContact.started -= OnPointerPressed;
            MapActions.PrimaryContact.canceled -= OnPointerReleased;
            MapActions.SecondaryPosition.performed -= OnSecondaryTouchPerformed;
            MapActions.Scroll.performed -= OnScrollPerformed;

            _inputComponentGenerated.Disable();
            _inputComponentGenerated.Dispose();
            EnhancedTouchSupport.Disable();
        }

        private void OnPointerPressed(InputAction.CallbackContext callbackContext)
        {
            Vector2 position = TouchPosition;
            _gesture.Begin(new System.Numerics.Vector2(position.x, position.y),
                callbackContext.control.device is Mouse, _uiHitTest.IsOverUi(position));
            CurrentTouchPhase = TouchPhase.Began;

            if (callbackContext.control.device is Mouse)
            {
                OnLeftMousePressed?.Invoke();
            }

            if (!_isBlocked && !_gesture.StartedOverUi)
            {
                InvokeInputEvent(InputType.Selection);
            }
        }

        private void OnPointerReleased(InputAction.CallbackContext callbackContext)
        {
            Vector2 releasePosition = TouchPosition;
            CurrentTouchPhase = TouchPhase.Ended;

            if (!_isBlocked && _gesture.IsPressed && !_gesture.StartedOverUi &&
                TryStartDrag(releasePosition) && _gesture.IsMouse)
            {
                OnPrimaryDragChanged?.Invoke(releasePosition);
            }

            if (_isBlocked)
            {
                OnEndDrag?.Invoke(releasePosition);
            }
            else if (_gesture.IsPressed && !_gesture.StartedOverUi)
            {
                if (_gesture.HasDragged && _gesture.IsMouse)
                {
                    OnPrimaryDragEnded?.Invoke(releasePosition);
                }
                else if (!_gesture.HasDragged)
                {
                    InvokeInputEvent(InputType.Selection);
                    if (!_gesture.IsMouse)
                    {
                        InvokeInputEvent(InputType.ShipInput);
                    }
                }
            }

            _gesture.End();
        }

        private void OnSecondaryTouchPerformed(InputAction.CallbackContext callbackContext)
        {
            if (_isBlocked) return;

            if (Touchscreen.current == null ||
                !Touchscreen.current.primaryTouch.press.isPressed ||
                !Touchscreen.current.touches[1].press.isPressed)
            {
                _gesture.ResetPinch();
                return;
            }

            float magnitude = (TouchPosition - SecondaryTouchPosition).magnitude;
            if (_gesture.TryPinch(magnitude, out float delta))
                OnZoom?.Invoke(delta);
        }

        private void OnScrollPerformed(InputAction.CallbackContext callbackContext)
        {
            if (_isBlocked) return;

            float value = callbackContext.ReadValue<float>();
            if (!Mathf.Approximately(value, 0f))
            {
                OnZoom?.Invoke(value);
            }
        }

        public void Tick()
        {
            bool modifierPressed = IsWaypointModifierPressed;
            if (_wasWaypointModifierPressed && !modifierPressed)
                OnWaypointModifierReleased?.Invoke();
            _wasWaypointModifierPressed = modifierPressed;
            ProcessEscapeInput();
            ProcessRightMouseCommand();

            if (!_isBlocked)
            {
                ProcessUnitSelectionInput();

                if (MapActions.CameraDrag.IsPressed())
                {
                    Vector2 dragDelta = MapActions.TouchDelta.ReadValue<Vector2>();
                    Vector2 direction = new Vector2(
                        Mathf.Clamp(dragDelta.x, -MAX_SWIPE_DELTA, MAX_SWIPE_DELTA),
                        Mathf.Clamp(dragDelta.y, -MAX_SWIPE_DELTA, MAX_SWIPE_DELTA));

                    if (direction.sqrMagnitude > Mathf.Epsilon)
                    {
                        OnCameraPan?.Invoke(direction);
                    }
                }
            }

            if (!_isBlocked)
            {
                float zoomDirection = GetKeyboardZoomDirection();
                if (Mathf.Approximately(zoomDirection, 0f) &&
                    MapActions.Zoom.IsPressed())
                {
                    zoomDirection = MapActions.Zoom.ReadValue<float>();
                }

                if (!Mathf.Approximately(zoomDirection, 0f))
                {
                    OnZoom?.Invoke(zoomDirection);
                }
            }

            if (Touchscreen.current != null)
            {
                if (Touchscreen.current.touches[1].press.isPressed)
                {
                    return;
                }

                _gesture.ResetPinch();
            }

            if (!_gesture.IsPressed || _isBlocked || _gesture.StartedOverUi)
            {
                return;
            }

            Vector2 currentPosition = TouchPosition;
            System.Numerics.Vector2 movement = _gesture.Move(
                new System.Numerics.Vector2(currentPosition.x, currentPosition.y));
            Vector2 delta = new Vector2(movement.X, movement.Y);

            TryStartDrag(currentPosition);

            if (!_gesture.HasDragged || delta.sqrMagnitude <= Mathf.Epsilon)
            {
                return;
            }

            CurrentTouchPhase = TouchPhase.Moved;
            if (_gesture.IsMouse)
            {
                OnPrimaryDragChanged?.Invoke(currentPosition);
            }
            else
            {
                Vector2 direction = new Vector2(
                    Mathf.Clamp(delta.x, -MAX_SWIPE_DELTA, MAX_SWIPE_DELTA),
                    Mathf.Clamp(delta.y, -MAX_SWIPE_DELTA, MAX_SWIPE_DELTA));
                OnSwipe?.Invoke(direction);
            }
        }

        private void InvokeInputEvent(InputType inputType)
        {
            OnInput?.Invoke(inputType, CurrentTouchPhase, TouchPosition);
        }

        private bool TryStartDrag(Vector2 currentPosition)
        {
            if (!_gesture.TryStartDrag(new System.Numerics.Vector2(currentPosition.x, currentPosition.y)))
            {
                return false;
            }

            if (_gesture.IsMouse)
            {
                OnPrimaryDragStarted?.Invoke(new Vector2(_gesture.PressPosition.X, _gesture.PressPosition.Y));
            }

            return true;
        }

        public void Block(bool isBlocked)
        {
            _isBlocked = isBlocked;
            if (isBlocked)
            {
                OnCameraPan?.Invoke(Vector2.zero);
            }
            OnBlocked?.Invoke(isBlocked);
        }

        private static Vector2 GetEdgeScrollDirection()
        {
            if (Mouse.current == null)
            {
                return Vector2.zero;
            }

            Vector2 position = Mouse.current.position.ReadValue();
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

        private static Vector2 GetKeyboardMoveDirection()
        {
            if (Keyboard.current == null)
            {
                return Vector2.zero;
            }

            float horizontal = 0f;
            if (Keyboard.current.aKey.isPressed ||
                Keyboard.current.qKey.isPressed ||
                Keyboard.current.leftArrowKey.isPressed)
            {
                horizontal -= 1f;
            }
            if (Keyboard.current.dKey.isPressed ||
                Keyboard.current.eKey.isPressed ||
                Keyboard.current.rightArrowKey.isPressed)
            {
                horizontal += 1f;
            }

            float vertical = 0f;
            if (Keyboard.current.sKey.isPressed ||
                Keyboard.current.downArrowKey.isPressed)
            {
                vertical -= 1f;
            }
            if (Keyboard.current.wKey.isPressed ||
                Keyboard.current.upArrowKey.isPressed)
            {
                vertical += 1f;
            }

            return new Vector2(horizontal, vertical);
        }

        private static float GetKeyboardZoomDirection()
        {
            if (Keyboard.current == null)
            {
                return 0f;
            }

            float direction = 0f;
            if (Keyboard.current.rKey.isPressed)
            {
                direction -= 1f;
            }
            if (Keyboard.current.fKey.isPressed)
            {
                direction += 1f;
            }
            return direction;
        }

        private void ProcessRightMouseCommand()
        {
            if (_isBlocked ||
                Mouse.current == null ||
                !Mouse.current.rightButton.wasReleasedThisFrame)
            {
                return;
            }

            Vector2 position = Mouse.current.position.ReadValue();
            if (_uiHitTest.IsOverUi(position))
            {
                return;
            }

            CurrentTouchPhase = TouchPhase.Ended;
            OnInput?.Invoke(InputType.ShipInput, CurrentTouchPhase, position);
        }

        private void ProcessEscapeInput()
        {
            if (Keyboard.current != null &&
                Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                OnEscapePressed?.Invoke();
            }
        }

        private void ProcessUnitSelectionInput()
        {
            if (Keyboard.current == null ||
                !Keyboard.current.ctrlKey.isPressed ||
                !Keyboard.current.aKey.wasPressedThisFrame)
            {
                return;
            }

            if (Keyboard.current.shiftKey.isPressed)
            {
                OnSelectAllUnitsPressed?.Invoke();
            }
            else
            {
                OnSelectVisibleUnitsPressed?.Invoke();
            }
        }
    }
}
