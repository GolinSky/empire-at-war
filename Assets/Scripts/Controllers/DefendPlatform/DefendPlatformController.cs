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
        private readonly ISelectionService selectionService;
        private readonly ISelectionModelObserver selectionModelObserver;

        private readonly UnitStateMachine stateMachine;
        private readonly UnitIdleState idleState;
        private readonly LockMainTargetState lockMainTargetState;
        
        public DefendPlatformController(DefendPlatformModel model, IWeaponComponent weaponComponent, IComponentHub componentHub, ISelectionService selectionService) : base(model)
        {
            this.selectionService = selectionService;
            selectionModelObserver = Model.GetModelObserver<ISelectionModelObserver>();

            stateMachine = new UnitStateMachine(weaponComponent, componentHub, Model);
            idleState = new UnitIdleState(stateMachine);
            lockMainTargetState = new LockMainTargetState(stateMachine);
            stateMachine.SetDefaultState(idleState);
            stateMachine.ChangeState(idleState);
        }
        
        public void Initialize()
        {
            selectionService.OnHitSelected += HandleSelected;
        }

        public void LateDispose()
        {
            selectionService.OnHitSelected -= HandleSelected;
        }
        
        private void HandleSelected(RaycastHit raycastHit)
        {
            if(!selectionModelObserver.IsSelected) return;
            
            IHardPointsProvider mainTarget = raycastHit.collider.GetComponentInChildren<IHardPointsProvider>();
            if (mainTarget is { PlayerType: PlayerType.Opponent, HasUnits: true })
            {
                lockMainTargetState.SetData(mainTarget); 
                stateMachine.ChangeState(lockMainTargetState);
            }
        }

        public void Tick()
        {
            stateMachine.Update();
        }
    }
}