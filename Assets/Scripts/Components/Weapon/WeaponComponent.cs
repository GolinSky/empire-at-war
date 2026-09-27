using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Combat;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Mvc;
using EmpireAtWar.ViewComponents.Health;
using EmpireAtWar.Services.Timing;
using UnityEngine;
using Utilities.ScriptUtils.Time;
using Zenject;

namespace EmpireAtWar.Components.Weapon
{
    public class WeaponComponent: MonoComponent<WeaponModel>, IWeaponComponent, IInitializable, ITickable, IWeaponPresenter,
        IWeaponFireEvents, IWeaponFacing
    {
        private struct TargetCandidate
        {
            public AttackData Group;
            public IHardPointModel Unit;
        }

        internal struct TargetSelectionCandidate
        {
            public AttackData Group;
            public IHardPointModel Unit;
            public int Generation;
            public Vector3 Position;
        }

        [SerializeField] private List<WeaponHardPoint> hardPoints;
        [SerializeField] private bool useWeaponDamageRange;
        
        private CombatAttackCoordinator _attackCoordinator;
        private CombatModifiers _modifiers;
        [Inject] private ImpactEffectPresenter _impactPresenter;
        private ITimer _attackTimer = TimerFactory.ConstructTimer();
        private List<AttackData> _attackDataList = new List<AttackData>();
        private readonly List<TargetCandidate> _orderedCandidates = new List<TargetCandidate>();
        private readonly HashSet<AttackData> _subscribedGroups = new HashSet<AttackData>();
        private readonly Dictionary<IHardPointModel, Action> _unitDestroyedHandlers = new Dictionary<IHardPointModel, Action>();
        private readonly Dictionary<IHardPointModel, Vector3> _targetPositions = new Dictionary<IHardPointModel, Vector3>();
        private readonly List<TargetSelectionCandidate> _targetSelectionCandidates = new List<TargetSelectionCandidate>();
        private readonly List<Vector2> _turnArcs = new List<Vector2>();
        private readonly WeaponFacingSolver _facingSolver = new WeaponFacingSolver();
        private AttackData _mainAttackData = null;
        private int _currentWeaponIndex = 0;
        private int _targetVersion;
        private bool _isReleased;
        public float AttackDistance => Model.OptimalAttackRange;
        public event Action<WeaponProfile, Transform> ShotEmitted;


        [Inject]
        private void Construct(CombatAttackCoordinator attackCoordinator, CombatModifiers modifiers)
        {
            _attackCoordinator = attackCoordinator;
            _modifiers = modifiers;
        }
        
        public void Initialize()
        {
            _attackCoordinator.Register(this);
            if (useWeaponDamageRange)
            {
                Model.SetOptimalAttackRange(hardPoints.Select(hardPoint => hardPoint.WeaponType));
            }

            foreach (WeaponHardPoint hardPoint in hardPoints)
            {
                hardPoint.SetData(Model.GetProfile(hardPoint.WeaponType), Model.OptimalAttackRange, Model.MissSpread,
                    this, _attackCoordinator, _modifiers, _impactPresenter);
                hardPoint.ShotEmitted += OnShotEmitted;
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
            foreach (WeaponHardPoint hardPoint in hardPoints)
            {
                hardPoint.ShotEmitted -= OnShotEmitted;
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
            _targetSelectionCandidates.Clear();
            _targetPositions.Clear();
            _attackDataList.Clear();
            _mainAttackData = null;
        }

        private void OnShotEmitted(WeaponProfile profile, Transform muzzle)
        {
            if (ShotEmitted != null) ShotEmitted.Invoke(profile, muzzle);
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

        public float GetFiringTurnAngle(Vector3 targetPosition)
        {
            _turnArcs.Clear();
            foreach (WeaponHardPoint hardPoint in hardPoints)
            {
                if (hardPoint.IsDestroyed) continue;
                Transform weaponTransform = hardPoint.transform;
                Quaternion aim = Quaternion.LookRotation(targetPosition - weaponTransform.position, Vector3.up);
                float localYaw = Mathf.DeltaAngle(0f, (Quaternion.Inverse(weaponTransform.parent.rotation) * aim).eulerAngles.y);
                _turnArcs.Add(new Vector2(localYaw - hardPoint.MaxYaw, localYaw - hardPoint.MinYaw));
            }

            return _facingSolver.FindTurn(_turnArcs);
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

            if (hardPoints == null || hardPoints.Count == 0)
                return;

            WeaponHardPoint weapon = hardPoints[_currentWeaponIndex];
            if (!weapon.IsDestroyed && !weapon.IsBusy)
            {
                _attackCoordinator.QueueTargetSelection(this, weapon);
            }

            _currentWeaponIndex++;
            if (_currentWeaponIndex >= hardPoints.Count)
                _currentWeaponIndex = 0;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            }
#endif
        }

        internal IReadOnlyList<TargetSelectionCandidate> CaptureTargetSelection(WeaponHardPoint weapon,
            out int targetVersion, out Vector3 origin, out Quaternion parentRotation)
        {
            _targetPositions.Clear();
            _targetSelectionCandidates.Clear();
            bool removedInvalidCandidate = false;

            for (int i = 0; i < _orderedCandidates.Count; i++)
            {
                TargetCandidate candidate = _orderedCandidates[i];
                if (!candidate.Group.CanTarget(candidate.Unit))
                {
                    _orderedCandidates.RemoveAt(i--);
                    AttackSequenceDiagnostics.RecordTargetInvalidation();
                    removedInvalidCandidate = true;
                    continue;
                }

                _targetSelectionCandidates.Add(new TargetSelectionCandidate
                {
                    Group = candidate.Group,
                    Unit = candidate.Unit,
                    Generation = candidate.Unit.Generation,
                    Position = GetTargetPosition(candidate.Unit)
                });
            }

            if (removedInvalidCandidate) _targetVersion++;

            Transform weaponTransform = weapon.transform;
            targetVersion = _targetVersion;
            origin = weaponTransform.position;
            parentRotation = weaponTransform.parent == null ? Quaternion.identity : weaponTransform.parent.rotation;
            return _targetSelectionCandidates;
        }

        private bool TryFireWeapon(WeaponHardPoint weapon)
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
                if (!candidate.Group.CanTarget(candidate.Unit))
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

        internal void CommitTargetSelection(WeaponHardPoint weapon, int targetVersion,
            WeaponTargetSelectionJob.Result result, TargetSelectionCandidate selectedCandidate)
        {
            if (_isReleased || weapon.IsDestroyed || weapon.IsBusy) return;

            if (_targetVersion != targetVersion || result.CandidateIndex >= 0 &&
                (!IsTargetValid(selectedCandidate.Group, selectedCandidate.Unit) ||
                 selectedCandidate.Unit.Generation != selectedCandidate.Generation))
            {
                AttackSequenceDiagnostics.RecordTargetSelectionFallback();
                CommitTargetSelectionSerial(weapon);
                return;
            }

            if (result.CandidateIndex < 0)
            {
                if (result.HasInRangeAim != 0) weapon.ApplyAim(ToQuaternion(result.LastInRangeAim));
                return;
            }

            weapon.ApplyAim(ToQuaternion(result.SelectedAim));
            weapon.Attack(selectedCandidate.Group, selectedCandidate.Unit);
        }

        internal void CommitTargetSelectionSerial(WeaponHardPoint weapon)
        {
            if (_isReleased || weapon.IsDestroyed || weapon.IsBusy) return;
            TryFireWeapon(weapon);
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
            _targetVersion++;
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
                if (group.CanTarget(units[i]))
                {
                    _orderedCandidates.Add(new TargetCandidate { Group = group, Unit = units[i] });
                    if (units[i].IsDestroyed || _unitDestroyedHandlers.ContainsKey(units[i])) continue;
                    IHardPointModel unit = units[i];
                    Action handler = () => OnUnitDestroyed(unit);
                    _unitDestroyedHandlers.Add(unit, handler);
                    unit.OnDestroyed += handler;
                }
        }

        // Rebuild instead of removing one candidate: the last destroyed hardpoint turns the ship into a wreck target.
        private void OnUnitDestroyed(IHardPointModel unit)
        {
            AttackSequenceDiagnostics.RecordTargetInvalidation();
            RebuildCandidates();
        }

        private Vector3 GetTargetPosition(IHardPointModel unit)
        {
            if (_targetPositions.TryGetValue(unit, out Vector3 position)) return position;
            position = unit.Position;
            _targetPositions.Add(unit, position);
            AttackSequenceDiagnostics.RecordTargetSnapshot();
            return position;
        }
        
        public bool RollHit(AttackData attackData, WeaponProfile profile) =>
            Model.RollHit(profile.DamageType, attackData.TargetClass);

        public void ApplyDamage(AttackData attackData, IHardPointModel hardPointModel, WeaponProfile profile, float attackDelay)
        {
            if (_isReleased || !IsTargetValid(attackData, hardPointModel)) return;
            _attackCoordinator.ScheduleImpact(this, attackData, hardPointModel,
                profile.Damage * _modifiers.DamageMultiplier, profile.DamageType, attackDelay);
        }

        public bool CommitImpact(AttackData attackData, IHardPointModel hardPointModel, float damage,
            DamageType damageType, int targetId)
        {
            if (_isReleased || hardPointModel.Id != targetId || !IsTargetValid(attackData, hardPointModel))
            {
                return false;
            }

            attackData.ApplyDamage(damage, damageType, targetId);
            return true;
        }

        private static bool IsTargetValid(AttackData attackData, IHardPointModel hardPointModel) =>
            attackData.CanTarget(hardPointModel) && attackData.Contains(hardPointModel);

        private static Quaternion ToQuaternion(Unity.Mathematics.float4 value) =>
            new Quaternion(value.x, value.y, value.z, value.w);
    }
}
