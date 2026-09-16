using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private struct TargetCandidate
        {
            public AttackData Group;
            public IHardPointModel Unit;
        }

        [SerializeField] private List<WeaponHardPointView> hardPoints;
        [SerializeField] private Transform attackOrigin;
        [SerializeField] private bool useWeaponDamageRange;
        
        private CombatAttackCoordinator _attackCoordinator;
        private ITimer _attackTimer = TimerFactory.ConstructTimer();
        private List<AttackData> _attackDataList = new List<AttackData>();
        private readonly List<TargetCandidate> _orderedCandidates = new List<TargetCandidate>();
        private readonly HashSet<AttackData> _subscribedGroups = new HashSet<AttackData>();
        private readonly Dictionary<IHardPointModel, Action> _unitDestroyedHandlers = new Dictionary<IHardPointModel, Action>();
        private readonly Dictionary<IHardPointModel, Vector3> _targetPositions = new Dictionary<IHardPointModel, Vector3>();
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

            foreach (AttackData group in _subscribedGroups)
            {
                group.UnitsChanged -= RebuildCandidates;
                group.Destroyed -= RebuildCandidates;
            }
            foreach (KeyValuePair<IHardPointModel, Action> handler in _unitDestroyedHandlers)
                handler.Key.OnDestroyed -= handler.Value;
            _subscribedGroups.Clear();
            _unitDestroyedHandlers.Clear();
            _orderedCandidates.Clear();
            _targetPositions.Clear();
            _attackDataList.Clear();
            _mainAttackData = null;
        }

        public void AddTarget(AttackData attackData, AttackType attackType)
        {
            if (_isReleased) return;

            switch (attackType)
            {
                case AttackType.Base:
                {
                    if (_attackDataList.Any(data => attackData.SameSource(data)))
                    {
                        return;
                    }

                    _attackDataList.Add(attackData);
                    Subscribe(attackData);
                    RebuildCandidates();
                    break;
                }
                case AttackType.MainTarget:
                {
                    AttackData previousMain = _mainAttackData;
                    _mainAttackData = attackData;
                    Subscribe(attackData);

                    if (!_attackDataList.Any(data => attackData.SameSource(data)))
                    {
                        _attackDataList.Add(attackData);
                    }

                    UnsubscribeIfUnused(previousMain);
                    RebuildCandidates();
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
            AttackData previousMain = _mainAttackData;
            _mainAttackData = null;
            UnsubscribeIfUnused(previousMain);
            RebuildCandidates();
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
            long selectionStart = Stopwatch.GetTimestamp();
#endif
            Transform weaponTransform = weapon.transform;
            Vector3 origin = weaponTransform.position;
            Quaternion parentRotation = weaponTransform.parent == null
                ? Quaternion.identity : weaponTransform.parent.rotation;
            Quaternion lastAim = default;
            bool hasAim = false;
            _targetPositions.Clear();

            for (int i = 0; i < _orderedCandidates.Count; i++)
            {
                TargetCandidate candidate = _orderedCandidates[i];
                if (candidate.Group.IsDestroyed || candidate.Unit.IsDestroyed)
                {
                    _orderedCandidates.RemoveAt(i--);
                    AttackSequenceDiagnostics.RecordTargetInvalidation();
                    continue;
                }

                AttackSequenceDiagnostics.RecordCandidateVisit();
                Vector3 position = GetTargetPosition(candidate.Unit);
                bool canAttack = WeaponTargetSelector.TryCalculateAim(position, origin, parentRotation,
                    weapon.MaxAttackDistance, weapon.MinYaw, weapon.MaxYaw,
                    out Quaternion aim, out bool inRange);
                if (inRange)
                {
                    lastAim = aim;
                    hasAim = true;
                }

                if (canAttack)
                {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    AttackSequenceDiagnostics.RecordTargetSelectionTime(selectionStart);
#endif
                    weapon.ApplyAim(aim);
                    weapon.Attack(candidate.Group, candidate.Unit);
                    return true;
                }
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            AttackSequenceDiagnostics.RecordTargetSelectionTime(selectionStart);
#endif
            if (hasAim) weapon.ApplyAim(lastAim);
            return false;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            }
#endif
        }

        private void Subscribe(AttackData group)
        {
            if (!_subscribedGroups.Add(group)) return;
            group.UnitsChanged += RebuildCandidates;
            group.Destroyed += RebuildCandidates;
        }

        private void UnsubscribeIfUnused(AttackData group)
        {
            if (group == null || ReferenceEquals(group, _mainAttackData) || _attackDataList.Contains(group)) return;
            if (!_subscribedGroups.Remove(group)) return;
            group.UnitsChanged -= RebuildCandidates;
            group.Destroyed -= RebuildCandidates;
        }

        private void RebuildCandidates()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            long rebuildStart = Stopwatch.GetTimestamp();
#endif
            foreach (KeyValuePair<IHardPointModel, Action> handler in _unitDestroyedHandlers)
                handler.Key.OnDestroyed -= handler.Value;
            _unitDestroyedHandlers.Clear();
            _orderedCandidates.Clear();
            if (_mainAttackData != null)
            {
                if (_mainAttackData.IsDestroyed)
                {
                    AttackData destroyedMain = _mainAttackData;
                    _mainAttackData = null;
                    UnsubscribeIfUnused(destroyedMain);
                    AttackSequenceDiagnostics.RecordTargetInvalidation();
                }
                else
                    AddCandidates(_mainAttackData);
            }

            for (int i = _attackDataList.Count - 1; i >= 0; i--)
            {
                AttackData group = _attackDataList[i];
                if (group.IsDestroyed)
                {
                    _attackDataList.RemoveAt(i);
                    UnsubscribeIfUnused(group);
                    AttackSequenceDiagnostics.RecordTargetInvalidation();
                    continue;
                }

                AddCandidates(group);
            }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            AttackSequenceDiagnostics.RecordCandidateRebuildTime(rebuildStart);
#endif
        }

        private void AddCandidates(AttackData group)
        {
            List<IHardPointModel> units = group.Units;
            for (int i = 0; i < units.Count; i++)
                if (!units[i].IsDestroyed)
                {
                    _orderedCandidates.Add(new TargetCandidate { Group = group, Unit = units[i] });
                    if (_unitDestroyedHandlers.ContainsKey(units[i])) continue;
                    IHardPointModel unit = units[i];
                    Action handler = () => OnUnitDestroyed(unit);
                    _unitDestroyedHandlers.Add(unit, handler);
                    unit.OnDestroyed += handler;
                }
        }

        private void OnUnitDestroyed(IHardPointModel unit)
        {
            Action handler = _unitDestroyedHandlers[unit];
            unit.OnDestroyed -= handler;
            _unitDestroyedHandlers.Remove(unit);
            for (int i = _orderedCandidates.Count - 1; i >= 0; i--)
                if (ReferenceEquals(_orderedCandidates[i].Unit, unit))
                    _orderedCandidates.RemoveAt(i);
            AttackSequenceDiagnostics.RecordTargetInvalidation();
        }

        private Vector3 GetTargetPosition(IHardPointModel unit)
        {
            if (_targetPositions.TryGetValue(unit, out Vector3 position)) return position;
            position = unit.Position;
            _targetPositions.Add(unit, position);
            AttackSequenceDiagnostics.RecordTargetSnapshot();
            return position;
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
