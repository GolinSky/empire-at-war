using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.BaseEntity.Orders;
using UnityEngine;

namespace EmpireAtWar.Entities.BaseEntity.EntityFacades
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

        public void FocusFire(IEntity target) =>
            _weapon.AddTarget(_attackDataFactory.ConstructData(target), AttackType.OrderedTarget);

        // Stations cannot move, so neither the formation offset nor the pace cluster applies.
        public void AttackHardPoint(IEntity target, int hardPointId, Vector3 formationOffset, PaceCluster pace = null) =>
            _weapon.AddTarget(_attackDataFactory.ConstructHardPointData(target, hardPointId),
                AttackType.OrderedTarget);

        public void Stop() => _weapon.ResetTarget();
    }
}
