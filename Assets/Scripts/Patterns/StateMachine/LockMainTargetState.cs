using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Ship.WeaponComponent;
using EmpireAtWar.ViewComponents.Health;

namespace EmpireAtWar.Patterns.StateMachine
{
    public class LockMainTargetState: UnitIdleState
    {
        private IHardPointsProvider _mainTarget;

        public LockMainTargetState(UnitStateMachine stateMachine) : base(stateMachine)
        {
        }
        
        public void SetData(IHardPointsProvider mainTarget)
        {
            _mainTarget = mainTarget;
        }

        public override void Enter()
        {
            base.Enter();
            _weaponComponent.AddTarget(new AttackData(_mainTarget,
                _componentHub.GetComponent(_mainTarget.ModelObserver),
                HardPointType.Any), AttackType.MainTarget);
        }

        public override void Update()
        {
            base.Update();
            if (!_mainTarget.HasUnits)
            {
                StateMachine.ChangeToDefaultState();
            }
            //check if target is alive
        }
    }
}