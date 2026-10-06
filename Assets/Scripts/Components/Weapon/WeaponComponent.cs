using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Combat;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Mvc;
using EmpireAtWar.ViewComponents.Health;
using EmpireAtWar.Services.Timing;
using EmpireAtWar.Models.Selection;
using EmpireAtWar.Utils;
using UnityEngine;
using Utilities.ScriptUtils.Time;
using Zenject;

namespace EmpireAtWar.Components.Weapon
{
    public class WeaponComponent: MonoComponent<WeaponModel>, IWeaponComponent, IInitializable, ITickable, IWeaponPresenter,
        IWeaponFireEvents, IWeaponFacing
    {
        // The ship stops a bit inside its range so hardpoints on the far side of the hull still reach.
        private const float ENGAGE_RANGE_FACTOR = 0.8f;
        // Continuous weapons such as beams land their damage in ticks this far apart.
        private const float DAMAGE_TICK_INTERVAL = 0.1f;

        private IWeaponRangeData _rangeData;
        private ISelectionModelObserver _selection;
        private ILocalPlayer _localPlayer;
        private PlayerId _owner;
        private ITimer _attackTimer = TimerFactory.ConstructTimer();

        [SerializeField] private List<WeaponHardPoint> hardPoints;
        private CombatAttackCoordinator _attackCoordinator;
        private CombatModifiers _modifiers;
        private WeaponsData _weaponsData;
        private DamageMatrixData _damageMatrix;
        private IncomingMissileRegistry _missiles;
        private DebugRangeCircleFactory _rangeCircleFactory;
        [Inject] private ImpactEffectPresenter _impactPresenter;
        private List<AttackData> _attackDataList = new List<AttackData>();
        private readonly List<TargetCandidate> _orderedCandidates = new List<TargetCandidate>();
        private readonly HashSet<AttackData> _subscribedGroups = new HashSet<AttackData>();
        private readonly Dictionary<IHardPointModel, Action> _unitDestroyedHandlers = new Dictionary<IHardPointModel, Action>();
        private readonly Dictionary<IHardPointModel, Vector3> _targetPositions = new Dictionary<IHardPointModel, Vector3>();
        private readonly List<TargetSelectionCandidate> _targetSelectionCandidates = new List<TargetSelectionCandidate>();
        private readonly List<Vector2> _turnArcs = new List<Vector2>();
        private readonly WeaponFacingSolver _facingSolver = new WeaponFacingSolver();
        private AttackData _mainAttackData = null;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private DebugRangeCircle _attackRangeCircle;
#endif

        private int _currentWeaponIndex = 0;
        private int _targetVersion;

        private bool _isReleased;
        private bool _isInitialized;
        // The local team never fires at what its fog of war hides, so no shot flies into the dark.
        private bool _respectsLocalFog;

        public event Action<WeaponProfile, Transform> ShotEmitted;

        public float AttackDistance => Model.OptimalAttackRange;

        [Inject]
        private void Construct(IWeaponRangeData rangeData, ISelectionModelObserver selection,
            CombatAttackCoordinator attackCoordinator, CombatModifiers modifiers, WeaponsData weaponsData,
            DamageMatrixData damageMatrix, DebugRangeCircleFactory rangeCircleFactory, IncomingMissileRegistry missiles,
            ILocalPlayer localPlayer, PlayerId owner)
        {
            _localPlayer = localPlayer;
            _owner = owner;
            _missiles = missiles;
            _attackCoordinator = attackCoordinator;
            _modifiers = modifiers;
            _weaponsData = weaponsData;
            _damageMatrix = damageMatrix;
            _rangeData = rangeData;
            _rangeCircleFactory = rangeCircleFactory;
            _selection = selection;
        }

        public void Initialize()
        {
            _isInitialized = true;
            _respectsLocalFog = _localPlayer.IsFriendly(_owner);
            _attackCoordinator.Register(this);
            Model.SetAttackRange(_rangeData.WeaponRange * _modifiers.VisionMultiplier);

            foreach (WeaponHardPoint hardPoint in hardPoints)
            {
                hardPoint.SetData(_weaponsData.GetProfile(hardPoint.WeaponType), Model.OptimalAttackRange, _damageMatrix.MissSpread,
                    this, _attackCoordinator, _modifiers, _impactPresenter, _missiles);
                hardPoint.ShotEmitted += OnShotEmitted;
            }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _attackRangeCircle = _rangeCircleFactory.Create("AttackRange", Color.red, _selection);
#endif
        }

        private void OnDestroy()
        {
            // Editor previews (e.g. icon rendering) instantiate the prefab without injection or Initialize.
            if (!_isInitialized) return;
            Release();
        }

        public override void Release()
        {
            if (_isReleased)
            {
                return;
            }

            _isReleased = true;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (_attackRangeCircle != null) { _attackRangeCircle.Destroy(); _attackRangeCircle = null; }
#endif
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

        private bool CanAcquire(AttackData group) =>
            group.CanAcquireTarget && !(_respectsLocalFog && group.TargetEntity.IsHiddenByFog());

        public bool HasEnoughRange(float distance)
        {
            return distance <= Model.OptimalAttackRange * ENGAGE_RANGE_FACTOR;
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

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _attackRangeCircle.Draw(transform.position, Model.OptimalAttackRange);
#endif
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
                if (!CanAcquire(candidate.Group)) continue;
                if (!candidate.Group.CanTarget(candidate.Unit))
                {
                    _orderedCandidates.RemoveAt(i--);
                    AttackSequenceDiagnostics.RecordTargetInvalidation();
                    removedInvalidCandidate = true;
                    continue;
                }

                if (!weapon.CanEngage(candidate.Group.TargetClass)) continue;

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
            _targetPositions.Clear();

            for (int i = 0; i < _orderedCandidates.Count; i++)
            {
                TargetCandidate candidate = _orderedCandidates[i];
                if (!CanAcquire(candidate.Group)) continue;
                if (!candidate.Group.CanTarget(candidate.Unit))
                {
                    _orderedCandidates.RemoveAt(i--);
                    AttackSequenceDiagnostics.RecordTargetInvalidation();
                    continue;
                }

                if (!weapon.CanEngage(candidate.Group.TargetClass)) continue;

                AttackSequenceDiagnostics.RecordCandidateVisit();
                Vector3 position = GetTargetPosition(candidate.Unit);
                bool canAttack = WeaponTargetSelector.TryCalculateAim(position, origin, parentRotation,
                    weapon.MaxAttackDistance, weapon.MinYaw, weapon.MaxYaw,
                    out _, out _);

                if (canAttack)
                {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    AttackSequenceDiagnostics.RecordTargetSelectionTime(selectionStart);
#endif
                    weapon.Attack(candidate.Group, candidate.Unit);
                    return true;
                }
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            AttackSequenceDiagnostics.RecordTargetSelectionTime(selectionStart);
#endif
            return false;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            }
#endif
        }

        internal void CommitTargetSelection(WeaponHardPoint weapon, int targetVersion,
            WeaponTargetSelectionJob.Result result, TargetSelectionCandidate selectedCandidate)
        {
            if (_isReleased || weapon.IsDestroyed || weapon.IsBusy || _modifiers.IsCloaked) return;

            if (_targetVersion != targetVersion || result.CandidateIndex >= 0 &&
                (!CanAcquire(selectedCandidate.Group) || !IsTargetValid(selectedCandidate.Group, selectedCandidate.Unit) ||
                 selectedCandidate.Unit.Generation != selectedCandidate.Generation))
            {
                AttackSequenceDiagnostics.RecordTargetSelectionFallback();
                CommitTargetSelectionSerial(weapon);
                return;
            }

            if (result.CandidateIndex < 0)
            {
                return;
            }

            weapon.Attack(selectedCandidate.Group, selectedCandidate.Unit);
        }

        internal void CommitTargetSelectionSerial(WeaponHardPoint weapon)
        {
            if (_isReleased || weapon.IsDestroyed || weapon.IsBusy || _modifiers.IsCloaked) return;
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
            group.RefreshStaleUnits();
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
            Model.RollHit(profile.DamageType, attackData.TargetClass, UnityEngine.Random.value);

        public void ApplyDamage(AttackData attackData, IHardPointModel hardPointModel, WeaponProfile profile, float attackDelay,
            float damageDuration, IncomingMissile missile)
        {
            if (_isReleased || !IsTargetValid(attackData, hardPointModel)) return;
            float damage = profile.Damage * _modifiers.GetDamageMultiplier(attackData.TargetEntity);
            int ticks = Mathf.Max(1, Mathf.CeilToInt(damageDuration / DAMAGE_TICK_INTERVAL));
            for (int i = 0; i < ticks; i++)
            {
                _attackCoordinator.ScheduleImpact(this, attackData, hardPointModel, damage / ticks, profile.DamageType,
                    attackDelay + damageDuration * i / ticks, missile);
            }
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

        private struct TargetCandidate
        {
            public IHardPointModel Unit;

            public AttackData Group;
        }

        internal struct TargetSelectionCandidate
        {
            public IHardPointModel Unit;

            public AttackData Group;

            public Vector3 Position;

            public int Generation;
        }

    }
}
