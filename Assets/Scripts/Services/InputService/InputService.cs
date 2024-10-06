using System;
using System.Collections.Generic;
using EmpireAtWar.Services.Camera;
using UnityEngine;
using UnityEngine.EventSystems;
using LightWeightFramework.Components.Service;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;

using Zenject;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.TouchPhase;


namespace EmpireAtWar.Services.InputService
{
    public class InputService : Service, IInputService, ITickable, IInitializable, IDisposable
    {
        private const string LOG_ID = "InputService: ";
        public event Action<Vector2> OnSwipe;
        public event Action<float> OnZoom; 
        public event Action<Vector2> OnEndDrag;
        public event Action<bool> OnBlocked;
        public event Action<InputType, TouchPhase, Vector2> OnInput;

        private InputComponent_Generated inputComponentGenerated;
        private InputComponent_Generated.TouchMapActions MapActions => inputComponentGenerated.TouchMap;
        private Touch touch;
        private TouchPhase lastTouchPhase;

        
        private bool isBlocked;
        private float previousMagnitude;
        public TouchPhase CurrentTouchPhase { get; private set; }
        public Vector2 TouchPosition =>  MapActions.PrimaryPosition.ReadValue<Vector2>();
        public Vector2 SecondaryTouchPosition =>  MapActions.SecondaryPosition.ReadValue<Vector2>();


        public InputService()
        {
            inputComponentGenerated = new InputComponent_Generated();
        }

        public void Initialize()
        {
            inputComponentGenerated.Enable();
            EnhancedTouchSupport.Enable();
            
            MapActions.PrimaryContact.canceled += OnTouchReleased;
            MapActions.SecondaryPosition.performed += OnSecondaryTouchPerformed;
        }

        public void Dispose()
        {
            EnhancedTouchSupport.Disable();
            inputComponentGenerated.Disable();
            inputComponentGenerated?.Dispose();
            
            MapActions.PrimaryContact.canceled -= OnTouchReleased;
            MapActions.SecondaryPosition.performed -= OnSecondaryTouchPerformed;
        }

        private void OnSecondaryTouchPerformed(InputAction.CallbackContext callbackContext)
        {
            if (isBlocked) return;

            float magnitude = (TouchPosition - SecondaryTouchPosition).magnitude;
            if (previousMagnitude == 0f)
            {
                previousMagnitude = magnitude;
            }
            float difference = magnitude - previousMagnitude;
            previousMagnitude = magnitude;
            OnZoom?.Invoke(-difference);
        }

        private void OnTouchReleased(InputAction.CallbackContext callbackContext)
        {
            if (isBlocked)
            {
                OnEndDrag?.Invoke(MapActions.PrimaryPosition.ReadValue<Vector2>());
            }
            else
            {
                int touchCount = MapActions.TouchCount.ReadValue<int>();
                Debug.Log($"touchCount:{touchCount}");
                if (touchCount == 2)
                {
                    InvokeInputEvent(InputType.ShipInput);
                }
            }
        }
        
        public void Tick()
        {
#if UNITY_ANDROID && UNITY_EDITOR_OSX
                        if (Touch.activeTouches.Count == 1)
#endif
            {
                bool isBlockedByUi = IsPointerOverUIObject();

                if (MapActions.PrimaryContact.IsPressed() && !isBlockedByUi )
                {
                    if (!isBlocked)
                    {
                        Vector2 delta = MapActions.TouchDelta.ReadValue<Vector2>();

                        Vector2 direction = Vector2.zero;

                        direction.x = Mathf.Clamp(delta.x, -10, 10);
                        direction.y = Mathf.Clamp(delta.y, -10, 10);

                        if (direction == Vector2.zero)
                        {
                            InvokeInputEvent(InputType.Selection);
                            
                            OnSwipe?.Invoke(Vector2.one*0.1f);

                            return;
                        }

                        OnSwipe?.Invoke(direction);
                    }
                }
                
            
            }
            
            if (MapActions.Scroll.IsPressed() || MapActions.Scroll.IsInProgress())
            {
                float scrollValue = MapActions.Scroll.ReadValue<float>();
                OnZoom.Invoke(scrollValue);
            }

           
            // if (Input.touchCount == 1)
            // {
            //     touch = Input.GetTouch(0);
            //
            //     if (isBlocked)
            //     {
            //         CurrentTouchPhase = touch.phase;
            //
            //         if (CurrentTouchPhase != TouchPhase.Moved && CurrentTouchPhase != TouchPhase.Stationary)
            //         {
            //             OnEndDrag?.Invoke(TouchPosition);
            //         }
            //         return;
            //     }
            //
            //     for (var i = 0; i < Input.touches.Length; i++)
            //     {
            //         if (IsBlocked(Input.touches[i].fingerId))  return;
            //     }
            //  
            //     CurrentTouchPhase = touch.phase;
            //
            //     switch (CurrentTouchPhase)
            //     {
            //         case TouchPhase.Began:
            //         {
            //             if (touch.tapCount > 1)
            //             {
            //                 InvokeInputEvent(InputType.ShipInput);
            //             }
            //             break;
            //         }
            //         case TouchPhase.Moved:
            //             if (touch.tapCount == 1 && lastTouchPhase != TouchPhase.Stationary)
            //             {
            //                 InvokeInputEvent(InputType.CameraInput);
            //             }
            //             break;
            //         case TouchPhase.Stationary:
            //             break;
            //         case TouchPhase.Ended:
            //         {
            //             if (touch.tapCount == 1 && lastTouchPhase != TouchPhase.Moved)
            //             {
            //                 InvokeInputEvent(InputType.Selection);
            //             }
            //             break;
            //         }
            //         case TouchPhase.Canceled:
            //         {
            //             break;
            //         }
            //     }
            //     lastTouchPhase = CurrentTouchPhase;
            // }
            // else
            // {
            //     if (Input.touchCount == 2)
            //     { 
            //         OnDoubleInput?.Invoke(InputType.CameraInput, Input.GetTouch(0), Input.GetTouch(1));
            //     }
            // }
            
          //  Debug.Log($"Input.mouseScrollDelta:{Input.mouseScrollDelta}");
            // float scrollAxis = Input.mouseScrollDelta.y;
            // if (scrollAxis != 0)
            // {
            //     OnZoom?.Invoke(InputType.CameraInput, scrollAxis);
            // }
            //
          
        }
       private void InvokeInputEvent(InputType inputType)
        {
            OnInput?.Invoke(inputType, CurrentTouchPhase, TouchPosition);
        }

        public void Block(bool isBlocked)
        {
            this.isBlocked = isBlocked;
            OnBlocked?.Invoke(isBlocked);
        }
        
        public  bool IsPointerOverUIObject()
        {
            PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);
            eventDataCurrentPosition.position = TouchPosition;
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventDataCurrentPosition, results);

            for (int i = 0; i < results.Count; i++)
            {
                if (results[i].gameObject.layer == 5) //5 = UI layer
                {
                    return true;
                }
            }

            return false;
        }
        
  


    }
}