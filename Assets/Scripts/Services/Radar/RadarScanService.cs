using System;
using System.Collections.Generic;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Mvc;
using EmpireAtWar.Services.Timing;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;
using Zenject;

namespace EmpireAtWar.Services.Radar
{
    /// <summary>
    /// Runs every radar due this fixed step as one Burst batch over entity positions.
    /// Radars are staggered across fixed steps so they never all scan in the same step.
    /// Detected = hostile, not destroyed, has units, not cloaked, planar centre within range.
    /// </summary>
    public sealed class RadarScanService : Service, IRadarScanService, IInitializable, IFixedTickable, IDisposable
    {
        private const int INITIAL_CAPACITY = 256;
        private const int BITS_PER_WORD = 64;
        private const int TRANSFORM_BATCH_SIZE = 64;
        private const int DETECTION_BATCH_SIZE = 4;

        private readonly IEntityLocator _entityLocator;
        private readonly IPlayerRelations _relations;

        private readonly List<RadarRegistration> _radars = new List<RadarRegistration>();
        private readonly Dictionary<IRadarScanner, RadarRegistration> _registrations =
            new Dictionary<IRadarScanner, RadarRegistration>();
        private readonly List<RadarRegistration> _dueRadars = new List<RadarRegistration>();

        // Target indices are read while scan results are delivered, so entity changes wait for the next step.
        private readonly List<IEntity> _targets = new List<IEntity>();
        private readonly Dictionary<IEntity, int> _targetIndices = new Dictionary<IEntity, int>();
        private readonly List<IEntity> _pendingAdded = new List<IEntity>();
        private readonly List<IEntity> _pendingRemoved = new List<IEntity>();

        private readonly Dictionary<PlayerId, int> _ownerSlots = new Dictionary<PlayerId, int>();
        private readonly List<PlayerId> _slotOwners = new List<PlayerId>();
        private readonly List<IEntity> _detected = new List<IEntity>();

        private TransformAccessArray _targetTransforms;
        private NativeList<float3> _targetPositions;
        private NativeList<RadarTarget> _targetStates;
        private NativeList<RadarQuery> _queries;
        private NativeList<byte> _hostility;
        private NativeList<ulong> _detectedBits;

        private int _registrationCount;

        public RadarScanService(IEntityLocator entityLocator, IPlayerRelations relations)
        {
            _entityLocator = entityLocator;
            _relations = relations;
        }

        public void Initialize()
        {
            _targetTransforms = new TransformAccessArray(INITIAL_CAPACITY);
            _targetPositions = new NativeList<float3>(INITIAL_CAPACITY, Allocator.Persistent);
            _targetStates = new NativeList<RadarTarget>(INITIAL_CAPACITY, Allocator.Persistent);
            _queries = new NativeList<RadarQuery>(INITIAL_CAPACITY, Allocator.Persistent);
            _hostility = new NativeList<byte>(INITIAL_CAPACITY, Allocator.Persistent);
            _detectedBits = new NativeList<ulong>(INITIAL_CAPACITY, Allocator.Persistent);

            _pendingAdded.AddRange(_entityLocator.Entities);
            _entityLocator.EntityAdded += _pendingAdded.Add;
            _entityLocator.EntityRemoved += _pendingRemoved.Add;
        }

        public void Dispose()
        {
            _entityLocator.EntityAdded -= _pendingAdded.Add;
            _entityLocator.EntityRemoved -= _pendingRemoved.Add;
            _targetTransforms.Dispose();
            _targetPositions.Dispose();
            _targetStates.Dispose();
            _queries.Dispose();
            _hostility.Dispose();
            _detectedBits.Dispose();
        }

        public void Register(IRadarScanner scanner)
        {
            // Matches the old strict "time > start + delay" timer at the fixed step rate.
            int intervalSteps = Mathf.FloorToInt(scanner.Delay / Time.fixedDeltaTime) + 1;
            RadarRegistration registration = new RadarRegistration(
                scanner, intervalSteps, 1 + _registrationCount++ % intervalSteps);
            _registrations.Add(scanner, registration);
            _radars.Add(registration);
        }

        public void Unregister(IRadarScanner scanner)
        {
            RadarRegistration registration = _registrations[scanner];
            _registrations.Remove(scanner);
            registration.IsRegistered = false;
            int index = _radars.IndexOf(registration);
            int last = _radars.Count - 1;
            _radars[index] = _radars[last];
            _radars.RemoveAt(last);
        }

        public void FixedTick()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using (BattleProfilerMarkers.RadarScan.Auto())
            {
#endif
            ApplyPendingTargets();
            CollectDueRadars();
            if (_dueRadars.Count > 0)
            {
                ScanDueRadars();
            }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            }
#endif
        }

        private void ScanDueRadars()
        {
            int wordsPerQuery;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using (BattleProfilerMarkers.RadarSnapshot.Auto())
#endif
            {
                wordsPerQuery = PrepareScan();
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using (BattleProfilerMarkers.RadarJob.Auto())
#endif
            {
                RunJobs(wordsPerQuery);
            }

            DeliverResults(wordsPerQuery);
        }

        private int PrepareScan()
        {
            int targetCount = _targets.Count;
            _targetStates.ResizeUninitialized(targetCount);
            _targetPositions.ResizeUninitialized(targetCount);
            for (int i = 0; i < targetCount; i++)
            {
                IEntity target = _targets[i];
                _targetStates[i] = new RadarTarget(GetOwnerSlot(target.Owner), IsDetectable(target));
                _targetPositions[i] = new float3(float.NaN);
            }

            _queries.Clear();
            for (int i = 0; i < _dueRadars.Count; i++)
            {
                IRadarScanner scanner = _dueRadars[i].Scanner;
                Vector3 center = scanner.ScanCenter;
                float range = scanner.Range;
                _queries.Add(new RadarQuery(new float2(center.x, center.z), range * range,
                    GetOwnerSlot(scanner.Owner)));
            }

            BuildHostility(_slotOwners.Count);

            int wordsPerQuery = (targetCount + BITS_PER_WORD - 1) / BITS_PER_WORD;
            _detectedBits.ResizeUninitialized(_dueRadars.Count * wordsPerQuery);
            return wordsPerQuery;
        }

        private void RunJobs(int wordsPerQuery)
        {
            JobHandle positions = new RadarPositionJob
            {
                Positions = _targetPositions.AsArray()
            }.ScheduleReadOnly(_targetTransforms, TRANSFORM_BATCH_SIZE);
            new RadarDetectionJob
            {
                Queries = _queries.AsArray(),
                Targets = _targetStates.AsArray(),
                Positions = _targetPositions.AsArray(),
                Hostility = _hostility.AsArray(),
                DetectedBits = _detectedBits.AsArray(),
                SlotCount = _slotOwners.Count,
                WordsPerQuery = wordsPerQuery
            }.Schedule(_dueRadars.Count, DETECTION_BATCH_SIZE, positions).Complete();
        }

        private void DeliverResults(int wordsPerQuery)
        {
            for (int query = 0; query < _dueRadars.Count; query++)
            {
                RadarRegistration registration = _dueRadars[query];
                // A delivered scan can release another radar (unit death); skip it this step.
                if (!registration.IsRegistered)
                {
                    continue;
                }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                using (BattleProfilerMarkers.RadarApply.Auto())
#endif
                {
                    DeliverResult(registration.Scanner, query * wordsPerQuery, wordsPerQuery);
                }
            }
        }

        private void DeliverResult(IRadarScanner scanner, int offset, int wordsPerQuery)
        {
            _detected.Clear();
            for (int word = 0; word < wordsPerQuery; word++)
            {
                ulong bits = _detectedBits[offset + word];
                while (bits != 0)
                {
                    _detected.Add(_targets[word * BITS_PER_WORD + math.tzcnt(bits)]);
                    bits &= bits - 1;
                }
            }

            scanner.ApplyScan(_detected);
        }

        private void CollectDueRadars()
        {
            _dueRadars.Clear();
            for (int i = 0; i < _radars.Count; i++)
            {
                RadarRegistration registration = _radars[i];
                if (--registration.StepsUntilScan > 0)
                {
                    continue;
                }

                registration.StepsUntilScan = registration.IntervalSteps;
                _dueRadars.Add(registration);
            }
        }

        private void ApplyPendingTargets()
        {
            for (int i = 0; i < _pendingAdded.Count; i++)
            {
                IEntity entity = _pendingAdded[i];
                _targetIndices.Add(entity, _targets.Count);
                _targets.Add(entity);
                _targetTransforms.Add(entity.GetFacade<IEntityTransformFacade>().Transform);
            }

            for (int i = 0; i < _pendingRemoved.Count; i++)
            {
                IEntity entity = _pendingRemoved[i];
                int index = _targetIndices[entity];
                _targetIndices.Remove(entity);
                int last = _targets.Count - 1;
                IEntity moved = _targets[last];
                _targets[index] = moved;
                _targets.RemoveAt(last);
                _targetTransforms.RemoveAtSwapBack(index);
                if (index != last)
                {
                    _targetIndices[moved] = index;
                }
            }

            _pendingAdded.Clear();
            _pendingRemoved.Clear();
        }

        private void BuildHostility(int slotCount)
        {
            _hostility.ResizeUninitialized(slotCount * slotCount);
            for (int row = 0; row < slotCount; row++)
            {
                for (int column = 0; column < slotCount; column++)
                {
                    _hostility[row * slotCount + column] =
                        (byte)(_relations.IsHostile(_slotOwners[row], _slotOwners[column]) ? 1 : 0);
                }
            }
        }

        private int GetOwnerSlot(PlayerId owner)
        {
            if (_ownerSlots.TryGetValue(owner, out int slot))
            {
                return slot;
            }

            slot = _slotOwners.Count;
            _ownerSlots.Add(owner, slot);
            _slotOwners.Add(owner);
            return slot;
        }

        private static bool IsDetectable(IEntity entity) =>
            !entity.HealthModel.IsDestroyed && entity.HealthModel.HasUnits && !entity.IsCloaked();
    }
}
