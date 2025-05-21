using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Ship.Selection;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Patterns.StateMachine;
using LightWeightFramework.Components.Components;
using LightWeightFramework.Model;
using Zenject;

namespace EmpireAtWar.Components.Ship.AiComponent
{
    public class EnemyShipStateComponent: Component, IInitializable, ILateDisposable, ITickable
    {
        private readonly IMapModelObserver _mapModelObserver;
        private readonly ShipStateMachine _shipStateMachine;

        private readonly ShipIdleState _shipIdleState;
        private readonly ShipLockMainTargetState _shipLockMainTargetState;
        private readonly MoveToPointState _moveToPointState;

        public EnemyShipStateComponent(
            IModel model,
            IShipMoveComponent shipMoveComponent,
            IAttackComponent attackComponent,
            IMapModelObserver mapModelObserver,
            IAttackDataFactory attackDataFactory)
        {
            _mapModelObserver = mapModelObserver;
            _shipStateMachine = new ShipStateMachine(
                shipMoveComponent, 
                attackComponent,
                model);
            
            _shipIdleState = new ShipIdleState(_shipStateMachine);
            _shipLockMainTargetState = new ShipLockMainTargetState(_shipStateMachine, attackDataFactory);
            _moveToPointState = new MoveToPointState(_shipStateMachine);

            _shipStateMachine.SetDefaultState(_shipIdleState);
            _shipStateMachine.ChangeState(_shipIdleState);
        }
        public void Initialize()
        {
            _moveToPointState.SetWorldCoordinates(_mapModelObserver.GetStationPosition(PlayerType.Player));
            _shipStateMachine.ChangeState(_moveToPointState);
        }

        public void LateDispose()
        {
            
        }

        public void Tick()
        {
            _shipStateMachine.Update();
        }
    }
}