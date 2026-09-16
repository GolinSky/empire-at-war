using System;
using System.Collections.Generic;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Models.Health;
using EmpireAtWar.ViewComponents.Health;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Components.Weapon
{
    public sealed class CombatAttackCoordinator : ILateTickable, IDisposable
    {
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

        private readonly Dictionary<IWeaponPresenter, int> _owners = new Dictionary<IWeaponPresenter, int>();
        private readonly List<SequenceRecord> _sequences = new List<SequenceRecord>();
        private readonly List<ImpactRecord> _impacts = new List<ImpactRecord>();
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
            float now = Time.time;
            int frame = Time.frameCount;
            while (TryFindNextDue(now, frame, out bool isImpact, out int index))
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
                    continue;
                }

                SequenceRecord sequence = _sequences[index];
                RemoveSequenceAt(index);
                if (!IsRegistered(sequence.Owner, sequence.OwnerGeneration) ||
                    !sequence.HardPoint.IsEmitting(sequence.HardPointGeneration)) continue;

                if (sequence.ShotsRemaining == 0)
                {
                    sequence.HardPoint.StopEmitting(sequence.HardPointGeneration);
                    continue;
                }

                sequence.HardPoint.EmitScheduledShot(sequence.TargetGroup, sequence.Target, sequence.HardPointGeneration);
                if (!sequence.HardPoint.IsEmitting(sequence.HardPointGeneration)) continue;
                sequence.ShotsRemaining--;
                sequence.NextTime = now + sequence.HardPoint.DelayBetweenShots;
                sequence.EarliestFrame = frame + 1;
                sequence.EventSequence = ++_nextEventSequence;
                _sequences.Add(sequence);
            }
        }

        public void Dispose()
        {
            for (int i = 0; i < _impacts.Count; i++) AttackSequenceDiagnostics.RecordCancelledImpact();
            _impacts.Clear();
            _sequences.Clear();
            _owners.Clear();
        }

        private bool IsRegistered(IWeaponPresenter owner, int generation) =>
            _owners.TryGetValue(owner, out int currentGeneration) && currentGeneration == generation;

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
