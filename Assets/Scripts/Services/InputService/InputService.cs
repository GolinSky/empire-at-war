using System;
using UnityEngine;
using UnityEngine.EventSystems;
using LightWeightFramework.Components.Service;
using Zenject;

namespace EmpireAtWar.Services.InputService
{
    public class InputService : Service, IInputService, ITickable
    {
        public event Action<Vector2> OnEndDrag;
        public event Action<InputType, TouchPhase, Vector2> OnInput;
        public event Action<InputType, Touch, Touch> OnDoubleInput;


        private Touch touch;
        private TouchPhase lastTouchPhase;
        private bool isBlocked;
        public TouchPhase CurrentTouchPhase { get; private set; }
        public Vector2 TouchPosition => touch.position;
        
        public void Tick()
        {
            if (Input.touchCount == 1)
            {
                touch = Input.GetTouch(0);

                if (isBlocked)
                {
                    CurrentTouchPhase = touch.phase;

                    if (CurrentTouchPhase != TouchPhase.Moved && CurrentTouchPhase != TouchPhase.Stationary)
                    {
                        OnEndDrag?.Invoke(TouchPosition);
                    }
                    return;
                }

                for (var i = 0; i < Input.touches.Length; i++)
                {
                    if (IsBlocked(Input.touches[i].fingerId))  return;
                }
             
                CurrentTouchPhase = touch.phase;

                switch (CurrentTouchPhase)
                {
                    case TouchPhase.Began:
                    {
                        if (touch.tapCount == 2)
                        {
                            InvokeInputEvent(InputType.ShipInput);
                        }
                        break;
                    }
                    case TouchPhase.Moved:
                        if (touch.tapCount == 1 && lastTouchPhase != TouchPhase.Stationary)
                        {
                            InvokeInputEvent(InputType.CameraInput);
                        }
                        break;
                    case TouchPhase.Stationary:
                        break;
                    case TouchPhase.Ended:
                    {
                        if (touch.tapCount == 1 && lastTouchPhase != TouchPhase.Moved && touch.deltaTime < 0.1f)
                        {
                            InvokeInputEvent(InputType.Selection);
                        }
                        break;
                    }
                    case TouchPhase.Canceled:
                    {
                        break;
                    }
                }
                lastTouchPhase = CurrentTouchPhase;
            }
            else
            {
                if (Input.touchCount == 2)
                { 
                    OnDoubleInput?.Invoke(InputType.CameraInput, Input.GetTouch(0), Input.GetTouch(1));
                }
            }

            void InvokeInputEvent(InputType inputType)
            {
                OnInput?.Invoke(inputType, CurrentTouchPhase, touch.position);
            }
        }

        private bool IsBlocked(int id) => EventSystem.current.IsPointerOverGameObject(id) ||
                                          EventSystem.current.IsPointerOverGameObject();

        public void Block(bool isBlocked)
        {
            this.isBlocked = isBlocked;
        }
    }
}