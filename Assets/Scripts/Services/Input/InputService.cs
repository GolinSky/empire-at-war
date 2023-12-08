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
        public Vector2 MouseCoordinates => UnityEngine.Input.mousePosition;
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
            if (UnityEngine.Input.GetKeyDown(KeyCode.Mouse1))
            {
                OnInput?.Invoke(MouseCoordinates);
            }
            else if(UnityEngine.Input.GetKeyDown(KeyCode.Mouse0))
            {
                OnSelect?.Invoke();
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.Space))
            {
               // ShipFactory.Create();
            }
        }
    }
}