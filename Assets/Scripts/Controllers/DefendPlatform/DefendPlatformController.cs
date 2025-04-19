using EmpireAtWar.Components.Ship.WeaponComponent;
using EmpireAtWar.Models.DefendPlatform;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Selection;
using EmpireAtWar.Patterns.StateMachine;
using EmpireAtWar.Services.Battle;
using EmpireAtWar.Services.ComponentHub;
using EmpireAtWar.ViewComponents.Health;
using LightWeightFramework.Controller;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Controllers.DefendPlatform
{
    public class DefendPlatformController : Controller<DefendPlatformModel>, IInitializable, ILateDisposable, ITickable
    {
        private readonly ISelectionService _selectionService;
        private readonly ISelectionModelObserver _selectionModelObserver;

        private readonly UnitStateMachine _stateMachine;
        private readonly UnitIdleState _idleState;
        private readonly LockMainTargetState _lockMainTargetState;
        
        public DefendPlatformController(DefendPlatformModel model, IWeaponComponent weaponComponent, IComponentHub componentHub, ISelectionService selectionService) : base(model)
        {
            _selectionService = selectionService;
            _selectionModelObserver = Model.GetModelObserver<ISelectionModelObserver>();

            _stateMachine = new UnitStateMachine(weaponComponent, componentHub, Model);
            _idleState = new UnitIdleState(_stateMachine);
            _lockMainTargetState = new LockMainTargetState(_stateMachine);
            _stateMachine.SetDefaultState(_idleState);
            _stateMachine.ChangeState(_idleState);
        }
        
        public void Initialize()
        {
            // _selectionService.OnHitSelected += HandleSelected;
        }

        public void LateDispose()
        {
            // _selectionService.OnHitSelected -= HandleSelected;
        }
        
        private void HandleSelected(RaycastHit raycastHit)
        {
            if(!_selectionModelObserver.IsSelected) return;
            
            IHardPointsProvider mainTarget = raycastHit.collider.GetComponentInChildren<IHardPointsProvider>();
            if (mainTarget is { PlayerType: PlayerType.Opponent, HasUnits: true })
            {
                _lockMainTargetState.SetData(mainTarget); 
                _stateMachine.ChangeState(_lockMainTargetState);
            }
        }

        public void Tick()
        {
            _stateMachine.Update();
        }
    }
}