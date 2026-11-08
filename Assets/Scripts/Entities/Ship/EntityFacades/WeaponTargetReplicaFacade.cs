using System;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;

namespace EmpireAtWar.Entities.Ship.EntityFacades
{
    /// <summary>Online target relay of an armed entity; clients rebuild the host's attack data with their own factory.</summary>
    public sealed class WeaponTargetReplicaFacade : IWeaponTargetReplicaFacade
    {
        private const int NO_HARD_POINT = -1;

        private readonly IWeaponTargetReplica _weapon;
        private readonly IAttackDataFactory _attackDataFactory;

        public WeaponTargetReplicaFacade(IWeaponTargetReplica weapon, IAttackDataFactory attackDataFactory)
        {
            _weapon = weapon;
            _attackDataFactory = attackDataFactory;
        }

        public event Action<AttackData, AttackType> TargetAdded
        {
            add => _weapon.TargetAdded += value;
            remove => _weapon.TargetAdded -= value;
        }

        public event Action TargetReset
        {
            add => _weapon.TargetReset += value;
            remove => _weapon.TargetReset -= value;
        }

        public void AddTarget(IEntity target, AttackType attackType, int hardPointId, HardPointType hardPointType) =>
            _weapon.AddReplicaTarget(hardPointId == NO_HARD_POINT
                ? _attackDataFactory.ConstructData(target, hardPointType)
                : _attackDataFactory.ConstructHardPointData(target, hardPointId), attackType);

        public void ResetTarget() => _weapon.ResetReplicaTarget();

        // A focused hardpoint attack resumes against the whole target.
        public void RetargetRebuilt(IEntity rebuilt)
        {
            if (!_weapon.RemoveTargetsOf(rebuilt.Id, out bool wasMain)) return;
            _weapon.AddReplicaTarget(_attackDataFactory.ConstructData(rebuilt),
                wasMain ? AttackType.MainTarget : AttackType.Base);
        }
    }
}
