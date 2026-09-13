using System;
using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Mvc;
using EmpireAtWar.Services.CoroutineService;
using EmpireAtWar.ViewComponents.Health;
using EmpireAtWar.Mvc;
using UnityEngine;
using UnityEngine.Assertions;
using Utilities.ScriptUtils.Time;
using Zenject;

namespace EmpireAtWar.Components.Weapon
{
    public class WeaponComponent: MonoComponent<WeaponModel>, IWeaponComponent, IInitializable, ITickable, IWeaponPresenter
    {
        [SerializeField] private List<WeaponHardPointView> hardPoints;
        [SerializeField] private Transform attackOrigin;
        [SerializeField] private bool useWeaponDamageRange;
        
        private ICoroutineService _coroutineService;
        private ITimer _attackTimer = TimerFactory.ConstructTimer();
        private List<AttackData> _attackDataList = new List<AttackData>();
        private List<Coroutine> _pendingAttacks = new();
        private AttackData _mainAttackData = null;
        private float _nextFireTime = 0f;
        private int _currentWeaponIndex = 0;
        private bool _isAttackedThisFrame;
        private bool _isReleased;
        public float AttackDistance => Model.OptimalAttackRange;


        [Inject]
        private void Construct(ICoroutineService coroutineService)
        {
            _coroutineService = coroutineService;
        }
        
        public void Initialize()
        {
            if (useWeaponDamageRange)
            {
                Model.SetOptimalAttackRange(hardPoints.Select(hardPoint => hardPoint.WeaponType));
            }

            foreach (WeaponHardPointView hardPoint in hardPoints)
            {
                hardPoint.SetData(Model.ProjectileModel.GetData(hardPoint.WeaponType), Model.OptimalAttackRange, this);
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
            foreach (Coroutine pendingAttack in _pendingAttacks)
            {
                _coroutineService.StopCustomCoroutine(pendingAttack);
            }

            _pendingAttacks.Clear();
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
        }

        private bool TryFireWeapon(WeaponHardPointView weapon)
        {
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
        }
        
        public void ApplyDamage(AttackData attackData, IHardPointModel hardPointModel, WeaponType weaponType, float attackDelay)
        {
            if (_isReleased || !IsTargetValid()) return;

            Coroutine attackCoroutine = null;
            attackCoroutine = _coroutineService.InvokeWithDelay(() =>
            {
                Assert.IsNotNull(attackCoroutine);
                _pendingAttacks.Remove(attackCoroutine);

                if (_isReleased || !IsTargetValid()) return;

                ApplyDamageInternal(
                    attackData,
                    weaponType,
                    hardPointModel.Id,
                    GetDistance(hardPointModel.Position));

            }, attackDelay);

            _pendingAttacks.Add(attackCoroutine);

            bool IsTargetValid()
            {
                if (attackData.IsDestroyed || !attackData.Contains(hardPointModel))
                {
                    Debug.LogWarning("Can not attack hardpoint");
                    return false;
                }

                return true;
            }
        }
        
        private void ApplyDamageInternal(AttackData attackData, WeaponType weaponType, int id, float distance)
        {
            attackData.ApplyDamage(Model.GetDamage(weaponType,distance), weaponType, id);
        }
        private float GetDistance(Vector3 targetPosition) =>
            Vector3.Distance(attackOrigin == null ? transform.position : attackOrigin.position, targetPosition);
    }
}
