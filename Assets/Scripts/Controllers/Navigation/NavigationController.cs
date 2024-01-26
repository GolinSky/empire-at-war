using EmpireAtWar.Models.Navigation;
using EmpireAtWar.Services.Input;
using LightWeightFramework.Controller;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Controllers.Navigation
{
    public class NavigationController: Controller<NavigationModel>, IInitializable, ILateDisposable
    {
        private readonly IInputService inputService;
        public NavigationController(NavigationModel model, IInputService inputService) : base(model)
        {
            this.inputService = inputService;
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
            
            Model.TapPosition = screenPosition;
        }
    }
}