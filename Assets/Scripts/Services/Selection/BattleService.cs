using System;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Services.Camera;
using EmpireAtWar.Services.InputService;
using EmpireAtWar.ViewComponents.Selection;
using UnityEngine;
using LightWeightFramework.Components.Service;
using Zenject;

namespace EmpireAtWar.Services.Battle
{
    public interface ISelectionService : IService
    {
        event Action<RaycastHit> OnHitSelected;
    }

    public class SelectionService : Service, ISelectionService, IInitializable, ILateDisposable
    {
        private readonly IInputService _inputService;
        private readonly ICameraService _cameraService;
        public event Action<RaycastHit> OnHitSelected;

        public SelectionService(IInputService inputService, ICameraService cameraService)
        {
            _inputService = inputService;
            _cameraService = cameraService;
        }
        
        public void Initialize()
        {
            _inputService.OnInput += HandleInput;
        }
        
        public void LateDispose()
        {
            _inputService.OnInput -= HandleInput;
        }
        
        private void HandleInput(InputType inputType, TouchPhase touchPhase, Vector2 touchPosition)
        {
            if(inputType != InputType.Selection) return;
            
            RaycastHit raycastHit = _cameraService.ScreenPointToRay(touchPosition);

            if(raycastHit.collider == null) return;
            
            SelectionViewComponent selectionComponent = raycastHit.collider.GetComponent<SelectionViewComponent>();

            if (selectionComponent != null)
            {
                OnHitSelected?.Invoke(raycastHit);
                selectionComponent.OnSelected();
            }
        }
    }
}