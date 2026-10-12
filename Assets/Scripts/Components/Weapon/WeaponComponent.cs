using System;
using System.Collections.Generic;
using System.Diagnostics;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Combat;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Extensions;
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
        // A hull too large for its range stops near hull contact, give or take navigation slack.
        private const float CONTACT_ENGAGE_FACTOR = 1.15f;
        // Continuous weapons such as beams land their damage in ticks this far apart.
        private const float DAMAGE_TICK_INTERVAL = 0.1f;
        // With every candidate beyond reach of all hardpoints the weapon waits this long before selecting again.
        // Target changes (orders, new contacts) cancel the wait.
        private const float IDLE_RESELECT_INTERVAL = 0.25f;

        private ISelectionModelObserver _selection;
        private ILocalPlayer _localPlayer;
        private IPlayerRoster _playerRoster;
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
        // Candidates in priority order: a hardpoint slot in the coordinator's target table and an index into
        // _candidateGroups. A group's candidates are contiguous; groups without candidates are not listed.
        private readonly List<AttackData> _candidateGroups = new List<AttackData>();
        private int[] _candidateSlots = new int[16];
        private int[] _candidateGroupIndices = new int[16];
        private int _candidateCount;
        private byte[] _groupEligibility = new byte[8];
        private readonly HashSet<AttackData> _subscribedGroups = new HashSet<AttackData>();
        // Groups whose target still had live hardpoints at the last rebuild. Destroyed hardpoints drop out
        // lazily, so a group turning into a wreck needs one more rebuild to aim at its dead hardpoints.
        private readonly HashSet<AttackData> _liveGroupsAtRebuild = new HashSet<AttackData>();
        private readonly Dictionary<IHardPointModel, Vector3> _targetPositions = new Dictionary<IHardPointModel, Vector3>();
        private readonly Dictionary<AttackData, bool> _targetGroupEligibility = new Dictionary<AttackData, bool>();
        private readonly List<Vector2> _turnArcs = new List<Vector2>();
        private readonly WeaponFacingSolver _facingSolver = new WeaponFacingSolver();
        private AttackData _mainAttackData = null;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [Inject(Id = EntityBindType.ViewTransform)] private Transform _viewTransform;
        private DebugRangeCircle _attackRangeCircle;
#endif

        private int _currentWeaponIndex = 0;
        private int _targetVersion;
        // Target changes only mark candidates stale; the next reader rebuilds once for all of them.
        private bool _candidatesDirty;
        private float _idleUntil;
        // Salvos waiting for their mount to finish turning.
        private readonly List<PendingSalvo> _turningSalvos = new List<PendingSalvo>();
        // Largest distance between two hardpoints: a candidate farther than range + span from one hardpoint
        // is out of every hardpoint's range.
        private float _hardPointSpan;

        private bool _isReleased;
        private bool _isInitialized;
        // The local team never fires at what its fog of war hides, so no shot flies into the dark.
        private bool _respectsLocalFog;

        public event Action<WeaponProfile, Transform> ShotEmitted;

        public float AttackDistance => Model.MaxWeaponRange;

        [Inject]
        private void Construct(ISelectionModelObserver selection,
            CombatAttackCoordinator attackCoordinator, CombatModifiers modifiers, WeaponsData weaponsData,
            DamageMatrixData damageMatrix, DebugRangeCircleFactory rangeCircleFactory, IncomingMissileRegistry missiles,
            ILocalPlayer localPlayer, IPlayerRoster playerRoster, PlayerId owner)
        {
            _localPlayer = localPlayer;
            _playerRoster = playerRoster;
            _owner = owner;
            _missiles = missiles;
            _attackCoordinator = attackCoordinator;
            _modifiers = modifiers;
            _weaponsData = weaponsData;
            _damageMatrix = damageMatrix;
            _rangeCircleFactory = rangeCircleFactory;
            _selection = selection;
        }

        public void Initialize()
        {
            _isInitialized = true;
            _respectsLocalFog = _localPlayer.IsFriendly(_owner);
            _attackCoordinator.Register(this);

            // Every hardpoint fires to its own weapon's range; nebulae and other vision effects scale it.
            FactionType faction = _playerRoster.Get(_owner).Faction;
            List<WeaponProfile> profiles = new List<WeaponProfile>(hardPoints.Count);
            foreach (WeaponHardPoint hardPoint in hardPoints)
            {
                WeaponProfile profile = _weaponsData.GetProfile(hardPoint.WeaponType, faction);
                profiles.Add(profile);
                hardPoint.SetData(profile, profile.Range * _modifiers.VisionMultiplier, _damageMatrix.MissSpread,
                    this, _attackCoordinator, _modifiers, _impactPresenter, _missiles);
                hardPoint.ShotEmitted += OnShotEmitted;
            }

            Dictionary<ShipClass, float> engagementRanges = new Dictionary<ShipClass, float>();
            float maxRange = 0f;
            foreach (ShipClass targetClass in (ShipClass[])Enum.GetValues(typeof(ShipClass)))
                engagementRanges.Add(targetClass,
                    WeaponEngagementRange.Calculate(profiles, targetClass, _damageMatrix) * _modifiers.VisionMultiplier);
            foreach (WeaponProfile profile in profiles)
                maxRange = Mathf.Max(maxRange, profile.Range * _modifiers.VisionMultiplier);
            Model.SetRanges(maxRange, engagementRanges);
            for (int i = 0; i < hardPoints.Count; i++)
                for (int j = i + 1; j < hardPoints.Count; j++)
                    _hardPointSpan = Mathf.Max(_hardPointSpan,
                        Vector3.Distance(hardPoints[i].transform.position, hardPoints[j].transform.position));
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
                group.UnitsChanged -= InvalidateCandidates;
                group.Destroyed -= InvalidateCandidates;
            }
            _subscribedGroups.Clear();
            _liveGroupsAtRebuild.Clear();
            _candidatesDirty = false;
            ReleaseCandidates();
            _targetGroupEligibility.Clear();
            _targetPositions.Clear();
            _turningSalvos.Clear();
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
                    if (HasSameSource(attackData))
                    {
                        return;
                    }

                    _attackDataList.Add(attackData);
                    Subscribe(attackData);
                    InvalidateCandidates();
                    break;
                }
                case AttackType.MainTarget:
                {
                    AttackData previousMain = _mainAttackData;
                    _mainAttackData = attackData;
                    Subscribe(attackData);

                    if (!HasSameSource(attackData))
                    {
                        _attackDataList.Add(attackData);
                    }

                    UnsubscribeIfUnused(previousMain);
                    InvalidateCandidates();
                    break;
                }
            }
        }

        private bool CanAcquire(AttackData group) =>
            group.CanAcquireTarget && !(_respectsLocalFog && group.TargetEntity.IsHiddenByFog());

        public float GetEngagementRange(ShipClass targetClass) => Model.GetEngagementRange(targetClass);

        public bool HasEnoughRange(float distance, float contactDistance, ShipClass targetClass)
        {
            return distance <= Mathf.Max(Model.GetEngagementRange(targetClass) * ENGAGE_RANGE_FACTOR,
                contactDistance * CONTACT_ENGAGE_FACTOR);
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
            InvalidateCandidates();
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
            _attackRangeCircle.Draw(_viewTransform.position, Model.MaxWeaponRange);
#endif
            if (hardPoints == null || hardPoints.Count == 0)
                return;

            FireTurnedSalvos();
            if (Time.time < _idleUntil)
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

        internal int TargetVersion => _targetVersion;
        internal int CandidateCount => _candidateCount;
        internal int CandidateGroupCount => _candidateGroups.Count;
        internal int[] CandidateSlots => _candidateSlots;
        internal int[] CandidateGroupIndices => _candidateGroupIndices;
        internal byte[] GroupEligibility => _groupEligibility;

        internal AttackData GetCandidateGroup(int index) => _candidateGroups[index];

        /// <summary>Readies this frame's candidates: rebuilds stale ones and decides each group's eligibility once.</summary>
        internal void PrepareTargetSelection(TargetSelectionCapture capture)
        {
            EnsureCandidates();
            _targetGroupEligibility.Clear();
            for (int i = 0; i < _candidateGroups.Count; i++)
            {
                AttackData group = _candidateGroups[i];
                if (!_targetGroupEligibility.TryGetValue(group, out bool canAcquire))
                {
                    canAcquire = capture.CanAcquire(group, _respectsLocalFog);
                    _targetGroupEligibility.Add(group, canAcquire);
                    // A new wreck makes its destroyed hardpoints valid aim points again.
                    if (_liveGroupsAtRebuild.Contains(group) && capture.IsWreck(group))
                    {
                        AttackSequenceDiagnostics.RecordTargetInvalidation();
                        InvalidateCandidates();
                    }
                }
                _groupEligibility[i] = canAcquire ? (byte)1 : (byte)0;
            }
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
            EnsureCandidates();
            _targetPositions.Clear();
            TargetSelectionCapture targets = _attackCoordinator.TargetUnits;

            for (int i = 0; i < _candidateCount; i++)
            {
                AttackData group = _candidateGroups[_candidateGroupIndices[i]];
                IHardPointModel unit = targets.GetUnit(_candidateSlots[i]);
                if (!CanAcquire(group)) continue;
                if (!group.CanTarget(unit)) continue;

                if (!weapon.CanEngage(group.TargetClass)) continue;

                AttackSequenceDiagnostics.RecordCandidateVisit();
                Vector3 position = GetTargetPosition(_targetPositions, unit);
                bool canAttack = WeaponTargetSelector.TryCalculateAim(position, origin, parentRotation,
                    weapon.MaxAttackDistance, weapon.MinYaw, weapon.MaxYaw,
                    out _, out _);

                if (canAttack)
                {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    AttackSequenceDiagnostics.RecordTargetSelectionTime(selectionStart);
#endif
                    Engage(weapon, group, unit);
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
            WeaponTargetSelectionJob.Result result, AttackData group, IHardPointModel unit, int generation)
        {
            if (_isReleased || weapon.IsDestroyed || weapon.IsBusy || _modifiers.IsCloaked) return;

            if (_targetVersion != targetVersion || result.CandidateIndex >= 0 &&
                (!CanAcquire(group) || !IsTargetValid(group, unit) || unit.Generation != generation))
            {
                AttackSequenceDiagnostics.RecordTargetSelectionFallback(_targetVersion != targetVersion
                    ? WeaponTargetSelectionJob.FALLBACK_TARGET_VERSION
                    : WeaponTargetSelectionJob.FALLBACK_INVALID_CANDIDATE);
                CommitTargetSelectionSerial(weapon);
                return;
            }

            if (result.CandidateIndex < 0)
            {
                if (result.NearestDistance > Model.MaxWeaponRange + _hardPointSpan)
                    _idleUntil = Time.time + IDLE_RESELECT_INTERVAL;
                return;
            }

            Engage(weapon, group, unit);
        }

        // A turret turns to the target first; the salvo starts once the mount points at it.
        private void Engage(WeaponHardPoint weapon, AttackData group, IHardPointModel unit)
        {
            float turnSeconds = weapon.TurnTowards(unit.Position);
            if (turnSeconds <= 0f)
            {
                weapon.Attack(group, unit);
                return;
            }

            _turningSalvos.Add(new PendingSalvo
            {
                HardPoint = weapon,
                Group = group,
                Unit = unit,
                FireTime = Time.time + turnSeconds
            });
        }

        // The target is checked again when the turn ends: it may have died, hidden or left range meanwhile.
        private void FireTurnedSalvos()
        {
            for (int i = _turningSalvos.Count - 1; i >= 0; i--)
            {
                PendingSalvo salvo = _turningSalvos[i];
                if (Time.time < salvo.FireTime) continue;
                _turningSalvos.RemoveAt(i);
                WeaponHardPoint weapon = salvo.HardPoint;
                if (weapon.IsDestroyed || _modifiers.IsCloaked || !CanAcquire(salvo.Group) ||
                    !IsTargetValid(salvo.Group, salvo.Unit)) continue;

                Transform weaponTransform = weapon.transform;
                Quaternion parentRotation = weaponTransform.parent == null
                    ? Quaternion.identity : weaponTransform.parent.rotation;
                if (!WeaponTargetSelector.TryCalculateAim(salvo.Unit.Position, weaponTransform.position,
                        parentRotation, weapon.MaxAttackDistance, weapon.MinYaw, weapon.MaxYaw, out _, out _)) continue;
                weapon.Attack(salvo.Group, salvo.Unit);
            }
        }

        internal void CommitTargetSelectionSerial(WeaponHardPoint weapon)
        {
            if (_isReleased || weapon.IsDestroyed || weapon.IsBusy || _modifiers.IsCloaked) return;
            TryFireWeapon(weapon);
        }

        private void Subscribe(AttackData group)
        {
            if (!_subscribedGroups.Add(group)) return;
            group.UnitsChanged += InvalidateCandidates;
            group.Destroyed += InvalidateCandidates;
        }

        private void UnsubscribeIfUnused(AttackData group)
        {
            if (group == null || ReferenceEquals(group, _mainAttackData) || _attackDataList.Contains(group)) return;
            if (!_subscribedGroups.Remove(group)) return;
            group.UnitsChanged -= InvalidateCandidates;
            group.Destroyed -= InvalidateCandidates;
        }

        // The version changes immediately so selections captured before the change fall back at commit.
        private void InvalidateCandidates()
        {
            _targetVersion++;
            _candidatesDirty = true;
            _idleUntil = 0f;
        }

        private void EnsureCandidates()
        {
            if (!_candidatesDirty) return;
            _candidatesDirty = false;
            RebuildCandidates();
        }

        private void RebuildCandidates()
        {
            _targetVersion++;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            long rebuildStart = Stopwatch.GetTimestamp();
#endif
            _liveGroupsAtRebuild.Clear();
            ReleaseCandidates();
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
            if (group.TargetHealth.HasLiveHardPoints) _liveGroupsAtRebuild.Add(group);
            TargetSelectionCapture targets = _attackCoordinator.TargetUnits;
            int groupIndex = _candidateGroups.Count;
            int firstCandidate = _candidateCount;
            List<IHardPointModel> units = group.Units;
            for (int i = 0; i < units.Count; i++)
            {
                IHardPointModel unit = units[i];
                if (!group.CanTarget(unit)) continue;
                if (_candidateCount == _candidateSlots.Length)
                {
                    Array.Resize(ref _candidateSlots, _candidateCount * 2);
                    Array.Resize(ref _candidateGroupIndices, _candidateCount * 2);
                }
                _candidateSlots[_candidateCount] = targets.Acquire(unit, group.TargetHealth);
                _candidateGroupIndices[_candidateCount] = groupIndex;
                _candidateCount++;
            }

            if (_candidateCount == firstCandidate) return;
            _candidateGroups.Add(group);
            if (_groupEligibility.Length < _candidateGroups.Count)
                Array.Resize(ref _groupEligibility, _groupEligibility.Length * 2);
        }

        private void ReleaseCandidates()
        {
            TargetSelectionCapture targets = _attackCoordinator.TargetUnits;
            for (int i = 0; i < _candidateCount; i++) targets.Release(_candidateSlots[i]);
            _candidateCount = 0;
            _candidateGroups.Clear();
        }

        // Destroyed groups stay listed until the next rebuild; they no longer count as targets.
        public bool HasTarget(IHealthFacade healthFacade)
        {
            for (int i = 0; i < _attackDataList.Count; i++)
                if (!_attackDataList[i].IsDestroyed && _attackDataList[i].IsFrom(healthFacade)) return true;
            return false;
        }

        private bool HasSameSource(AttackData attackData)
        {
            for (int i = 0; i < _attackDataList.Count; i++)
                if (!_attackDataList[i].IsDestroyed && attackData.SameSource(_attackDataList[i])) return true;
            return false;
        }

        private static Vector3 GetTargetPosition(Dictionary<IHardPointModel, Vector3> positions, IHardPointModel unit)
        {
            if (positions.TryGetValue(unit, out Vector3 position)) return position;
            position = unit.Position;
            positions.Add(unit, position);
            AttackSequenceDiagnostics.RecordTargetSnapshot();
            return position;
        }

        public bool RollHit(AttackData attackData, WeaponProfile profile) =>
            Model.RollHit(profile.DamageType, attackData.TargetClass,
                profile.AccuracyMultiplier * profile.GetAccuracyBias(attackData.TargetClass), UnityEngine.Random.value);

        public void ApplyDamage(AttackData attackData, IHardPointModel hardPointModel, WeaponProfile profile, float attackDelay,
            float damageDuration, IncomingMissile missile)
        {
            if (_isReleased || !IsTargetValid(attackData, hardPointModel)) return;
            float damage = profile.Damage * profile.GetDamageBias(attackData.TargetClass) *
                           _modifiers.GetDamageMultiplier(attackData.TargetEntity);
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

        private struct PendingSalvo
        {
            public WeaponHardPoint HardPoint;
            public AttackData Group;
            public IHardPointModel Unit;
            public float FireTime;
        }
    }
}
