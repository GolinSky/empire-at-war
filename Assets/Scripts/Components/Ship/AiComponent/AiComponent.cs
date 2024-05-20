using EmpireAtWar.Components.Ship.Selection;
using EmpireAtWar.Components.Ship.WeaponComponent;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Selection;
using EmpireAtWar.Patterns.StateMachine;
using EmpireAtWar.Services.Battle;
using EmpireAtWar.Services.ComponentHub;
using EmpireAtWar.Services.InputService;
using EmpireAtWar.ViewComponents.Health;
using LightWeightFramework.Model;
using UnityEngine;
using Zenject;
using Component = LightWeightFramework.Components.Components.Component;

namespace EmpireAtWar.Components.Ship.AiComponent
{
    public class AiComponent : Component, IInitializable, ILateDisposable, ITickable
    {
        private readonly ISelectionService selectionService;
        private readonly IInputService inputService;
        private readonly ISelectionModelObserver selectionModelObserver;
        private readonly ShipIdleState shipIdleState;
        private readonly ShipStateMachine shipStateMachine;
        private readonly MoveToPointState moveToPointState;
        private readonly LockMainTargetState lockMainTargetState;

        //todo: radar component
        public AiComponent(
            IModel model,
            IShipMoveComponent shipMoveComponent,
            IWeaponComponent weaponComponent,
            IComponentHub componentHub,
            ISelectionService selectionService,
            IInputService inputService)
        {
            this.selectionService = selectionService;
            this.inputService = inputService;

            selectionModelObserver = model.GetModelObserver<ISelectionModelObserver>();
            shipStateMachine = new ShipStateMachine(
                shipMoveComponent, 
                weaponComponent,
                componentHub,
                model);

            shipIdleState = new ShipIdleState(shipStateMachine);
            moveToPointState = new MoveToPointState(shipStateMachine);
            lockMainTargetState = new LockMainTargetState(shipStateMachine);
            shipStateMachine.SetDefaultState(shipIdleState);
            shipStateMachine.ChangeState(shipIdleState);
        }

        public void Initialize()
        {
            selectionService.OnHitSelected += HandleSelected;
            inputService.OnInput += HandleInput;
        }

        public void LateDispose()
        {
            selectionService.OnHitSelected -= HandleSelected;
            inputService.OnInput -= HandleInput;
        }
        
        private void HandleInput(InputType inputType, TouchPhase touchPhase, Vector2 screenPosition)
        {
            if (inputType != InputType.ShipInput) return;
            if(!selectionModelObserver.IsSelected) return;

            moveToPointState.SetCoordinates(screenPosition);
            shipStateMachine.ChangeState(moveToPointState);
        }
        
        private void HandleSelected(RaycastHit raycastHit)
        {
            if(!selectionModelObserver.IsSelected) return;
            
            IShipUnitsProvider mainTarget = raycastHit.collider.GetComponentInChildren<IShipUnitsProvider>();
            if (mainTarget is { PlayerType: PlayerType.Opponent, HasUnits: true })
            {
                lockMainTargetState.SetData(mainTarget, raycastHit.transform.position); 
                shipStateMachine.ChangeState(lockMainTargetState);
            }
        }

        public void Tick()
        {
            shipStateMachine.Update();
        }
    }
}