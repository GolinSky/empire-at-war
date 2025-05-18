using EmpireAtWar.Components.Ship.WeaponComponent;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Selection;
using EmpireAtWar.Patterns.StateMachine;
using EmpireAtWar.Services.Battle;
using LightWeightFramework.Controller;
using Zenject;

namespace EmpireAtWar.Entities.DefendPlatform
{
    public class DefendPlatformController : Controller<DefendPlatformModel>, IInitializable, ILateDisposable, ITickable, IObserver<ISelectionSubject>
    {
        private readonly ISelectionService _selectionService;
        private readonly ISelectionModelObserver _selectionModelObserver;

        private readonly UnitStateMachine _stateMachine;
        private readonly UnitIdleState _idleState;
        private readonly LockMainTargetState _lockMainTargetState;
        
        public DefendPlatformController(
            DefendPlatformModel model,
            IWeaponComponent weaponComponent,
            ISelectionService selectionService,
            IAttackDataFactory attackDataFactory) : base(model)
        {
            _selectionService = selectionService;
            _selectionModelObserver = Model.GetModelObserver<ISelectionModelObserver>();

            _stateMachine = new UnitStateMachine(weaponComponent, Model);
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