using System;
using System.Collections.Generic;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Services.Timing;
using EmpireAtWar.ViewComponents.Health;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using Unity.Jobs;

namespace EmpireAtWar.Components.Weapon
{
    internal sealed class TargetSelectionBatch : IDisposable
    {
        internal const int JOB_SELECTION_THRESHOLD = 8;
        internal const int TARGET_JOB_BATCH_SIZE = 1;

        private readonly Func<IWeaponPresenter, int, bool> _isRegistered;
        private readonly List<TargetSelectionRequest> _targetSelectionRequests = new List<TargetSelectionRequest>();
        private readonly Dictionary<WeaponComponent, int> _capturedOwners = new Dictionary<WeaponComponent, int>();
        private readonly List<WeaponComponent> _capturedWeapons = new List<WeaponComponent>();
        private readonly List<TargetSelectionRequest> _capturedSpans = new List<TargetSelectionRequest>();
        private readonly TargetOriginCapture _origins = new TargetOriginCapture();
        private readonly TargetSelectionCapture _capture = new TargetSelectionCapture();

        private NativeArray<WeaponTargetSelectionJob.Input> _targetSelectionInputs;
        private NativeArray<int> _candidateSlots;
        private NativeArray<int> _candidateGroups;
        private NativeArray<byte> _groupEligible;
        private NativeArray<WeaponTargetSelectionJob.Result> _targetSelectionResults;

        public TargetSelectionCapture Units => _capture;
        public int RequestCount => _targetSelectionRequests.Count;
        public int LastCandidateCount { get; private set; }
        public int InputCapacity => _targetSelectionInputs.IsCreated ? _targetSelectionInputs.Length : 0;
        public int PositionCapacity => _capture.PositionCapacity;
        public int ResultCapacity => _targetSelectionResults.IsCreated ? _targetSelectionResults.Length : 0;

        public TargetSelectionBatch(Func<IWeaponPresenter, int, bool> isRegistered)
        {
            _isRegistered = isRegistered;
        }

        public void Dispose()
        {
            _targetSelectionRequests.Clear();
            ClearCaptured();
            _capture.Dispose();
            _origins.Dispose();
            if (_targetSelectionInputs.IsCreated) _targetSelectionInputs.Dispose();
            if (_candidateSlots.IsCreated) _candidateSlots.Dispose();
            if (_candidateGroups.IsCreated) _candidateGroups.Dispose();
            if (_groupEligible.IsCreated) _groupEligible.Dispose();
            if (_targetSelectionResults.IsCreated) _targetSelectionResults.Dispose();
        }

        public void Queue(WeaponComponent weapon, WeaponHardPoint hardPoint, int ownerGeneration)
        {
            _origins.Register(weapon, hardPoint);
            _targetSelectionRequests.Add(new TargetSelectionRequest
            {
                Owner = weapon,
                Weapon = weapon,
                OwnerGeneration = ownerGeneration,
                HardPoint = hardPoint
            });
        }

        public void Unregister(IWeaponPresenter owner) => _origins.Unregister(owner);

        public void Process()
        {
            LastCandidateCount = 0;
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
                        if (_isRegistered(request.Owner, request.OwnerGeneration))
                            ApplyTargetSelection(request, EvaluateTargetSelectionSerial(request));
                    }
                }

                _targetSelectionRequests.Clear();
                ClearCaptured();
                return;
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            long selectionStart = System.Diagnostics.Stopwatch.GetTimestamp();
            using (BattleProfilerMarkers.TargetBatchPrepare.Auto())
#endif
            {
                EmpireAtWar.Utils.NativeArrayBuffer.EnsureCapacity(ref _targetSelectionInputs, _targetSelectionRequests.Count);
                EmpireAtWar.Utils.NativeArrayBuffer.EnsureCapacity(ref _targetSelectionResults, _targetSelectionRequests.Count);
                for (int i = 0; i < _targetSelectionRequests.Count; i++)
                {
                    TargetSelectionRequest request = _targetSelectionRequests[i];
                    _targetSelectionInputs[i] = new WeaponTargetSelectionJob.Input
                    {
                        Origin = new float3(request.Origin.x, request.Origin.y, request.Origin.z),
                        ParentRotation = new quaternion(request.ParentRotation.x, request.ParentRotation.y,
                            request.ParentRotation.z, request.ParentRotation.w),
                        MaxDistance = request.HardPoint.MaxAttackDistance,
                        MinYaw = request.HardPoint.MinYaw,
                        MaxYaw = request.HardPoint.MaxYaw,
                        CandidateStart = request.CandidateStart,
                        CandidateCount = request.CandidateCount,
                        GroupStart = request.GroupStart,
                        StrikecraftOnly = request.CandidateCount > 0 && request.HardPoint.StrikecraftOnly ? 1 : 0,
                        ShipsOnly = request.HardPoint.ShipsOnly ? 1 : 0,
                        Preference = (int)request.HardPoint.PreferredTarget
                    };
                }
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using (BattleProfilerMarkers.TargetBatchJob.Auto())
#endif
            {
                WeaponTargetSelectionJob job = new WeaponTargetSelectionJob
                {
                    Inputs = _targetSelectionInputs,
                    CandidateSlots = _candidateSlots,
                    CandidateGroups = _candidateGroups,
                    GroupEligible = _groupEligible,
                    SlotPositions = _capture.Positions,
                    SlotFlags = _capture.Flags,
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
                    if (!_isRegistered(request.Owner, request.OwnerGeneration)) continue;
                    ApplyTargetSelection(request, _targetSelectionResults[i]);
                }
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            AttackSequenceDiagnostics.RecordTargetSelectionBatchTime(selectionStart, _targetSelectionRequests.Count);
#endif
            _targetSelectionRequests.Clear();
            ClearCaptured();
        }

        private void CaptureTargetSelections()
        {
            // Capture every owner once at one LateTick boundary before either selection path commits.
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using (BattleProfilerMarkers.TargetBatchCapture.Auto())
#endif
            {
                // Owners first: a stale owner rebuilds its candidates (acquiring slots) before slots are read.
                int candidateCount = 0;
                int groupCount = 0;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                using (BattleProfilerMarkers.TargetBatchCaptureOwners.Auto())
#endif
                {
                    for (int i = 0; i < _targetSelectionRequests.Count; i++)
                    {
                        TargetSelectionRequest request = _targetSelectionRequests[i];
                        if (!_isRegistered(request.Owner, request.OwnerGeneration)) continue;
                        if (_capturedOwners.TryGetValue(request.Weapon, out int ownerIndex))
                        {
                            request.CapturedOwnerIndex = ownerIndex;
                            request.OriginSlot = _origins.GetSlot(request.HardPoint);
                            _targetSelectionRequests[i] = request;
                            continue;
                        }
                        WeaponComponent weapon = request.Weapon;
                        weapon.PrepareTargetSelection(_capture);
                        request.CapturedOwnerIndex = _capturedSpans.Count;
                        request.OriginSlot = _origins.GetSlot(request.HardPoint);
                        _targetSelectionRequests[i] = request;
                        _capturedOwners.Add(weapon, request.CapturedOwnerIndex);
                        _capturedSpans.Add(new TargetSelectionRequest
                        {
                            CandidateStart = candidateCount,
                            CandidateCount = weapon.CandidateCount,
                            GroupStart = groupCount,
                            TargetVersion = weapon.TargetVersion
                        });
                        _capturedWeapons.Add(weapon);
                        candidateCount += weapon.CandidateCount;
                        groupCount += weapon.CandidateGroupCount;
                    }
                }

                // Slots are all acquired now, so ship positions are read on workers while the copies below run.
                if (candidateCount > 0) _capture.BeginPositionRead();
                _origins.BeginRead();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                using (BattleProfilerMarkers.TargetBatchCaptureCopy.Auto())
#endif
                {
                    for (int i = 0; i < _capturedWeapons.Count; i++)
                        _capture.MarkUsed(_capturedWeapons[i].CandidateSlots, _capturedWeapons[i].CandidateCount);
                    EmpireAtWar.Utils.NativeArrayBuffer.EnsureCapacity(ref _candidateSlots, candidateCount);
                    EmpireAtWar.Utils.NativeArrayBuffer.EnsureCapacity(ref _candidateGroups, candidateCount);
                    EmpireAtWar.Utils.NativeArrayBuffer.EnsureCapacity(ref _groupEligible, groupCount);
                    for (int i = 0; i < _capturedWeapons.Count; i++)
                    {
                        WeaponComponent weapon = _capturedWeapons[i];
                        // An owner without candidates may start exactly at a full buffer's end, which Copy rejects.
                        if (weapon.CandidateCount == 0) continue;
                        TargetSelectionRequest captured = _capturedSpans[i];
                        NativeArray<int>.Copy(weapon.CandidateSlots, 0, _candidateSlots, captured.CandidateStart,
                            captured.CandidateCount);
                        NativeArray<int>.Copy(weapon.CandidateGroupIndices, 0, _candidateGroups, captured.CandidateStart,
                            captured.CandidateCount);
                        NativeArray<byte>.Copy(weapon.GroupEligibility, 0, _groupEligible, captured.GroupStart,
                            weapon.CandidateGroupCount);
                    }

                    _origins.CompleteRead();
                    for (int i = 0; i < _targetSelectionRequests.Count; i++)
                    {
                        TargetSelectionRequest request = _targetSelectionRequests[i];
                        if (!_isRegistered(request.Owner, request.OwnerGeneration)) continue;
                        TargetSelectionRequest captured = _capturedSpans[request.CapturedOwnerIndex];
                        request.CandidateStart = captured.CandidateStart;
                        request.CandidateCount = captured.CandidateCount;
                        request.GroupStart = captured.GroupStart;
                        request.TargetVersion = captured.TargetVersion;
                        float3 position = _origins.Positions[request.OriginSlot];
                        quaternion rotation = _origins.ParentRotations[request.OriginSlot];
                        request.Origin = new Vector3(position.x, position.y, position.z);
                        request.ParentRotation = new Quaternion(rotation.value.x, rotation.value.y, rotation.value.z, rotation.value.w);
                        _targetSelectionRequests[i] = request;
                    }
                }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                using (BattleProfilerMarkers.TargetBatchCapturePositions.Auto())
#endif
                    _capture.Refresh();
                LastCandidateCount = candidateCount;
            }
        }

        private WeaponTargetSelectionJob.Result EvaluateTargetSelectionSerial(TargetSelectionRequest request)
        {
            WeaponTargetSelectionJob.Result result = new WeaponTargetSelectionJob.Result
            {
                CandidateIndex = -1,
                NearestDistance = float.PositiveInfinity
            };
            NativeArray<byte> flags = _capture.Flags;
            NativeArray<float3> positions = _capture.Positions;
            int preference = (int)request.HardPoint.PreferredTarget;
            int passes = preference == (int)TargetPreference.Any ? 1 : 2;
            int bestGroup = -1;
            float bestDistanceSquared = float.PositiveInfinity;
            for (int pass = 0; pass < passes && result.CandidateIndex < 0; pass++)
            for (int i = request.CandidateStart; i < request.CandidateStart + request.CandidateCount; i++)
            {
                // Same choice as WeaponTargetSelectionJob: the nearest reachable hardpoint of the first group in reach.
                if (result.CandidateIndex >= 0 && _candidateGroups[i] != bestGroup) break;
                if (_groupEligible[request.GroupStart + _candidateGroups[i]] == 0) continue;
                int slot = _candidateSlots[i];
                byte slotFlags = flags[slot];
                if ((slotFlags & TargetSelectionCapture.SLOT_VALID) == 0) continue;
                if (request.HardPoint.StrikecraftOnly && (slotFlags & TargetSelectionCapture.SLOT_STRIKECRAFT) == 0) continue;
                if (request.HardPoint.ShipsOnly && (slotFlags & TargetSelectionCapture.SLOT_STRIKECRAFT) != 0) continue;
                if (WeaponTargetSelectionJob.IsPreferred(preference, slotFlags) != (pass == 0)) continue;
                result.Visited++;
                float3 position = positions[slot];
                result.NearestDistance = Mathf.Min(result.NearestDistance,
                    new Vector2(position.x - request.Origin.x, position.z - request.Origin.z).magnitude);
                float distanceSquared = math.lengthsq(new float2(position.x - request.Origin.x, position.z - request.Origin.z));
                bool canAttack = WeaponTargetSelector.TryCalculateAim(
                    new Vector3(position.x, position.y, position.z), request.Origin, request.ParentRotation,
                    request.HardPoint.MaxAttackDistance, request.HardPoint.MinYaw, request.HardPoint.MaxYaw,
                    out Quaternion aim, out bool inRange);
                // Like the job, a hardpoint no nearer than the current pick is not considered.
                if (distanceSquared >= bestDistanceSquared) continue;
                if (inRange)
                {
                    result.HasInRangeAim = 1;
                    result.LastInRangeAim = new float4(aim.x, aim.y, aim.z, aim.w);
                }

                if (!canAttack) continue;
                result.CandidateIndex = i;
                result.SelectedAim = new float4(aim.x, aim.y, aim.z, aim.w);
                bestGroup = _candidateGroups[i];
                bestDistanceSquared = distanceSquared;
            }

            return result;
        }

        private void ApplyTargetSelection(TargetSelectionRequest request, WeaponTargetSelectionJob.Result result)
        {
            AttackSequenceDiagnostics.RecordCandidateVisits(result.Visited);
            if (request.TargetVersion != request.Weapon.TargetVersion)
            {
                AttackSequenceDiagnostics.RecordTargetSelectionFallback(WeaponTargetSelectionJob.FALLBACK_TARGET_VERSION);
                request.Weapon.CommitTargetSelectionSerial(request.HardPoint);
                return;
            }

            if (result.RequiresSerialFallback != 0)
            {
                AttackSequenceDiagnostics.RecordTargetSelectionFallback(result.FallbackReason);
                result = EvaluateTargetSelectionSerial(request);
                AttackSequenceDiagnostics.RecordCandidateVisits(result.Visited);
            }

            if (result.CandidateIndex >= 0 &&
                (result.CandidateIndex < request.CandidateStart ||
                 result.CandidateIndex >= request.CandidateStart + request.CandidateCount))
            {
                AttackSequenceDiagnostics.RecordTargetSelectionFallback(WeaponTargetSelectionJob.FALLBACK_OUT_OF_SPAN);
                request.Weapon.CommitTargetSelectionSerial(request.HardPoint);
                return;
            }

            if (result.CandidateIndex < 0)
            {
                request.Weapon.CommitTargetSelection(request.HardPoint, request.TargetVersion, result, null, null, 0);
                return;
            }

            int slot = _candidateSlots[result.CandidateIndex];
            AttackData group = request.Weapon.GetCandidateGroup(_candidateGroups[result.CandidateIndex]);
            request.Weapon.CommitTargetSelection(request.HardPoint, request.TargetVersion, result, group,
                _capture.GetUnit(slot), _capture.GetGeneration(slot));
        }

        private void ClearCaptured()
        {
            _capturedOwners.Clear();
            _capturedWeapons.Clear();
            _capturedSpans.Clear();
            _capture.ClearTargets();
        }

        private struct TargetSelectionRequest
        {
            public IWeaponPresenter Owner;

            public WeaponComponent Weapon;
            public WeaponHardPoint HardPoint;

            public Vector3 Origin;
            public Quaternion ParentRotation;

            public int OwnerGeneration;
            public int TargetVersion;
            public int CandidateStart;
            public int CandidateCount;
            public int GroupStart;
            public int CapturedOwnerIndex;
            public int OriginSlot;
        }
    }
}
