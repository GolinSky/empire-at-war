using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Ship.WeaponComponent;
using EmpireAtWar.ViewComponents.Health;

namespace EmpireAtWar.Patterns.StateMachine
{
    public class LockMainTargetState: UnitIdleState
    {
        private IHardPointsProvider mainTarget;

        public LockMainTargetState(UnitStateMachine stateMachine) : base(stateMachine)
        {
        }
        
        public void SetData(IHardPointsProvider mainTarget)
        {
            this.mainTarget = mainTarget;
        }

        public override void Enter()
        {
            base.Enter();
            weaponComponent.AddTarget(new AttackData(mainTarget,
                componentHub.GetComponent(mainTarget.ModelObserver),
                ShipUnitType.Any), AttackType.MainTarget);
        }

        public override void Update()
        {
            base.Update();
            if (!mainTarget.HasUnits)
            {
                StateMachine.ChangeToDefaultState();
            }
            //check if target is alive
        }
    }
}