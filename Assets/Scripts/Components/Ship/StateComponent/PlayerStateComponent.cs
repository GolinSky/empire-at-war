using EmpireAtWar.Components.Ship.Selection;
using EmpireAtWar.Components.Ship.WeaponComponent;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Health;
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
    public class PlayerStateComponent : Component, IInitializable, ILateDisposable, ITickable, IObserver<ISelectionSubject>
    {
        private readonly ISelectionService _selectionService;
        private readonly IInputService _inputService;
        private readonly ISelectionModelObserver _selectionModelObserver;
        private readonly ShipStateMachine _shipStateMachine;

        private readonly ShipIdleState _shipIdleState;
        private readonly MoveToPointState _moveToPointState;
        private readonly ShipLockMainTargetState _shipLockMainTargetState;

        //todo: radar component
        public PlayerStateComponent(
            IModel model,
            IShipMoveComponent shipMoveComponent,
            IWeaponComponent weaponComponent,
            IComponentHub componentHub,
            ISelectionService selectionService,
            IInputService inputService)
        {
            _selectionService = selectionService;
            _inputService = inputService;

            _selectionModelObserver = model.GetModelObserver<ISelectionModelObserver>();
            _shipStateMachine = new ShipStateMachine(
                shipMoveComponent, 
                weaponComponent,
                componentHub,
                model);

            _shipIdleState = new ShipIdleState(_shipStateMachine);
            _moveToPointState = new MoveToPointState(_shipStateMachine);
            _shipLockMainTargetState = new ShipLockMainTargetState(_shipStateMachine);
            _shipStateMachine.SetDefaultState(_shipIdleState);
            _shipStateMachine.ChangeState(_shipIdleState);
        }

        public void Initialize()
        {
            // _selectionService.OnHitSelected += HandleSelected;
            //_inputService.OnInput += HandleInput;
            
            _selectionService.AddObserver(this);
        }

        public void LateDispose()
        {
            _selectionService.RemoveObserver(this);

            // _selectionService.OnHitSelected -= HandleSelected;
           // _inputService.OnInput -= HandleInput;
        }
        
        private void HandleInput(InputType inputType, TouchPhase touchPhase, Vector2 screenPosition)
        {
            if (inputType != InputType.ShipInput) return;
            if(!_selectionModelObserver.IsSelected) return;

            _moveToPointState.SetScreenCoordinates(screenPosition);
            _shipStateMachine.ChangeState(_moveToPointState);
        }
        

        
        public void UpdateState(ISelectionSubject selectionSubject)
        {
            if(!_selectionModelObserver.IsSelected) return;

            if (selectionSubject.UpdatedType == PlayerType.Opponent && selectionSubject.EnemySelectionContext.HasSelectable)
            {
                IHealthModelObserver healthModel = selectionSubject.EnemySelectionContext.Selectable.ModelObserver
                    .GetModelObserver<IHealthModelObserver>();
                if (!healthModel.IsDestroyed && healthModel.HasUnits)
                {
                    _shipLockMainTargetState.SetData(healthModel); 
                    _shipStateMachine.ChangeState(_shipLockMainTargetState);
                }
            }
        }

        public void Tick()
        {
            _shipStateMachine.Update();
        }
    }
}