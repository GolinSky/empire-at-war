using EmpireAtWar.Models.Navigation;
using EmpireAtWar.Services.InputService;
using EmpireAtWar.Services.NavigationService;
using LightWeightFramework.Controller;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Controllers.Navigation
{
    public class NavigationController: Controller<NavigationModel>, IInitializable, ILateDisposable
    {
        private readonly IInputService inputService;
        private readonly INavigationService navigationService;

        public NavigationController(NavigationModel model, IInputService inputService, INavigationService navigationService) : base(model)
        {
            this.inputService = inputService;
            this.navigationService = navigationService;
        }

        public void Initialize()
        {
            inputService.OnInput += HandleInput;
        }

        public void LateDispose()
        {
            inputService.OnInput -= HandleInput;
        }
        
        private void HandleInput(InputType inputType, TouchPhase touchPhase, Vector2 screenPosition)
        {
            if(inputType != InputType.ShipInput) return;
            if(navigationService.Selectable == null) return;
            if(navigationService.Selectable.Movable == null) return;
            if(!navigationService.Selectable.Movable.CanMove) return;
            
            Model.TapPosition = screenPosition;
        }
    }
}