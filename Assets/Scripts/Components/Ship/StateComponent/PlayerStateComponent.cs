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
    public class PlayerStateComponent : Component, IInitializable, ILateDisposable, ITickable
    {
        private readonly ISelectionService selectionService;
        private readonly IInputService inputService;
        private readonly ISelectionModelObserver selectionModelObserver;
        private readonly ShipStateMachine shipStateMachine;

        private readonly ShipIdleState shipIdleState;
        private readonly MoveToPointState moveToPointState;
        private readonly ShipLockMainTargetState shipLockMainTargetState;

        //todo: radar component
        public PlayerStateComponent(
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
            shipLockMainTargetState = new ShipLockMainTargetState(shipStateMachine);
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
            
            IHardPointsProvider mainTarget = raycastHit.collider.GetComponentInChildren<IHardPointsProvider>();
            if (mainTarget is { PlayerType: PlayerType.Opponent, HasUnits: true })
            {
                shipLockMainTargetState.SetData(mainTarget, raycastHit.transform.position); 
                shipStateMachine.ChangeState(shipLockMainTargetState);
            }
        }

        public void Tick()
        {
            shipStateMachine.Update();
        }
    }
}