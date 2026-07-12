using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Selection;
using EmpireAtWar.Patterns.StateMachine;
using EmpireAtWar.Services.Battle;
using EmpireAtWar.Mvc;
using Zenject;

namespace EmpireAtWar.Components.StateMachine
{
    public class UnitStateMachineComponent: MonoComponent<PureModel>, IObserver<ISelectionSubject>, IInitializable,
        ILateDisposable, ITickable, IComponent
    {
        private ISelectionService _selectionService;
        private ISelectionModelObserver _selectionModelObserver;

        private UnitStateMachine _stateMachine;
        private UnitIdleState _idleState;
        private LockMainTargetState _lockMainTargetState;
        
        
        [Inject]
        private void Construct(
            IModel model,
            IAttackComponent attackComponent,
            ISelectionService selectionService,
            IAttackDataFactory attackDataFactory) 
        {
            _selectionService = selectionService;
            _selectionModelObserver = model.GetModelObserver<ISelectionModelObserver>();

            _stateMachine = new UnitStateMachine(attackComponent, model);
            _idleState = new UnitIdleState(_stateMachine);
            _lockMainTargetState = new LockMainTargetState(_stateMachine, attackDataFactory);
            _stateMachine.SetDefaultState(_idleState);
            _stateMachine.ChangeState(_idleState);
        }
        
        
        public void Initialize()
        {
            _selectionService.AddObserver(this);
        }

        public void LateDispose()
        {
            Release();
        }

        public override void Release()
        {
            _selectionService.RemoveObserver(this);
        }
        

        public void Tick()
        {
            _stateMachine.Update();
        }

        public void UpdateState(ISelectionSubject selectionSubject)
        {
            if(!_selectionModelObserver.IsSelected) return;

            if (selectionSubject.UpdatedType == PlayerType.Opponent && selectionSubject.EnemySelectionContext.HasSelectable)
            {
                IHealthModelObserver healthModel = selectionSubject.EnemySelectionContext.Entity.Model
                    .GetModelObserver<IHealthModelObserver>();
                if (!healthModel.IsDestroyed && healthModel.HasUnits)
                {
                    _lockMainTargetState.SetData(selectionSubject.EnemySelectionContext.Entity); 
                    _stateMachine.ChangeState(_lockMainTargetState);
                }
            }
        }
    }
}
