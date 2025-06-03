using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Ship.Movement;
using EmpireAtWar.Components.Ship.Selection;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Selection;
using EmpireAtWar.Patterns.StateMachine;
using EmpireAtWar.Services.Battle;
using LightWeightFramework.Model;
using UnityEngine;
using Zenject;
using Component = LightWeightFramework.Components.Components.Component;

namespace EmpireAtWar.Components.Ship.AiComponent
{
    public class PlayerShipStateMachine : Component, IInitializable, ILateDisposable, ITickable, IObserver<ISelectionSubject>
    {
        private readonly ISelectionService _selectionService;
        private readonly ISelectionModelObserver _selectionModelObserver;
        private readonly ShipStateMachine _shipStateMachine;

        private readonly ShipIdleState _shipIdleState;
        private readonly MoveToPointState _moveToPointState;
        private readonly ShipLockMainTargetState _shipLockMainTargetState;

        //todo: radar component
        public PlayerShipStateMachine(
            IModel model,
            IShipMoveComponent shipMoveComponent,
            IAttackComponent attackComponent,
            ISelectionService selectionService,
            IAttackDataFactory attackDataFactory)
        {
            _selectionService = selectionService;

            _selectionModelObserver = model.GetModelObserver<ISelectionModelObserver>();
            _shipStateMachine = new ShipStateMachine(
                shipMoveComponent, 
                attackComponent,
                model);

            _shipIdleState = new ShipIdleState(_shipStateMachine);
            _moveToPointState = new MoveToPointState(_shipStateMachine);
            _shipLockMainTargetState = new ShipLockMainTargetState(_shipStateMachine, attackDataFactory);
            _shipStateMachine.SetDefaultState(_shipIdleState);
            _shipStateMachine.ChangeState(_shipIdleState);
        }

        public void Initialize()
        {
            _selectionService.AddObserver(this);
        }

        public void LateDispose()
        {
            _selectionService.RemoveObserver(this);
        }
        
        
        public void UpdateState(ISelectionSubject selectionSubject)
        {
            if(!_selectionModelObserver.IsSelected) return;

            if (selectionSubject.UpdatedType == PlayerType.Opponent && selectionSubject.EnemySelectionContext.HasSelectable)
            {
                IHealthModelObserver healthModel = selectionSubject.EnemySelectionContext.Entity.Model
                    .GetModelObserver<IHealthModelObserver>();
                if (!healthModel.IsDestroyed && healthModel.HasUnits && !_shipLockMainTargetState.IsTheSameTarget(selectionSubject.EnemySelectionContext.Entity))
                {
                    _shipLockMainTargetState.SetData(selectionSubject.EnemySelectionContext.Entity); 
                    _shipStateMachine.ChangeState(_shipLockMainTargetState);
                }
            }
        }
        
        public void MoveTo(Vector2 screenPosition)
        {
            _moveToPointState.SetScreenCoordinates(screenPosition);
            _shipStateMachine.ChangeState(_moveToPointState);
        }

        public void Tick()
        {
            _shipStateMachine.Update();
        }
    }
}