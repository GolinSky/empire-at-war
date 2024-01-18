using System;
using UnityEngine;
using WorkShop.LightWeightFramework.Game;
using WorkShop.LightWeightFramework.Service;
using Zenject;

namespace EmpireAtWar.Services.Input
{
    public class InputService:Service, IInputService, ITickable
    {
        private Touch touch;
        public Vector2 MouseCoordinates => touch.position;
        public event Action<Vector2> OnInput;
        public event Action OnSelect;

        protected override void OnInit(IGameObserver gameObserver)
        {
        }

        protected override void Release()
        {
            
        }

        public void Tick()
        {
            if (UnityEngine.Input.touchCount > 0)
            {
                touch = UnityEngine.Input.GetTouch(0);
                
                if (touch.phase == TouchPhase.Began)
                {
                    OnInput?.Invoke(MouseCoordinates);
                }
            }
        }
    }
}