using EmpireAtWar.Components.Ship.Movement;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Patterns.StateMachine;

namespace EmpireAtWar.Entities.Ship.StateMachine
{
    public class IdleState : IBaseState
    {
        private readonly IShipMoveComponent _shipMoveComponent;
        private readonly IWeaponComponent _weaponComponent;

        public IdleState(IShipMoveComponent shipMoveComponent, IWeaponComponent weaponComponent)
        {
            _shipMoveComponent = shipMoveComponent;
            _weaponComponent = weaponComponent;
        }

        public void Enter()
        {
            if (_shipMoveComponent.IsMoving)
            {
                _shipMoveComponent.Stop();
            }

            _weaponComponent.ResetTarget();
        }

        public void Update()
        {
        }

        public void Exit()
        {
        }
    }
}
