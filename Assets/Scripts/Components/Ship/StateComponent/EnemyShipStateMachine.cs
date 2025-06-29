using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Ship.Movement;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Patterns.StateMachine;
using EmpireAtWar.Services.CoroutineService;
using LightWeightFramework.Components.Components;
using LightWeightFramework.Model;
using Zenject;

namespace EmpireAtWar.Components.Ship.AiComponent
{
    public class EnemyShipStateMachine: Component, IInitializable, ILateDisposable, ITickable
    {
        private const float DELAY_TIME = 5f;
        private readonly IMapModelObserver _mapModelObserver;
        private readonly CoroutineService _coroutineService;
        private readonly ShipStateMachine _shipStateMachine;

        private readonly ShipIdleState _shipIdleState;
        private readonly ShipLockMainTargetState _shipLockMainTargetState;
        private readonly MoveToPointState _moveToPointState;

        public EnemyShipStateMachine(
            IModel model,
            IShipMoveComponent shipMoveComponent,
            IAttackComponent attackComponent,
            IMapModelObserver mapModelObserver,
            IAttackDataFactory attackDataFactory,
            CoroutineService coroutineService)
        {
            _mapModelObserver = mapModelObserver;
            _coroutineService = coroutineService;
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
            _coroutineService.InvokeWithDelay((() =>
            {
                _moveToPointState.SetWorldCoordinates(_mapModelObserver.GetStationPosition(PlayerType.Player));
                _shipStateMachine.ChangeState(_moveToPointState);
            }), DELAY_TIME);

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