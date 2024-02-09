using System;
using UnityEngine;
using UnityEngine.EventSystems;
using WorkShop.LightWeightFramework.Service;
using Zenject;

namespace EmpireAtWar.Services.Input
{
    public class InputService : Service, IInputService, ITickable
    {
        private Touch touch;
        public event Action<InputType, TouchPhase, Vector2> OnInput;

        public TouchPhase LastTouchPhase { get; private set; }

        public void Tick()
        {
            if(block) return;
            if (UnityEngine.Input.touchCount == 1)
            {
                
                touch = UnityEngine.Input.GetTouch(0);
                
                if (IsBlocked(touch.fingerId)) return;

                LastTouchPhase = touch.phase;
                switch (LastTouchPhase)
                {
                    case TouchPhase.Began:
                    {
                        if (touch.tapCount == 2)
                        {
                            InvokeEvent(InputType.ShipInput);
                        }
                        if (touch.tapCount == 1)
                        {
                            InvokeEvent(InputType.CameraInput);
                        }
                        break;
                    }
                    case TouchPhase.Moved:
                        if (touch.tapCount == 1)
                        {
                            InvokeEvent(InputType.CameraInput);
                        }
                        break;
                    case TouchPhase.Stationary:
                        break;
                    case TouchPhase.Ended:
                        break;
                    case TouchPhase.Canceled:
                        break;
                }
            }

            void InvokeEvent(InputType inputType)
            {
                OnInput?.Invoke(inputType, LastTouchPhase, touch.position);
            }
        }

        private bool IsBlocked(int id) => EventSystem.current.IsPointerOverGameObject(id) ||
                                          EventSystem.current.IsPointerOverGameObject();

        public void Block(bool b)
        {
            block = b;
        }

        private bool block;
    }
}