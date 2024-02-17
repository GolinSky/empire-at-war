using System;
using EmpireAtWar.Services.Camera;
using EmpireAtWar.ViewComponents.Selection;
using UnityEngine;
using UnityEngine.EventSystems;
using WorkShop.LightWeightFramework.Service;
using Zenject;

namespace EmpireAtWar.Services.Input
{
    public class InputService : Service, IInputService, ITickable
    {
        public event Action<Vector2> OnEndDrag;
        public event Action<InputType, TouchPhase, Vector2> OnInput;


        private Touch touch;
        private bool block;
        private TouchPhase lastTouchPhase;
        public TouchPhase CurrentTouchPhase { get; private set; }
        public Vector2 TouchPosition => touch.position;


        [Inject]
        private ICameraService CameraService { get; }
        public void Tick()
        {
            if (UnityEngine.Input.touchCount == 1)
            {
                touch = UnityEngine.Input.GetTouch(0);

                if (block)
                {
                    CurrentTouchPhase = touch.phase;

                    if (CurrentTouchPhase != TouchPhase.Moved && CurrentTouchPhase != TouchPhase.Stationary)
                    {
                        OnEndDrag?.Invoke(TouchPosition);
                    }
                    return;
                }

                if (IsBlocked(touch.fingerId))
                {
                    return;
                }
                CurrentTouchPhase = touch.phase;

                switch (CurrentTouchPhase)
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
                    {
                        //move to ship selection entity
                        if (lastTouchPhase != TouchPhase.Moved && touch.deltaTime < 0.1f)
                        {
                            InvokeEvent(InputType.ShipSelection);
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

            void InvokeEvent(InputType inputType)
            {
                OnInput?.Invoke(inputType, CurrentTouchPhase, touch.position);
            }
        }

        private bool IsBlocked(int id) => EventSystem.current.IsPointerOverGameObject(id) ||
                                          EventSystem.current.IsPointerOverGameObject();

        public void Block(bool b)
        {
            block = b;
        }

    }
}