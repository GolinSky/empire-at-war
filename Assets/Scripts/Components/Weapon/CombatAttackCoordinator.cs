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
        private const int JOB_SELECTION_THRESHOLD = 8;
        private const int JOB_PROGRESSION_THRESHOLD = 64;

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
            public WeaponHardPointView HardPoint;
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
            public WeaponType WeaponType;
            public float DueTime;
            public int EarliestFrame;
            public long EventSequence;
        }

        private struct TargetSelectionRequest
        {
            public IWeaponPresenter Owner;
            public WeaponComponent Weapon;
            public int OwnerGeneration;
            public WeaponHardPointView HardPoint;
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

        public void BeginSequence(IWeaponPresenter owner, WeaponHardPointView hardPoint,
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

        internal void QueueTargetSelection(WeaponComponent weapon, WeaponHardPointView hardPoint, int targetVersion,
            IReadOnlyList<WeaponComponent.TargetSelectionCandidate> candidates, Vector3 origin, Quaternion parentRotation)
        {
            if (!_owners.TryGetValue(weapon, out int ownerGeneration))
                throw new InvalidOperationException("Weapon must be registered before selecting a target.");

            int candidateStart = _targetSelectionCandidates.Count;
            for (int i = 0; i < candidates.Count; i++)
                _targetSelectionCandidates.Add(new TargetSelectionCandidateRecord { Candidate = candidates[i] });

            _targetSelectionRequests.Add(new TargetSelectionRequest
            {
                Owner = weapon,
                Weapon = weapon,
                OwnerGeneration = ownerGeneration,
                HardPoint = hardPoint,
                TargetVersion = targetVersion,
                Origin = origin,
                ParentRotation = parentRotation,
                CandidateStart = candidateStart,
                CandidateCount = candidates.Count
            });
        }

        public void CancelSequence(WeaponHardPointView hardPoint, int generation)
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
            IHardPointModel target, WeaponType weaponType, float delay)
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
                WeaponType = weaponType,
                DueTime = Time.time + delay,
                EarliestFrame = Time.frameCount + 1,
                EventSequence = ++_nextEventSequence
            });
            AttackSequenceDiagnostics.RecordScheduledImpact();
        }

        public void LateTick()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using (BattleProfilerMarkers.TargetBatch.Auto())
#endif
            ProcessTargetSelections();

            float now = Time.time;
            int frame = Time.frameCount;
            if (_sequences.Count + _impacts.Count >= JOB_PROGRESSION_THRESHOLD)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                using (BattleProfilerMarkers.DueBatch.Auto())
#endif
                ProcessDueEventsBatched(now, frame);
                return;
            }

            while (TryFindNextDue(now, frame, out bool isImpact, out int index))
                CommitDueEvent(isImpact, index, now, frame);
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

            if (_targetSelectionRequests.Count < JOB_SELECTION_THRESHOLD)
            {
                for (int i = 0; i < _targetSelectionRequests.Count; i++)
                {
                    TargetSelectionRequest request = _targetSelectionRequests[i];
                    if (IsRegistered(request.Owner, request.OwnerGeneration))
                        request.Weapon.CommitTargetSelectionSerial(request.HardPoint);
                }

                _targetSelectionRequests.Clear();
                _targetSelectionCandidates.Clear();
                return;
            }

            EnsureTargetSelectionCapacity(_targetSelectionRequests.Count, _targetSelectionCandidates.Count);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            long selectionStart = System.Diagnostics.Stopwatch.GetTimestamp();
#endif
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

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using (BattleProfilerMarkers.TargetBatchJob.Auto())
#endif
            {
                new WeaponTargetSelectionJob
                {
                    Inputs = _targetSelectionInputs,
                    CandidatePositions = _targetSelectionPositions,
                    Results = _targetSelectionResults
                }.Schedule(_targetSelectionRequests.Count, 1).Complete();
            }

            for (int i = 0; i < _targetSelectionRequests.Count; i++)
            {
                TargetSelectionRequest request = _targetSelectionRequests[i];
                if (!IsRegistered(request.Owner, request.OwnerGeneration)) continue;

                WeaponTargetSelectionJob.Result result = _targetSelectionResults[i];
                AttackSequenceDiagnostics.RecordCandidateVisits(result.Visited);
                if (result.CandidateIndex < request.CandidateStart ||
                    result.CandidateIndex >= request.CandidateStart + request.CandidateCount)
                {
                    if (result.CandidateIndex >= 0)
                    {
                        AttackSequenceDiagnostics.RecordTargetSelectionFallback();
                        request.Weapon.CommitTargetSelectionSerial(request.HardPoint);
                    }
                    else request.Weapon.CommitTargetSelection(request.HardPoint, request.TargetVersion, result, default);
                    continue;
                }

                request.Weapon.CommitTargetSelection(request.HardPoint, request.TargetVersion, result,
                    _targetSelectionCandidates[result.CandidateIndex].Candidate);
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            AttackSequenceDiagnostics.RecordTargetSelectionBatchTime(selectionStart, _targetSelectionRequests.Count);
#endif
            _targetSelectionRequests.Clear();
            _targetSelectionCandidates.Clear();
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

        private void ProcessDueEventsBatched(float now, int frame)
        {
            int sequenceCount = _sequences.Count;
            int recordCount = sequenceCount + _impacts.Count;
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

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using (BattleProfilerMarkers.DueBatchJob.Auto())
#endif
            {
                new AttackDueJob
                {
                    Inputs = _dueInputs,
                    Results = _dueResults,
                    Now = now,
                    Frame = frame
                }.Schedule(recordCount, 32).Complete();
            }

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

            _dueEvents.Sort(_compareDueEvents);
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
            if (isImpact)
            {
                ImpactRecord impact = _impacts[index];
                RemoveImpactAt(index);
                if (IsRegistered(impact.Owner, impact.OwnerGeneration) &&
                    impact.Target.Generation == impact.TargetGeneration &&
                    impact.Owner.CommitImpact(impact.TargetGroup, impact.Target, impact.WeaponType, impact.TargetId))
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
