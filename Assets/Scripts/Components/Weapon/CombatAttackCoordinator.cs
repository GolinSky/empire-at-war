using System;
using System.Collections.Generic;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Services.Timing;
using EmpireAtWar.ViewComponents.Health;
using Unity.Collections;
using UnityEngine;
using Unity.Jobs;
using Zenject;

namespace EmpireAtWar.Components.Weapon
{
    public sealed class CombatAttackCoordinator : ILateTickable, IDisposable
    {
        internal const int JOB_SELECTION_THRESHOLD = 8;
        internal const int JOB_PROGRESSION_THRESHOLD = 64;
        internal const int TARGET_JOB_BATCH_SIZE = 1;
        internal const int DUE_JOB_BATCH_SIZE = 32;

        private struct DueEvent
        {
            public bool IsImpact;
            public float Time;
            public long Sequence;
        }

        private struct SequenceRecord
        {
            public IWeaponPresenter Owner;
            public int OwnerGeneration;
            public WeaponHardPoint HardPoint;
            public int HardPointGeneration;
            public AttackData TargetGroup;
            public IHardPointModel Target;
            public int ShotsRemaining;
            public float NextTime;
            public int EarliestFrame;
            public long EventSequence;
        }

        private struct ImpactRecord
        {
            public IWeaponPresenter Owner;
            public int OwnerGeneration;
            public AttackData TargetGroup;
            public IHardPointModel Target;
            public int TargetId;
            public int TargetGeneration;
            public float Damage;
            public DamageType DamageType;
            public float DueTime;
            public int EarliestFrame;
            public long EventSequence;
        }

        private struct TargetSelectionRequest
        {
            public IWeaponPresenter Owner;
            public WeaponComponent Weapon;
            public int OwnerGeneration;
            public WeaponHardPoint HardPoint;
            public int TargetVersion;
            public Vector3 Origin;
            public Quaternion ParentRotation;
            public int CandidateStart;
            public int CandidateCount;
        }

        private struct TargetSelectionCandidateRecord
        {
            public WeaponComponent.TargetSelectionCandidate Candidate;
        }

        private readonly Dictionary<IWeaponPresenter, int> _owners = new Dictionary<IWeaponPresenter, int>();
        private readonly List<SequenceRecord> _sequences = new List<SequenceRecord>();
        private readonly List<ImpactRecord> _impacts = new List<ImpactRecord>();
        private readonly List<TargetSelectionRequest> _targetSelectionRequests = new List<TargetSelectionRequest>();
        private readonly List<TargetSelectionCandidateRecord> _targetSelectionCandidates = new List<TargetSelectionCandidateRecord>();
        private readonly List<DueEvent> _dueEvents = new List<DueEvent>();
        private static readonly Comparison<DueEvent> _compareDueEvents = CompareDueEvents;
        private NativeArray<WeaponTargetSelectionJob.Input> _targetSelectionInputs;
        private NativeArray<Unity.Mathematics.float3> _targetSelectionPositions;
        private NativeArray<WeaponTargetSelectionJob.Result> _targetSelectionResults;
        private NativeArray<AttackDueJob.Input> _dueInputs;
        private NativeArray<byte> _dueResults;
        private int _nextOwnerGeneration;
        private long _nextEventSequence;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private int _lastTargetCandidateCount;
        private int _lastDueFlaggedCount;
        private int _dueCommittedCount;
#endif

        public int PendingSequences => _sequences.Count;
        public int PendingImpacts => _impacts.Count;

        public void Register(IWeaponPresenter owner)
        {
            if (owner == null) throw new ArgumentNullException(nameof(owner));
            if (_owners.ContainsKey(owner)) throw new InvalidOperationException("Weapon is already registered.");
            _owners.Add(owner, ++_nextOwnerGeneration);
        }

        public void Unregister(IWeaponPresenter owner)
        {
            if (!_owners.Remove(owner)) return;

            for (int i = _sequences.Count - 1; i >= 0; i--)
            {
                if (ReferenceEquals(_sequences[i].Owner, owner)) RemoveSequenceAt(i);
            }

            for (int i = _impacts.Count - 1; i >= 0; i--)
            {
                if (!ReferenceEquals(_impacts[i].Owner, owner)) continue;
                RemoveImpactAt(i);
                AttackSequenceDiagnostics.RecordCancelledImpact();
            }
        }

        public void BeginSequence(IWeaponPresenter owner, WeaponHardPoint hardPoint,
            AttackData targetGroup, IHardPointModel target)
        {
            if (!_owners.TryGetValue(owner, out int ownerGeneration))
                throw new InvalidOperationException("Weapon must be registered before firing.");
            if (!hardPoint.TryStartScheduledSequence(out int hardPointGeneration)) return;

            int shots = hardPoint.ShotsPerSalvo;
            if (shots <= 0)
            {
                hardPoint.StopEmitting(hardPointGeneration);
                return;
            }

            hardPoint.EmitScheduledShot(targetGroup, target, hardPointGeneration);
            if (!hardPoint.IsEmitting(hardPointGeneration)) return;

            // Coroutine waits resumed on a later frame; never emit a catch-up burst after a long frame.
            _sequences.Add(new SequenceRecord
            {
                Owner = owner,
                OwnerGeneration = ownerGeneration,
                HardPoint = hardPoint,
                HardPointGeneration = hardPointGeneration,
                TargetGroup = targetGroup,
                Target = target,
                ShotsRemaining = shots - 1,
                NextTime = Time.time + hardPoint.DelayBetweenShots,
                EarliestFrame = Time.frameCount + 1,
                EventSequence = ++_nextEventSequence
            });
        }

        internal void QueueTargetSelection(WeaponComponent weapon, WeaponHardPoint hardPoint)
        {
            if (!_owners.TryGetValue(weapon, out int ownerGeneration))
                throw new InvalidOperationException("Weapon must be registered before selecting a target.");

            _targetSelectionRequests.Add(new TargetSelectionRequest
            {
                Owner = weapon,
                Weapon = weapon,
                OwnerGeneration = ownerGeneration,
                HardPoint = hardPoint
            });
        }

        public void CancelSequence(WeaponHardPoint hardPoint, int generation)
        {
            for (int i = _sequences.Count - 1; i >= 0; i--)
            {
                SequenceRecord record = _sequences[i];
                if (!ReferenceEquals(record.HardPoint, hardPoint) || record.HardPointGeneration != generation) continue;
                RemoveSequenceAt(i);
            }

            hardPoint.StopEmitting(generation);
        }

        public void ScheduleImpact(IWeaponPresenter owner, AttackData targetGroup,
            IHardPointModel target, float damage, DamageType damageType, float delay)
        {
            if (!_owners.TryGetValue(owner, out int ownerGeneration))
                throw new InvalidOperationException("Weapon must be registered before scheduling damage.");

            _impacts.Add(new ImpactRecord
            {
                Owner = owner,
                OwnerGeneration = ownerGeneration,
                TargetGroup = targetGroup,
                Target = target,
                TargetId = target.Id,
                TargetGeneration = target.Generation,
                Damage = damage,
                DamageType = damageType,
                DueTime = Time.time + delay,
                EarliestFrame = Time.frameCount + 1,
                EventSequence = ++_nextEventSequence
            });
            AttackSequenceDiagnostics.RecordScheduledImpact();
        }

        public void LateTick()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            int targetRequests = _targetSelectionRequests.Count;
            int targetInputCapacity = _targetSelectionInputs.IsCreated ? _targetSelectionInputs.Length : 0;
            int targetPositionCapacity = _targetSelectionPositions.IsCreated ? _targetSelectionPositions.Length : 0;
            int targetResultCapacity = _targetSelectionResults.IsCreated ? _targetSelectionResults.Length : 0;
            int dueInputCapacityBefore = _dueInputs.IsCreated ? _dueInputs.Length : 0;
            int dueResultCapacityBefore = _dueResults.IsCreated ? _dueResults.Length : 0;
            long fallbackCount = AttackSequenceDiagnostics.TargetSelectionFallbacks;
            _lastTargetCandidateCount = 0;
            _lastDueFlaggedCount = 0;
            _dueCommittedCount = 0;
#endif
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using (BattleProfilerMarkers.TargetBatch.Auto())
#endif
            ProcessTargetSelections();

            float now = Time.time;
            int frame = Time.frameCount;
            int dueRecords = _sequences.Count + _impacts.Count;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using (BattleProfilerMarkers.DueBatch.Auto())
#endif
            {
                if (dueRecords >= JOB_PROGRESSION_THRESHOLD)
                {
                    ProcessDueEventsBatched(now, frame);
                }
                else
                {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    using (BattleProfilerMarkers.DueBatchSerial.Auto())
#endif
                    {
                        ProcessDueEventsSerial(now, frame);
                    }
                }
            }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (BattlePerformanceCapture.IsCapturing)
            {
                int currentTargetInputCapacity = _targetSelectionInputs.IsCreated ? _targetSelectionInputs.Length : 0;
                int currentTargetPositionCapacity = _targetSelectionPositions.IsCreated ? _targetSelectionPositions.Length : 0;
                int currentTargetResultCapacity = _targetSelectionResults.IsCreated ? _targetSelectionResults.Length : 0;
                int dueInputCapacity = _dueInputs.IsCreated ? _dueInputs.Length : 0;
                int dueResultCapacity = _dueResults.IsCreated ? _dueResults.Length : 0;
                BattlePerformanceCapture.RecordCombatWorkload(new BattlePerformanceCapture.CombatWorkload
                {
                    CompletedUnityFrame = frame,
                    TargetRequests = targetRequests,
                    TargetCandidates = _lastTargetCandidateCount,
                    TargetSerialCalls = targetRequests > 0 && targetRequests < JOB_SELECTION_THRESHOLD ? 1 : 0,
                    TargetJobCalls = targetRequests >= JOB_SELECTION_THRESHOLD ? 1 : 0,
                    TargetFallbacks = (int)(AttackSequenceDiagnostics.TargetSelectionFallbacks - fallbackCount),
                    TargetInputCapacity = currentTargetInputCapacity,
                    TargetPositionCapacity = currentTargetPositionCapacity,
                    TargetResultCapacity = currentTargetResultCapacity,
                    TargetBufferGrowths = (currentTargetInputCapacity > targetInputCapacity ? 1 : 0) +
                                          (currentTargetPositionCapacity > targetPositionCapacity ? 1 : 0) +
                                          (currentTargetResultCapacity > targetResultCapacity ? 1 : 0),
                    DueRecords = dueRecords,
                    DueFlagged = dueRecords >= JOB_PROGRESSION_THRESHOLD ? _lastDueFlaggedCount : _dueCommittedCount,
                    DueCommitted = _dueCommittedCount,
                    DueSerialCalls = dueRecords > 0 && dueRecords < JOB_PROGRESSION_THRESHOLD ? 1 : 0,
                    DueJobCalls = dueRecords >= JOB_PROGRESSION_THRESHOLD ? 1 : 0,
                    DueInputCapacity = dueInputCapacity,
                    DueResultCapacity = dueResultCapacity,
                    DueBufferGrowths = (dueInputCapacity > dueInputCapacityBefore ? 1 : 0) +
                                       (dueResultCapacity > dueResultCapacityBefore ? 1 : 0),
                    PendingSequences = _sequences.Count,
                    PendingImpacts = _impacts.Count
                });
            }
#endif
        }

        public void Dispose()
        {
            for (int i = 0; i < _impacts.Count; i++) AttackSequenceDiagnostics.RecordCancelledImpact();
            _impacts.Clear();
            _sequences.Clear();
            _owners.Clear();
            _targetSelectionRequests.Clear();
            _targetSelectionCandidates.Clear();
            _dueEvents.Clear();
            if (_targetSelectionInputs.IsCreated) _targetSelectionInputs.Dispose();
            if (_targetSelectionPositions.IsCreated) _targetSelectionPositions.Dispose();
            if (_targetSelectionResults.IsCreated) _targetSelectionResults.Dispose();
            if (_dueInputs.IsCreated) _dueInputs.Dispose();
            if (_dueResults.IsCreated) _dueResults.Dispose();
        }

        private bool IsRegistered(IWeaponPresenter owner, int generation) =>
            _owners.TryGetValue(owner, out int currentGeneration) && currentGeneration == generation;

        private void ProcessTargetSelections()
        {
            if (_targetSelectionRequests.Count == 0) return;

            CaptureTargetSelections();

            if (_targetSelectionRequests.Count < JOB_SELECTION_THRESHOLD)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                using (BattleProfilerMarkers.TargetBatchSerial.Auto())
#endif
                {
                    for (int i = 0; i < _targetSelectionRequests.Count; i++)
                    {
                        TargetSelectionRequest request = _targetSelectionRequests[i];
                        if (IsRegistered(request.Owner, request.OwnerGeneration))
                            ApplyTargetSelection(request, EvaluateTargetSelectionSerial(request));
                    }
                }

                _targetSelectionRequests.Clear();
                _targetSelectionCandidates.Clear();
                return;
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            long selectionStart = System.Diagnostics.Stopwatch.GetTimestamp();
            using (BattleProfilerMarkers.TargetBatchPrepare.Auto())
#endif
            {
                EnsureTargetSelectionCapacity(_targetSelectionRequests.Count, _targetSelectionCandidates.Count);
                for (int i = 0; i < _targetSelectionRequests.Count; i++)
                {
                    TargetSelectionRequest request = _targetSelectionRequests[i];
                    _targetSelectionInputs[i] = new WeaponTargetSelectionJob.Input
                    {
                        Origin = new Unity.Mathematics.float3(request.Origin.x, request.Origin.y, request.Origin.z),
                        ParentRotation = new Unity.Mathematics.quaternion(request.ParentRotation.x, request.ParentRotation.y,
                            request.ParentRotation.z, request.ParentRotation.w),
                        MaxDistance = request.HardPoint.MaxAttackDistance,
                        MinYaw = request.HardPoint.MinYaw,
                        MaxYaw = request.HardPoint.MaxYaw,
                        CandidateStart = request.CandidateStart,
                        CandidateCount = request.CandidateCount
                    };
                }

                for (int i = 0; i < _targetSelectionCandidates.Count; i++)
                {
                    Vector3 position = _targetSelectionCandidates[i].Candidate.Position;
                    _targetSelectionPositions[i] = new Unity.Mathematics.float3(position.x, position.y, position.z);
                }
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using (BattleProfilerMarkers.TargetBatchJob.Auto())
#endif
            {
                WeaponTargetSelectionJob job = new WeaponTargetSelectionJob
                {
                    Inputs = _targetSelectionInputs,
                    CandidatePositions = _targetSelectionPositions,
                    Results = _targetSelectionResults
                };
                JobHandle handle;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                using (BattleProfilerMarkers.TargetBatchSchedule.Auto())
#endif
                    handle = job.Schedule(_targetSelectionRequests.Count, TARGET_JOB_BATCH_SIZE);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                using (BattleProfilerMarkers.TargetBatchComplete.Auto())
#endif
                    handle.Complete();
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using (BattleProfilerMarkers.TargetBatchApply.Auto())
#endif
            {
                for (int i = 0; i < _targetSelectionRequests.Count; i++)
                {
                    TargetSelectionRequest request = _targetSelectionRequests[i];
                    if (!IsRegistered(request.Owner, request.OwnerGeneration)) continue;
                    ApplyTargetSelection(request, _targetSelectionResults[i]);
                }
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            AttackSequenceDiagnostics.RecordTargetSelectionBatchTime(selectionStart, _targetSelectionRequests.Count);
#endif
            _targetSelectionRequests.Clear();
            _targetSelectionCandidates.Clear();
        }

        private void CaptureTargetSelections()
        {
            // Capture every request at one LateTick boundary before either selection path commits.
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using (BattleProfilerMarkers.TargetBatchCapture.Auto())
#endif
            {
                for (int i = 0; i < _targetSelectionRequests.Count; i++)
                {
                    TargetSelectionRequest request = _targetSelectionRequests[i];
                    if (!IsRegistered(request.Owner, request.OwnerGeneration)) continue;

                    IReadOnlyList<WeaponComponent.TargetSelectionCandidate> candidates =
                        request.Weapon.CaptureTargetSelection(request.HardPoint, out request.TargetVersion,
                            out request.Origin, out request.ParentRotation);
                    request.CandidateStart = _targetSelectionCandidates.Count;
                    request.CandidateCount = candidates.Count;
                    for (int candidateIndex = 0; candidateIndex < candidates.Count; candidateIndex++)
                        _targetSelectionCandidates.Add(new TargetSelectionCandidateRecord { Candidate = candidates[candidateIndex] });
                    _targetSelectionRequests[i] = request;
                }
            }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _lastTargetCandidateCount = _targetSelectionCandidates.Count;
#endif
        }

        private WeaponTargetSelectionJob.Result EvaluateTargetSelectionSerial(TargetSelectionRequest request)
        {
            WeaponTargetSelectionJob.Result result = new WeaponTargetSelectionJob.Result { CandidateIndex = -1 };
            for (int i = request.CandidateStart; i < request.CandidateStart + request.CandidateCount; i++)
            {
                result.Visited++;
                bool canAttack = WeaponTargetSelector.TryCalculateAim(
                    _targetSelectionCandidates[i].Candidate.Position, request.Origin, request.ParentRotation,
                    request.HardPoint.MaxAttackDistance, request.HardPoint.MinYaw, request.HardPoint.MaxYaw,
                    out Quaternion aim, out bool inRange);
                if (inRange)
                {
                    result.HasInRangeAim = 1;
                    result.LastInRangeAim = new Unity.Mathematics.float4(aim.x, aim.y, aim.z, aim.w);
                }

                if (!canAttack) continue;
                result.CandidateIndex = i;
                result.SelectedAim = new Unity.Mathematics.float4(aim.x, aim.y, aim.z, aim.w);
                break;
            }

            return result;
        }

        private void ApplyTargetSelection(TargetSelectionRequest request, WeaponTargetSelectionJob.Result result)
        {
            AttackSequenceDiagnostics.RecordCandidateVisits(result.Visited);
            if (result.RequiresSerialFallback != 0)
            {
                AttackSequenceDiagnostics.RecordTargetSelectionFallback();
                result = EvaluateTargetSelectionSerial(request);
                AttackSequenceDiagnostics.RecordCandidateVisits(result.Visited);
            }

            if (result.CandidateIndex >= 0 &&
                (result.CandidateIndex < request.CandidateStart ||
                 result.CandidateIndex >= request.CandidateStart + request.CandidateCount))
            {
                AttackSequenceDiagnostics.RecordTargetSelectionFallback();
                request.Weapon.CommitTargetSelectionSerial(request.HardPoint);
                return;
            }

            request.Weapon.CommitTargetSelection(request.HardPoint, request.TargetVersion, result,
                result.CandidateIndex < 0 ? default : _targetSelectionCandidates[result.CandidateIndex].Candidate);
        }

        private void EnsureTargetSelectionCapacity(int requestCount, int candidateCount)
        {
            EnsureTargetSelectionBuffer(ref _targetSelectionInputs, requestCount);
            EnsureTargetSelectionBuffer(ref _targetSelectionResults, requestCount);
            EnsureTargetSelectionBuffer(ref _targetSelectionPositions, candidateCount);
        }

        private static void EnsureTargetSelectionBuffer<T>(ref NativeArray<T> buffer, int requiredCapacity) where T : struct
        {
            if (buffer.IsCreated && buffer.Length >= requiredCapacity) return;
            if (buffer.IsCreated) buffer.Dispose();
            int capacity = 8;
            while (capacity < requiredCapacity) capacity *= 2;
            buffer = new NativeArray<T>(capacity, Allocator.Persistent);
        }

        internal void ProcessDueEventsSerial(float now, int frame)
        {
            while (TryFindNextDue(now, frame, out bool isImpact, out int index))
                CommitDueEvent(isImpact, index, now, frame);
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        internal void ProcessDueEventsSerialCollected(float now, int frame)
        {
            _dueEvents.Clear();
            for (int i = 0; i < _sequences.Count; i++)
            {
                SequenceRecord record = _sequences[i];
                if (record.EarliestFrame <= frame && record.NextTime <= now)
                    _dueEvents.Add(new DueEvent { Time = record.NextTime, Sequence = record.EventSequence });
            }

            for (int i = 0; i < _impacts.Count; i++)
            {
                ImpactRecord record = _impacts[i];
                if (record.EarliestFrame <= frame && record.DueTime <= now)
                    _dueEvents.Add(new DueEvent
                        { IsImpact = true, Time = record.DueTime, Sequence = record.EventSequence });
            }

            CommitCollectedDueEvents(now, frame);
        }
#endif

        internal void ProcessDueEventsBatched(float now, int frame)
        {
            int sequenceCount = _sequences.Count;
            int recordCount = sequenceCount + _impacts.Count;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using (BattleProfilerMarkers.DueBatchPrepare.Auto())
#endif
            {
                EnsureTargetSelectionBuffer(ref _dueInputs, recordCount);
                EnsureTargetSelectionBuffer(ref _dueResults, recordCount);

                for (int i = 0; i < sequenceCount; i++)
                {
                    SequenceRecord record = _sequences[i];
                    _dueInputs[i] = new AttackDueJob.Input { DueTime = record.NextTime, EarliestFrame = record.EarliestFrame };
                }

                for (int i = 0; i < _impacts.Count; i++)
                {
                    ImpactRecord record = _impacts[i];
                    _dueInputs[sequenceCount + i] = new AttackDueJob.Input
                        { DueTime = record.DueTime, EarliestFrame = record.EarliestFrame };
                }
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using (BattleProfilerMarkers.DueBatchJob.Auto())
#endif
            {
                AttackDueJob job = new AttackDueJob
                {
                    Inputs = _dueInputs,
                    Results = _dueResults,
                    Now = now,
                    Frame = frame
                };
                JobHandle handle;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                using (BattleProfilerMarkers.DueBatchSchedule.Auto())
#endif
                    handle = job.Schedule(recordCount, DUE_JOB_BATCH_SIZE);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                using (BattleProfilerMarkers.DueBatchComplete.Auto())
#endif
                    handle.Complete();
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using (BattleProfilerMarkers.DueBatchApply.Auto())
#endif
            {
                _dueEvents.Clear();
                for (int i = 0; i < recordCount; i++)
                {
                    if (_dueResults[i] == 0) continue;
                    if (i < sequenceCount)
                    {
                        SequenceRecord record = _sequences[i];
                        _dueEvents.Add(new DueEvent { Time = record.NextTime, Sequence = record.EventSequence });
                    }
                    else
                    {
                        ImpactRecord record = _impacts[i - sequenceCount];
                        _dueEvents.Add(new DueEvent
                            { IsImpact = true, Time = record.DueTime, Sequence = record.EventSequence });
                    }
                }

                CommitCollectedDueEvents(now, frame);
            }
        }

        private void CommitCollectedDueEvents(float now, int frame)
        {
            _dueEvents.Sort(_compareDueEvents);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _lastDueFlaggedCount = _dueEvents.Count;
#endif
            for (int i = 0; i < _dueEvents.Count; i++)
            {
                DueEvent due = _dueEvents[i];
                if (TryFindEvent(due.IsImpact, due.Sequence, out int index))
                    CommitDueEvent(due.IsImpact, index, now, frame);
            }
        }

        private static int CompareDueEvents(DueEvent left, DueEvent right)
        {
            int timeComparison = left.Time.CompareTo(right.Time);
            return timeComparison != 0 ? timeComparison : left.Sequence.CompareTo(right.Sequence);
        }

        private bool TryFindEvent(bool isImpact, long sequence, out int index)
        {
            if (isImpact)
            {
                for (int i = 0; i < _impacts.Count; i++)
                    if (_impacts[i].EventSequence == sequence)
                    {
                        index = i;
                        return true;
                    }
            }
            else
            {
                for (int i = 0; i < _sequences.Count; i++)
                    if (_sequences[i].EventSequence == sequence)
                    {
                        index = i;
                        return true;
                    }
            }

            index = -1;
            return false;
        }

        private void CommitDueEvent(bool isImpact, int index, float now, int frame)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _dueCommittedCount++;
#endif
            if (isImpact)
            {
                ImpactRecord impact = _impacts[index];
                RemoveImpactAt(index);
                if (IsRegistered(impact.Owner, impact.OwnerGeneration) &&
                    impact.Target.Generation == impact.TargetGeneration &&
                    impact.Owner.CommitImpact(impact.TargetGroup, impact.Target, impact.Damage, impact.DamageType,
                        impact.TargetId))
                    AttackSequenceDiagnostics.RecordAppliedImpact();
                else
                    AttackSequenceDiagnostics.RecordCancelledImpact();
                return;
            }

            SequenceRecord sequence = _sequences[index];
            RemoveSequenceAt(index);
            if (!IsRegistered(sequence.Owner, sequence.OwnerGeneration) ||
                !sequence.HardPoint.IsEmitting(sequence.HardPointGeneration)) return;

            if (sequence.ShotsRemaining == 0)
            {
                sequence.HardPoint.StopEmitting(sequence.HardPointGeneration);
                return;
            }

            sequence.HardPoint.EmitScheduledShot(sequence.TargetGroup, sequence.Target, sequence.HardPointGeneration);
            if (!sequence.HardPoint.IsEmitting(sequence.HardPointGeneration)) return;
            sequence.ShotsRemaining--;
            sequence.NextTime = now + sequence.HardPoint.DelayBetweenShots;
            sequence.EarliestFrame = frame + 1;
            sequence.EventSequence = ++_nextEventSequence;
            _sequences.Add(sequence);
        }

        private bool TryFindNextDue(float now, int frame, out bool isImpact, out int index)
        {
            isImpact = false;
            index = -1;
            float firstTime = float.MaxValue;
            long firstSequence = long.MaxValue;

            for (int i = 0; i < _sequences.Count; i++)
            {
                SequenceRecord record = _sequences[i];
                if (record.EarliestFrame > frame || record.NextTime > now ||
                    !IsEarlier(record.NextTime, record.EventSequence, firstTime, firstSequence)) continue;
                index = i;
                firstTime = record.NextTime;
                firstSequence = record.EventSequence;
            }

            for (int i = 0; i < _impacts.Count; i++)
            {
                ImpactRecord record = _impacts[i];
                if (record.EarliestFrame > frame || record.DueTime > now ||
                    !IsEarlier(record.DueTime, record.EventSequence, firstTime, firstSequence)) continue;
                isImpact = true;
                index = i;
                firstTime = record.DueTime;
                firstSequence = record.EventSequence;
            }

            return index >= 0;
        }

        private static bool IsEarlier(float time, long sequence, float firstTime, long firstSequence) =>
            time < firstTime || time == firstTime && sequence < firstSequence;

        private void RemoveSequenceAt(int index)
        {
            int last = _sequences.Count - 1;
            _sequences[index] = _sequences[last];
            _sequences.RemoveAt(last);
        }

        private void RemoveImpactAt(int index)
        {
            int last = _impacts.Count - 1;
            _impacts[index] = _impacts[last];
            _impacts.RemoveAt(last);
        }
    }
}
