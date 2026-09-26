using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using UnityEngine;

namespace EmpireAtWar.Entities.Ship.EntityFacades
{
    public sealed class StationaryAttackFacade : IFocusFireFacade, IHardPointAttackFacade, IStopFacade
    {
        private readonly IWeaponComponent _weapon;
        private readonly IAttackDataFactory _attackDataFactory;

        public StationaryAttackFacade(IWeaponComponent weapon,
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

        // Stations cannot move, so the formation offset does not apply.
        public void AttackHardPoint(IEntity target, int hardPointId, Vector3 formationOffset) =>
            _weapon.AddTarget(_attackDataFactory.ConstructHardPointData(target, hardPointId),
                AttackType.MainTarget);

        public void Stop() => _weapon.ResetTarget();
    }
}
