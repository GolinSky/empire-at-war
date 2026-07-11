using System;
using System.Collections.Generic;
using EmpireAtWar.Mvc;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Zenject;
using TouchPhase = UnityEngine.TouchPhase;

namespace EmpireAtWar.Services.InputService
{
    public class InputService : Service, IInputService, ITickable, IInitializable, IDisposable
    {
        private const float DRAG_THRESHOLD = 5f;
        private const float MAX_SWIPE_DELTA = 10f;

        public event Action<Vector2> OnSwipe;
        public event Action<float> OnZoom;
        public event Action<Vector2> OnEndDrag;
        public event Action<bool> OnBlocked;
        public event Action<InputType, TouchPhase, Vector2> OnInput;

        private readonly InputComponent_Generated _inputComponentGenerated;
        private InputComponent_Generated.TouchMapActions MapActions => _inputComponentGenerated.TouchMap;

        private bool _isBlocked;
        private bool _isPointerPressed;
        private bool _pressStartedOverUi;
        private bool _hasDragged;
        private Vector2 _pressPosition;
        private Vector2 _previousPosition;
        private float _previousMagnitude;

        public TouchPhase CurrentTouchPhase { get; private set; }
        public Vector2 TouchPosition => MapActions.PrimaryPosition.ReadValue<Vector2>();
        public Vector2 SecondaryTouchPosition => MapActions.SecondaryPosition.ReadValue<Vector2>();

        public InputService()
        {
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
            _isPointerPressed = true;
            _hasDragged = false;
            _pressPosition = TouchPosition;
            _previousPosition = _pressPosition;
            _pressStartedOverUi = IsPointerOverUIObject(_pressPosition);
            CurrentTouchPhase = TouchPhase.Began;

            if (!_isBlocked && !_pressStartedOverUi)
            {
                InvokeInputEvent(InputType.Selection);
            }
        }

        private void OnPointerReleased(InputAction.CallbackContext callbackContext)
        {
            Vector2 releasePosition = TouchPosition;
            CurrentTouchPhase = TouchPhase.Ended;

            if (_isBlocked)
            {
                OnEndDrag?.Invoke(releasePosition);
            }
            else if (_isPointerPressed && !_pressStartedOverUi && !_hasDragged)
            {
                InvokeInputEvent(InputType.ShipInput);
            }

            _isPointerPressed = false;
            _hasDragged = false;
            _pressStartedOverUi = false;
            _previousMagnitude = 0f;
        }

        private void OnSecondaryTouchPerformed(InputAction.CallbackContext callbackContext)
        {
            if (_isBlocked) return;

            if (Touchscreen.current == null ||
                !Touchscreen.current.primaryTouch.press.isPressed ||
                !Touchscreen.current.touches[1].press.isPressed)
            {
                _previousMagnitude = 0f;
                return;
            }

            float magnitude = (TouchPosition - SecondaryTouchPosition).magnitude;
            if (_previousMagnitude > 0f)
            {
                OnZoom?.Invoke(_previousMagnitude - magnitude);
            }
            _previousMagnitude = magnitude;
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
            if (!_isBlocked && MapActions.Zoom.IsPressed())
            {
                OnZoom?.Invoke(MapActions.Zoom.ReadValue<float>());
            }

            if (Touchscreen.current != null)
            {
                if (Touchscreen.current.touches[1].press.isPressed)
                {
                    return;
                }

                _previousMagnitude = 0f;
            }

            if (!_isPointerPressed || _isBlocked || _pressStartedOverUi)
            {
                return;
            }

            Vector2 currentPosition = TouchPosition;
            Vector2 delta = currentPosition - _previousPosition;
            _previousPosition = currentPosition;

            if (!_hasDragged)
            {
                _hasDragged = Vector2.Distance(_pressPosition, currentPosition) >= DRAG_THRESHOLD;
            }

            if (!_hasDragged || delta.sqrMagnitude <= Mathf.Epsilon)
            {
                return;
            }

            CurrentTouchPhase = TouchPhase.Moved;
            Vector2 direction = new Vector2(
                Mathf.Clamp(delta.x, -MAX_SWIPE_DELTA, MAX_SWIPE_DELTA),
                Mathf.Clamp(delta.y, -MAX_SWIPE_DELTA, MAX_SWIPE_DELTA));
            OnSwipe?.Invoke(direction);
        }

        private void InvokeInputEvent(InputType inputType)
        {
            OnInput?.Invoke(inputType, CurrentTouchPhase, TouchPosition);
        }

        public void Block(bool isBlocked)
        {
            _isBlocked = isBlocked;
            OnBlocked?.Invoke(isBlocked);
        }

        private bool IsPointerOverUIObject(Vector2 screenPosition)
        {
            if (EventSystem.current == null)
            {
                return false;
            }

            PointerEventData eventData = new PointerEventData(EventSystem.current)
            {
                position = screenPosition
            };
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);

            int uiLayer = LayerMask.NameToLayer("UI");
            for (int i = 0; i < results.Count; i++)
            {
                if (results[i].gameObject.layer == uiLayer)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
