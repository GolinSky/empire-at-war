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
using EmpireAtWar.ViewComponents.Weapon;
using EmpireAtWar.Services.Timing;
using EmpireAtWar.Services.Vision;
using EmpireAtWar.Models.Selection;
using EmpireAtWar.Utils;
using UnityEngine;
using Utilities.ScriptUtils.Time;
using VContainer;
using EmpireAtWar.Lifecycle;

namespace EmpireAtWar.Components.Weapon
{
    public class WeaponComponent: MonoComponent<WeaponModel>, IWeaponComponent, IInitializable, ITickable, IWeaponPresenter,
        IWeaponFireEvents, IWeaponFacing, IRespawnable
    {
        // The ship stops a bit inside its range so hardpoints on the far side of the hull still reach.
        private const float ENGAGE_RANGE_FACTOR = 0.8f;
        // A hull too large for its range stops near hull contact, give or take navigation slack.
        private const float CONTACT_ENGAGE_FACTOR = 1.15f;
        // Continuous weapons such as beams land their damage in ticks this far apart.
        public const float DAMAGE_TICK_INTERVAL = 0.1f;
        // A hardpoint that found nothing to fire at waits a random time in this range before selecting again,
        // so retries spread across frames. Target changes cancel the wait.
        private const float MIN_RETRY_INTERVAL = 0.25f;
        private const float MAX_RETRY_INTERVAL = 0.5f;

        private ISelectionModelObserver _selection;
        private IVisionService _visionService;
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
        private ImpactEffectPresenter _impactPresenter;
        private BoltFlightBatch _boltFlights;
        private ShotPoolPrewarmer _poolPrewarmer;
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
        private readonly List<Vector2> _turnArcs = new List<Vector2>();
        private readonly WeaponFacingSolver _facingSolver = new WeaponFacingSolver();
        private AttackData _mainAttackData = null;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private Transform _viewTransform;
        private DebugRangeCircle _attackRangeCircle;
#endif

        private int _targetVersion;
        // Target changes only mark candidates stale; the next reader rebuilds once for all of them.
        private bool _candidatesDirty;
        // The first candidate group is the main target's own block, added ahead of the listed groups.
        private bool _hasMainBlock;
        // Index = hardpoint: when it may select a target again.
        private float[] _retryAt = Array.Empty<float>();
        // Destroyed targets fail every candidate check already, so their candidates are dropped in bulk.
        private int _destroyedGroups;
        // Salvos waiting for their mount to finish turning.
        private readonly List<PendingSalvo> _turningSalvos = new List<PendingSalvo>();

        private bool _isReleased;
        private bool _isInitialized;

        public event Action<WeaponProfile, Transform> ShotEmitted;

        public float AttackDistance => Model.MaxWeaponRange * _modifiers.VisionMultiplier;

        public TargetPreference PreferredTarget { get; private set; }

        [Inject]
        private void Construct(WeaponModel model, ISelectionModelObserver selection,
            CombatAttackCoordinator attackCoordinator, CombatModifiers modifiers, WeaponsData weaponsData,
            DamageMatrixData damageMatrix, DebugRangeCircleFactory rangeCircleFactory, IncomingMissileRegistry missiles,
            IVisionService visionService, IPlayerRoster playerRoster, PlayerId owner,
            ImpactEffectPresenter impactPresenter, BoltFlightBatch boltFlights, ShotPoolPrewarmer poolPrewarmer,
            [Key(EntityBindType.ViewTransform)] Transform viewTransform)
        {
            _impactPresenter = impactPresenter;
            _boltFlights = boltFlights;
            _poolPrewarmer = poolPrewarmer;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _viewTransform = viewTransform;
#endif
            SetModel(model);
            _visionService = visionService;
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
            _retryAt = new float[hardPoints.Count];
            _attackCoordinator.Register(this);

            // Every hardpoint fires to its own weapon's range; nebulae and other vision effects scale it.
            FactionType faction = _playerRoster.Get(_owner).Faction;
            List<WeaponProfile> profiles = new List<WeaponProfile>(hardPoints.Count);
            foreach (WeaponHardPoint hardPoint in hardPoints)
            {
                WeaponProfile profile = _weaponsData.GetProfile(hardPoint.WeaponType, faction);
                profiles.Add(profile);
                hardPoint.SetData(profile, profile.Range, _damageMatrix.MissSpread,
                    this, _attackCoordinator, _modifiers, _impactPresenter, _boltFlights, _missiles, _poolPrewarmer);
                hardPoint.ShotEmitted += OnShotEmitted;
            }

            Dictionary<ShipClass, float> engagementRanges = new Dictionary<ShipClass, float>();
            float maxRange = 0f;
            foreach (ShipClass targetClass in (ShipClass[])Enum.GetValues(typeof(ShipClass)))
                engagementRanges.Add(targetClass,
                    WeaponEngagementRange.Calculate(profiles, targetClass, _damageMatrix));
            foreach (WeaponProfile profile in profiles)
                maxRange = Mathf.Max(maxRange, profile.Range);
            Model.SetRanges(maxRange, engagementRanges);
            PreferredTarget = GetLoadoutPreference(profiles);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _attackRangeCircle = _rangeCircleFactory.Create("AttackRange", Color.red, _selection);
#endif
        }

        public void Respawn()
        {
            foreach (WeaponHardPoint hardPoint in hardPoints)
            {
                hardPoint.RestartAttackSequence();
            }

            _isReleased = false;
            Initialize();
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
                group.Destroyed -= OnGroupDestroyed;
            }
            _subscribedGroups.Clear();
            _liveGroupsAtRebuild.Clear();
            _candidatesDirty = false;
            ReleaseCandidates();
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
                    AddGroupCandidates(attackData);
                    break;
                }
                case AttackType.MainTarget:
                {
                    AttackData previousMain = _mainAttackData;
                    _mainAttackData = attackData;
                    Subscribe(attackData);

                    bool isListed = !HasSameSource(attackData);
                    if (isListed)
                    {
                        _attackDataList.Add(attackData);
                    }

                    UnsubscribeIfUnused(previousMain);
                    SwitchMainCandidates(attackData, isListed);
                    break;
                }
            }
        }

        private bool CanAcquire(AttackData group) =>
            !group.IsDestroyed && group.CanAcquireTarget && _visionService.IsVisible(_owner,
                group.TargetEntity.GetFacade<IEntityTransformFacade>().Transform.position);

        // Strikecraft only when anti-strikecraft weapons carry most of the firepower, e.g. a laser corvette.
        private static TargetPreference GetLoadoutPreference(List<WeaponProfile> profiles)
        {
            float strikecraftDps = 0f;
            float shipsDps = 0f;
            foreach (WeaponProfile profile in profiles)
            {
                if (profile.StrikecraftOnly || profile.PreferredTarget == TargetPreference.Strikecraft)
                    strikecraftDps += profile.DamagePerSecond;
                else if (profile.PreferredTarget == TargetPreference.Ships) shipsDps += profile.DamagePerSecond;
            }

            return strikecraftDps > shipsDps ? TargetPreference.Strikecraft : TargetPreference.Ships;
        }

        public float GetEngagementRange(ShipClass targetClass) =>
            Model.GetEngagementRange(targetClass) * _modifiers.VisionMultiplier;

        public bool HasEnoughRange(float distance, float contactDistance, ShipClass targetClass)
        {
            return distance <= Mathf.Max(GetEngagementRange(targetClass) * ENGAGE_RANGE_FACTOR,
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
            SwitchMainCandidates(null, false);
        }

        /// <summary>
        /// Gives the candidates the order a rebuild would: the main target's block, then the listed groups newest
        /// first. A main target change touches only the old and new main blocks; ships change targets on every
        /// order, and a full rebuild re-acquires every unit of every group in radar range.
        /// </summary>
        private void SwitchMainCandidates(AttackData newMain, bool isNewlyListed)
        {
            _targetVersion++;
            CancelWaits();
            if (_candidatesDirty) return;

            if (_hasMainBlock) RemoveFirstGroup();
            _hasMainBlock = false;
            if (newMain == null) return;
            // A newly listed main is the newest listed group, so a rebuild lists it first after the main block.
            if (isNewlyListed) PrependGroup(newMain);
            _hasMainBlock = PrependGroup(newMain);
        }

        private bool PrependGroup(AttackData group)
        {
            int first = _candidateCount;
            int groupCount = _candidateGroups.Count;
            AddCandidates(group);
            if (_candidateGroups.Count == groupCount) return false;

            for (int i = 0; i < first; i++) _candidateGroupIndices[i]++;
            for (int i = first; i < _candidateCount; i++) _candidateGroupIndices[i] = 0;
            _candidateGroups.RemoveAt(groupCount);
            _candidateGroups.Insert(0, group);
            MoveBlockToStart(_candidateSlots, 0, first, _candidateCount);
            MoveBlockToStart(_candidateGroupIndices, 0, first, _candidateCount);
            return true;
        }

        private void RemoveFirstGroup()
        {
            TargetSelectionCapture targets = _attackCoordinator.TargetUnits;
            int blockEnd = 0;
            while (blockEnd < _candidateCount && _candidateGroupIndices[blockEnd] == 0)
            {
                targets.Release(_candidateSlots[blockEnd]);
                blockEnd++;
            }

            int remaining = _candidateCount - blockEnd;
            Array.Copy(_candidateSlots, blockEnd, _candidateSlots, 0, remaining);
            Array.Copy(_candidateGroupIndices, blockEnd, _candidateGroupIndices, 0, remaining);
            for (int i = 0; i < remaining; i++) _candidateGroupIndices[i]--;
            _candidateCount = remaining;
            _candidateGroups.RemoveAt(0);
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
            _attackRangeCircle.Draw(_viewTransform.position, AttackDistance);
#endif
            if (hardPoints == null || hardPoints.Count == 0)
                return;

            FireTurnedSalvos();
            for (int i = 0; i < hardPoints.Count; i++)
            {
                WeaponHardPoint weapon = hardPoints[i];
                if (!weapon.IsDestroyed && !weapon.IsBusy && Time.time >= _retryAt[i])
                    _attackCoordinator.QueueTargetSelection(this, weapon);
            }

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
            for (int i = 0; i < _candidateGroups.Count; i++)
            {
                AttackData group = _candidateGroups[i];
                bool canAcquire = capture.CanAcquire(group, _owner, _visionService, out bool isWreck);
                // A new wreck makes its destroyed hardpoints valid aim points again.
                if (isWreck && _liveGroupsAtRebuild.Contains(group))
                {
                    AttackSequenceDiagnostics.RecordTargetInvalidation();
                    InvalidateCandidates();
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

            // Preferred targets first, the main target first among them; the rest only when none is in reach.
            TargetPreference preference = weapon.PreferredTarget;
            int passes = preference == TargetPreference.Any ? 1 : 2;
            for (int pass = 0; pass < passes; pass++)
            for (int i = 0; i < _candidateCount; i++)
            {
                AttackData group = _candidateGroups[_candidateGroupIndices[i]];
                if (preference.Prefers(group.TargetClass) != (pass == 0)) continue;
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

            // Ship positions in the selection batch may be a few batches old, so the pick is re-aimed live.
            if (_targetVersion != targetVersion || result.CandidateIndex >= 0 &&
                (!CanAcquire(group) || !weapon.CanEngage(group.TargetClass) || !IsTargetValid(group, unit) || unit.Generation != generation ||
                 !HasFiringSolution(weapon, unit.Position)))
            {
                AttackSequenceDiagnostics.RecordTargetSelectionFallback(_targetVersion != targetVersion
                    ? WeaponTargetSelectionJob.FALLBACK_TARGET_VERSION
                    : WeaponTargetSelectionJob.FALLBACK_INVALID_CANDIDATE);
                CommitTargetSelectionSerial(weapon);
                return;
            }

            if (result.CandidateIndex < 0)
            {
                DelayRetry(weapon);
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
                TargetGeneration = unit.Generation,
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
                    salvo.Unit.Generation != salvo.TargetGeneration ||
                    !IsTargetValid(salvo.Group, salvo.Unit) || !HasFiringSolution(weapon, salvo.Unit.Position)) continue;
                weapon.Attack(salvo.Group, salvo.Unit);
            }
        }

        private static bool HasFiringSolution(WeaponHardPoint weapon, Vector3 target)
        {
            Transform weaponTransform = weapon.transform;
            Quaternion parentRotation = weaponTransform.parent == null
                ? Quaternion.identity : weaponTransform.parent.rotation;
            return WeaponTargetSelector.TryCalculateAim(target, weaponTransform.position, parentRotation,
                weapon.MaxAttackDistance, weapon.MinYaw, weapon.MaxYaw, out _, out _);
        }

        internal void CommitTargetSelectionSerial(WeaponHardPoint weapon)
        {
            if (_isReleased || weapon.IsDestroyed || weapon.IsBusy || _modifiers.IsCloaked) return;
            if (!TryFireWeapon(weapon)) DelayRetry(weapon);
        }

        private void DelayRetry(WeaponHardPoint weapon)
        {
            _retryAt[hardPoints.IndexOf(weapon)] =
                Time.time + UnityEngine.Random.Range(MIN_RETRY_INTERVAL, MAX_RETRY_INTERVAL);
        }

        private void Subscribe(AttackData group)
        {
            if (!_subscribedGroups.Add(group)) return;
            group.UnitsChanged += InvalidateCandidates;
            group.Destroyed += OnGroupDestroyed;
        }

        private void UnsubscribeIfUnused(AttackData group)
        {
            if (group == null || ReferenceEquals(group, _mainAttackData) || _attackDataList.Contains(group)) return;
            if (!_subscribedGroups.Remove(group)) return;
            group.UnitsChanged -= InvalidateCandidates;
            group.Destroyed -= OnGroupDestroyed;
        }

        // The version changes immediately so selections captured before the change fall back at commit.
        private void InvalidateCandidates()
        {
            _targetVersion++;
            _candidatesDirty = true;
            CancelWaits();
        }

        private void CancelWaits()
        {
            Array.Clear(_retryAt, 0, _retryAt.Length);
        }

        // Pooled targets (squadrons) report IsDestroyed false again after a respawn, so their dead groups leave the
        // list now; a stale group would otherwise match the new life by source and block its detection.
        private void OnGroupDestroyed()
        {
            for (int i = _attackDataList.Count - 1; i >= 0; i--)
            {
                AttackData group = _attackDataList[i];
                if (!group.IsDestroyed) continue;
                _attackDataList.RemoveAt(i);
                UnsubscribeIfUnused(group);
                AttackSequenceDiagnostics.RecordTargetInvalidation();
            }

            _destroyedGroups++;
            if (_destroyedGroups * 2 >= _candidateGroups.Count) InvalidateCandidates();
        }

        /// <summary>
        /// A new contact joins the candidates without rebuilding the others. It ranks right after the main target,
        /// where a rebuild would place the newest group.
        /// </summary>
        private void AddGroupCandidates(AttackData group)
        {
            _targetVersion++;
            CancelWaits();
            if (_candidatesDirty) return;

            int insertGroup = _hasMainBlock ? 1 : 0;
            int insertAt = 0;
            while (insertAt < _candidateCount && _candidateGroupIndices[insertAt] < insertGroup) insertAt++;
            int first = _candidateCount;
            AddCandidates(group);
            if (_candidateCount == first) return;

            for (int i = 0; i < first; i++)
                if (_candidateGroupIndices[i] >= insertGroup) _candidateGroupIndices[i]++;
            for (int i = first; i < _candidateCount; i++) _candidateGroupIndices[i] = insertGroup;
            _candidateGroups.RemoveAt(_candidateGroups.Count - 1);
            _candidateGroups.Insert(insertGroup, group);
            MoveBlockToStart(_candidateSlots, insertAt, first, _candidateCount);
            MoveBlockToStart(_candidateGroupIndices, insertAt, first, _candidateCount);
        }

        // Moves values[middle..end) in front of values[start..middle).
        private static void MoveBlockToStart(int[] values, int start, int middle, int end)
        {
            Array.Reverse(values, start, middle - start);
            Array.Reverse(values, middle, end - middle);
            Array.Reverse(values, start, end - start);
        }

        private void EnsureCandidates()
        {
            if (!_candidatesDirty) return;
            _candidatesDirty = false;
            RebuildCandidates();
        }

        private void RebuildCandidates()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using (BattleProfilerMarkers.WeaponRebuildCandidates.Auto())
            {
            long rebuildStart = Stopwatch.GetTimestamp();
#endif
            _targetVersion++;
            _destroyedGroups = 0;
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
                {
                    AddCandidates(_mainAttackData);
                    _hasMainBlock = _candidateGroups.Count > 0;
                }
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
            }
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
            _hasMainBlock = false;
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
                profile.AccuracyMultiplier * profile.GetAccuracyBias(attackData.TargetClass) * _modifiers.AccuracyMultiplier, UnityEngine.Random.value);

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
            if (hardPointModel.Id != targetId || !IsTargetValid(attackData, hardPointModel))
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
            public int TargetGeneration;
            public float FireTime;
        }
    }
}
