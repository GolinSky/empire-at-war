using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityCommands;

namespace EmpireAtWar.Entities.Ship.EntityCommands
{
    public sealed class StationaryAttackCommand : IFocusFireCommand, IStopCommand
    {
        private readonly IWeaponComponent _weapon;
        private readonly IAttackDataFactory _attackDataFactory;

        public StationaryAttackCommand(IWeaponComponent weapon,
            IAttackDataFactory attackDataFactory)
        {
            _weapon = weapon;
            _attackDataFactory = attackDataFactory;
        }

        public void FocusFire(IEntity target)
        {
            AttackData data = _attackDataFactory.ConstructData(target);
            if (data != null) _weapon.AddTarget(data, AttackType.MainTarget);
        }

        public void Stop() => _weapon.ResetTarget();
    }
}
