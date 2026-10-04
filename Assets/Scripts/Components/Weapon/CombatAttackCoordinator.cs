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
        internal const int JOB_SELECTION_THRESHOLD = TargetSelectionBatch.JOB_SELECTION_THRESHOLD;
        internal const int JOB_PROGRESSION_THRESHOLD = 64;
        internal const int TARGET_JOB_BATCH_SIZE = TargetSelectionBatch.TARGET_JOB_BATCH_SIZE;
        internal const int DUE_JOB_BATCH_SIZE = 32;

        private readonly Dictionary<IWeaponPresenter, int> _owners = new Dictionary<IWeaponPresenter, int>();
        private readonly List<SequenceRecord> _sequences = new List<SequenceRecord>();
        private readonly List<ImpactRecord> _impacts = new List<ImpactRecord>();
        private readonly List<DueEvent> _dueEvents = new List<DueEvent>();
        private static readonly Comparison<DueEvent> _compareDueEvents = CompareDueEvents;
        private readonly TargetSelectionBatch _targetSelection;

        private NativeArray<AttackDueJob.Input> _dueInputs;
        private NativeArray<byte> _dueResults;

        private long _nextEventSequence;

        private int _nextOwnerGeneration;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private int _lastDueFlaggedCount;
        private int _dueCommittedCount;
#endif

        public int PendingSequences => _sequences.Count;
        public int PendingImpacts => _impacts.Count;

        public CombatAttackCoordinator()
        {
            _targetSelection = new TargetSelectionBatch(IsRegistered);
        }

        public void Dispose()
        {
            for (int i = 0; i < _impacts.Count; i++) AttackSequenceDiagnostics.RecordCancelledImpact();
            _impacts.Clear();
            _sequences.Clear();
            _owners.Clear();
            _targetSelection.Dispose();
            _dueEvents.Clear();
            if (_dueInputs.IsCreated) _dueInputs.Dispose();
            if (_dueResults.IsCreated) _dueResults.Dispose();
        }

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
            if (!targetGroup.CanTarget(target) || !targetGroup.Contains(target)) return;
            if (!hardPoint.TryStartScheduledSequence(out int hardPointGeneration)) return;

            int shots = hardPoint.ShotsPerSalvo;
            if (shots <= 0)
            {
                hardPoint.StopEmitting(hardPointGeneration);
                return;
            }

            int targetGeneration = target.Generation;
            hardPoint.EmitScheduledShot(targetGroup, target, hardPointGeneration);
            if (!hardPoint.IsEmitting(hardPointGeneration))
            {
                // An ion disable interrupts the salvo; the sequence must end or the hardpoint stays busy forever.
                hardPoint.StopEmitting(hardPointGeneration);
                return;
            }

            // Coroutine waits resumed on a later frame; never emit a catch-up burst after a long frame.
            _sequences.Add(new SequenceRecord
            {
                Owner = owner,
                OwnerGeneration = ownerGeneration,
                HardPoint = hardPoint,
                HardPointGeneration = hardPointGeneration,
                TargetGroup = targetGroup,
                Target = target,
                TargetGeneration = targetGeneration,
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

            _targetSelection.Queue(weapon, hardPoint, ownerGeneration);
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
            IHardPointModel target, float damage, DamageType damageType, float delay, IncomingMissile missile)
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
                Missile = missile,
                DueTime = Time.time + delay,
                EarliestFrame = Time.frameCount + 1,
                EventSequence = ++_nextEventSequence
            });
            AttackSequenceDiagnostics.RecordScheduledImpact();
        }

        public void LateTick()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            int targetRequests = _targetSelection.RequestCount;
            int targetInputCapacity = _targetSelection.InputCapacity;
            int targetPositionCapacity = _targetSelection.PositionCapacity;
            int targetResultCapacity = _targetSelection.ResultCapacity;
            int dueInputCapacityBefore = _dueInputs.IsCreated ? _dueInputs.Length : 0;
            int dueResultCapacityBefore = _dueResults.IsCreated ? _dueResults.Length : 0;
            long fallbackCount = AttackSequenceDiagnostics.TargetSelectionFallbacks;
            _lastDueFlaggedCount = 0;
            _dueCommittedCount = 0;
            using (BattleProfilerMarkers.TargetBatch.Auto())
#endif
            _targetSelection.Process();

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
                int currentTargetInputCapacity = _targetSelection.InputCapacity;
                int currentTargetPositionCapacity = _targetSelection.PositionCapacity;
                int currentTargetResultCapacity = _targetSelection.ResultCapacity;
                int dueInputCapacity = _dueInputs.IsCreated ? _dueInputs.Length : 0;
                int dueResultCapacity = _dueResults.IsCreated ? _dueResults.Length : 0;
                BattlePerformanceCapture.RecordCombatWorkload(new BattlePerformanceCapture.CombatWorkload
                {
                    CompletedUnityFrame = frame,
                    TargetRequests = targetRequests,
                    TargetCandidates = _targetSelection.LastCandidateCount,
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

        private bool IsRegistered(IWeaponPresenter owner, int generation) =>
            _owners.TryGetValue(owner, out int currentGeneration) && currentGeneration == generation;

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
                EmpireAtWar.Utils.NativeArrayBuffer.EnsureCapacity(ref _dueInputs, recordCount);
                EmpireAtWar.Utils.NativeArrayBuffer.EnsureCapacity(ref _dueResults, recordCount);

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
                    (impact.Missile == null || !impact.Missile.IsIntercepted) &&
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
                !sequence.HardPoint.IsEmitting(sequence.HardPointGeneration))
            {
                sequence.HardPoint.StopEmitting(sequence.HardPointGeneration);
                return;
            }

            if (sequence.ShotsRemaining == 0 ||
                sequence.Target.Generation != sequence.TargetGeneration ||
                !sequence.TargetGroup.CanTarget(sequence.Target) ||
                !sequence.TargetGroup.Contains(sequence.Target))
            {
                sequence.HardPoint.StopEmitting(sequence.HardPointGeneration);
                return;
            }

            sequence.HardPoint.EmitScheduledShot(sequence.TargetGroup, sequence.Target, sequence.HardPointGeneration);
            if (!sequence.HardPoint.IsEmitting(sequence.HardPointGeneration))
            {
                sequence.HardPoint.StopEmitting(sequence.HardPointGeneration);
                return;
            }
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

        private struct DueEvent
        {
            public float Time;

            public long Sequence;

            public bool IsImpact;
        }

        private struct SequenceRecord
        {
            public IWeaponPresenter Owner;
            public IHardPointModel Target;

            public WeaponHardPoint HardPoint;
            public AttackData TargetGroup;

            public float NextTime;

            public long EventSequence;

            public int OwnerGeneration;
            public int HardPointGeneration;
            public int TargetGeneration;
            public int ShotsRemaining;
            public int EarliestFrame;
        }

        private struct ImpactRecord
        {
            public IWeaponPresenter Owner;
            public IHardPointModel Target;

            public AttackData TargetGroup;
            public IncomingMissile Missile;

            public DamageType DamageType;

            public float Damage;
            public float DueTime;

            public long EventSequence;

            public int OwnerGeneration;
            public int TargetId;
            public int TargetGeneration;
            public int EarliestFrame;
        }
    }
}
