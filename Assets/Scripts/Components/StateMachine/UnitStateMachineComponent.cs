using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Selection;
using EmpireAtWar.Patterns.StateMachine;
using EmpireAtWar.Services.Battle;
using LightWeightFramework.Components.Components;
using LightWeightFramework.Model;
using Zenject;

namespace EmpireAtWar.Components.StateMachine
{
    public class UnitStateMachineComponent: Component, IObserver<ISelectionSubject>, IInitializable, ILateDisposable, ITickable
    {
        private readonly ISelectionService _selectionService;
        private readonly ISelectionModelObserver _selectionModelObserver;

        private readonly UnitStateMachine _stateMachine;
        private readonly UnitIdleState _idleState;
        private readonly LockMainTargetState _lockMainTargetState;
        
        
        public UnitStateMachineComponent(
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