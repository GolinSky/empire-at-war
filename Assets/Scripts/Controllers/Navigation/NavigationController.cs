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
        private readonly IInputService _inputService;
        private readonly INavigationService _navigationService;
        private readonly IBattleService _battleService;
        private readonly ICameraService _cameraService;

        public NavigationController(NavigationModel model, IInputService inputService, INavigationService navigationService, IBattleService battleService, ICameraService cameraService) : base(model)
        {
            _inputService = inputService;
            _navigationService = navigationService;
            _battleService = battleService;// temp here
            _cameraService = cameraService;
        }

        public void Initialize()
        {
            _inputService.OnInput += HandleInput;
            _battleService.OnTargetAdded += HandleAttackInput;
        }

        public void LateDispose()
        {
            _inputService.OnInput -= HandleInput;
            _battleService.OnTargetAdded -= HandleAttackInput;
        }
        
        private void HandleAttackInput(IModel model)
        {
            ISimpleMoveModelObserver shipMoveModelObserver = model.GetModelObserver<ISimpleMoveModelObserver>();
            if (shipMoveModelObserver != null)
            {
                Model.AttackPosition = _cameraService.WorldToScreenPoint(shipMoveModelObserver.CurrentPosition);
            }
        }
        
        private void HandleInput(InputType inputType, TouchPhase touchPhase, Vector2 screenPosition)
        {
            if(inputType != InputType.ShipInput) return;
            if(_navigationService.Selectable == null) return;
            if(_navigationService.Selectable.Movable == null) return;
            if(!_navigationService.Selectable.Movable.CanMove) return;
            
            Model.TapPosition = screenPosition;
        }
    }
}