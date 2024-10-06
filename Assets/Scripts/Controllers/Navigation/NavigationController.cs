using EmpireAtWar.Models.Movement;
using EmpireAtWar.Models.Navigation;
using EmpireAtWar.Services.BattleService;
using EmpireAtWar.Services.Camera;
using EmpireAtWar.Services.InputService;
using EmpireAtWar.Services.NavigationService;
using LightWeightFramework.Controller;
using LightWeightFramework.Model;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Controllers.Navigation
{
    public class NavigationController: Controller<NavigationModel>, IInitializable, ILateDisposable
    {
        private readonly IInputService inputService;
        private readonly INavigationService navigationService;
        private readonly IBattleService battleService;
        private readonly ICameraService cameraService;

        public NavigationController(NavigationModel model, IInputService inputService, INavigationService navigationService, IBattleService battleService, ICameraService cameraService) : base(model)
        {
            this.inputService = inputService;
            this.navigationService = navigationService;
            this.battleService = battleService;// temp here
            this.cameraService = cameraService;
        }

        public void Initialize()
        {
            inputService.OnInput += HandleInput;
            battleService.OnTargetAdded += HandleAttackInput;
        }

        public void LateDispose()
        {
            inputService.OnInput -= HandleInput;
            battleService.OnTargetAdded -= HandleAttackInput;
        }
        
        private void HandleAttackInput(IModel model)
        {
            IShipMoveModelObserver shipMoveModelObserver = model.GetModelObserver<IShipMoveModelObserver>();
            if (shipMoveModelObserver != null)
            {
                Model.AttackPosition = cameraService.WorldToScreenPoint(shipMoveModelObserver.CurrentPosition);
            }
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