using System;
using EmpireAtWar.Views.Ship;
using UnityEngine;
using WorkShop.LightWeightFramework.Game;
using WorkShop.LightWeightFramework.Service;
using Zenject;

namespace EmpireAtWar.Services.Input
{
    public interface IInputService:IService
    {
        Vector2 MouseCoordinates { get; }
        event Action<Vector2> OnInput;

        event Action OnSelect;
    }
    
    public class InputService:Service, IInputService, ITickable
    {
        private Touch touch;
        public Vector2 MouseCoordinates => touch.position;
        public event Action<Vector2> OnInput;
        public event Action OnSelect;
      //  [Inject] private ShipView.ShipFactory ShipFactory;

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

                if (touch.phase == TouchPhase.Moved)
                {
                    OnSelect?.Invoke();
                }
                
                if (touch.phase == TouchPhase.Ended)
                {
                    OnInput?.Invoke(MouseCoordinates);
                }
            }

            if (UnityEngine.Input.touchCount > 1)
            {
            }

        }
    }
}