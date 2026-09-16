using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Mvc;
using EmpireAtWar.ViewComponents.Health;
using EmpireAtWar.Services.Timing;
using UnityEngine;
using Utilities.ScriptUtils.Time;
using Zenject;

namespace EmpireAtWar.Components.Weapon
{
    public class WeaponComponent: MonoComponent<WeaponModel>, IWeaponComponent, IInitializable, ITickable, IWeaponPresenter
    {
        [SerializeField] private List<WeaponHardPointView> hardPoints;
        [SerializeField] private Transform attackOrigin;
        [SerializeField] private bool useWeaponDamageRange;
        
        private CombatAttackCoordinator _attackCoordinator;
        private ITimer _attackTimer = TimerFactory.ConstructTimer();
        private List<AttackData> _attackDataList = new List<AttackData>();
        private AttackData _mainAttackData = null;
        private float _nextFireTime = 0f;
        private int _currentWeaponIndex = 0;
        private bool _isAttackedThisFrame;
        private bool _isReleased;
        public float AttackDistance => Model.OptimalAttackRange;


        [Inject]
        private void Construct(CombatAttackCoordinator attackCoordinator)
        {
            _attackCoordinator = attackCoordinator;
        }
        
        public void Initialize()
        {
            _attackCoordinator.Register(this);
            if (useWeaponDamageRange)
            {
                Model.SetOptimalAttackRange(hardPoints.Select(hardPoint => hardPoint.WeaponType));
            }

            foreach (WeaponHardPointView hardPoint in hardPoints)
            {
                hardPoint.SetData(Model.ProjectileModel.GetData(hardPoint.WeaponType), Model.OptimalAttackRange, this, _attackCoordinator);
            }
        }

        private void OnDestroy()
        {
            Release();
        }

        public override void Release()
        {
            if (_isReleased)
            {
                return;
            }

            _isReleased = true;
            _attackCoordinator.Unregister(this);
            foreach (WeaponHardPointView hardPoint in hardPoints)
            {
                hardPoint.ReleaseAttackSequence();
            }

            _attackDataList.Clear();
            _mainAttackData = null;
        }

        public void AddTarget(AttackData attackData, AttackType attackType)
        {
            switch (attackType)
            {
                case AttackType.Base:
                {
                    if (_attackDataList.Any(data => attackData.SameSource(data)))
                    {
                        return;
                    }

                    _attackDataList.Add(attackData);
                    break;
                }
                case AttackType.MainTarget:
                {
                    _mainAttackData = attackData;

                    if (!_attackDataList.Any(data => attackData.SameSource(data)))
                    {
                        _attackDataList.Add(attackData);
                    }

                    break;
                }
            }
        }

        public bool HasEnoughRange(float distance)
        {
            return distance <= Model.OptimalAttackRange;
        }

        public void ResetTarget()
        {
            _mainAttackData = null;
        }
        public void Tick()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using (BattleProfilerMarkers.WeaponTick.Auto())
            {
#endif
            if (_isReleased)
                return;

            if (Time.time < _nextFireTime)
                return;

            if (hardPoints == null || hardPoints.Count == 0)
                return;

            WeaponHardPointView weapon = hardPoints[_currentWeaponIndex];
            _isAttackedThisFrame = false;
            
            if (!weapon.IsDestroyed && !weapon.IsBusy)
            {
                _isAttackedThisFrame = TryFireWeapon(weapon);
            }

            _currentWeaponIndex++;
            if (_currentWeaponIndex >= hardPoints.Count)
                _currentWeaponIndex = 0;

            if (_isAttackedThisFrame)
            {
                _nextFireTime = Time.time + Model.DelayBetweenAttack;
            }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            }
#endif
        }

        private bool TryFireWeapon(WeaponHardPointView weapon)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using (BattleProfilerMarkers.WeaponTryFire.Auto())
            {
#endif
            // MAIN TARGET
            if (_mainAttackData != null && !_mainAttackData.IsDestroyed)
            {
                foreach (var unit in _mainAttackData.Units)
                {
                    if (!unit.IsDestroyed && weapon.CanAttack(unit.Position))
                    {
                        weapon.Attack(_mainAttackData, unit);
                        return true;
                    }
                }
            }

            // ADDITIONAL TARGETS
            for (int i = _attackDataList.Count - 1; i >= 0; i--)
            {
                var attackData = _attackDataList[i];
                if (attackData.IsDestroyed) continue;

                foreach (var unit in attackData.Units)
                {
                    if (!unit.IsDestroyed && weapon.CanAttack(unit.Position))
                    {
                        weapon.Attack(attackData, unit);
                        return true;
                    }
                }
            }

            return false;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            }
#endif
        }
        
        public void ApplyDamage(AttackData attackData, IHardPointModel hardPointModel, WeaponType weaponType, float attackDelay)
        {
            if (_isReleased || !IsTargetValid(attackData, hardPointModel)) return;
            _attackCoordinator.ScheduleImpact(this, attackData, hardPointModel, weaponType, attackDelay);
        }

        public bool CommitImpact(AttackData attackData, IHardPointModel hardPointModel, WeaponType weaponType, int targetId)
        {
            if (_isReleased || hardPointModel.Id != targetId || !IsTargetValid(attackData, hardPointModel))
            {
                return false;
            }

            ApplyDamageInternal(attackData, weaponType, targetId, GetDistance(hardPointModel.Position));
            return true;
        }

        private static bool IsTargetValid(AttackData attackData, IHardPointModel hardPointModel) =>
            !attackData.IsDestroyed && !hardPointModel.IsDestroyed && attackData.Contains(hardPointModel);
        
        private void ApplyDamageInternal(AttackData attackData, WeaponType weaponType, int id, float distance)
        {
            attackData.ApplyDamage(Model.GetDamage(weaponType,distance), weaponType, id);
        }
        private float GetDistance(Vector3 targetPosition) =>
            Vector3.Distance(attackOrigin == null ? transform.position : attackOrigin.position, targetPosition);
    }
}
