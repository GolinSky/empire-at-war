using System;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Services.Camera;
using EmpireAtWar.Services.Input;
using EmpireAtWar.ViewComponents.Selection;
using UnityEngine;
using WorkShop.LightWeightFramework.Service;
using Zenject;

namespace EmpireAtWar.Services.Battle
{
    public interface IBattleService : IService
    {
        event Action<IHealthComponent> OnTargetAdded;
        void AddTarget(IHealthComponent healthComponent);
    }

    public class BattleService : Service, IBattleService, IInitializable, ILateDisposable
    {
        private readonly IInputService inputService;
        private readonly ICameraService cameraService;
        public event Action<IHealthComponent> OnTargetAdded;

        public BattleService(IInputService inputService, ICameraService cameraService)
        {
            this.inputService = inputService;
            this.cameraService = cameraService;
        }
        
        public void AddTarget(IHealthComponent healthComponent)
        {
            if(healthComponent.Destroyed) return;
            
            OnTargetAdded?.Invoke(healthComponent);
        }


        public void Initialize()
        {
            inputService.OnInput += HandleInput;
        }
        
        public void LateDispose()
        {
            inputService.OnInput -= HandleInput;
        }
        
        private void HandleInput(InputType inputType, TouchPhase touchPhase, Vector2 touchPosition)
        {
            RaycastHit raycastHit = cameraService.ScreenPointToRay(touchPosition);

            SelectionViewComponent selectionComponent = raycastHit.collider.GetComponent<SelectionViewComponent>();

            if (selectionComponent != null)
            {
                selectionComponent.OnSelected();
            }
        }
    }
}