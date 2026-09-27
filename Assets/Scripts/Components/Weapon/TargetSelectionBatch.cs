using System;
using System.Collections.Generic;
using EmpireAtWar.Services.Timing;
using EmpireAtWar.ViewComponents.Health;
using Unity.Collections;
using UnityEngine;
using Unity.Jobs;

namespace EmpireAtWar.Components.Weapon
{
    internal sealed class TargetSelectionBatch : IDisposable
    {
        internal const int JOB_SELECTION_THRESHOLD = 8;
        internal const int TARGET_JOB_BATCH_SIZE = 1;
        private readonly Func<IWeaponPresenter, int, bool> _isRegistered;
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

        private readonly List<TargetSelectionRequest> _targetSelectionRequests = new List<TargetSelectionRequest>();
        private readonly List<TargetSelectionCandidateRecord> _targetSelectionCandidates = new List<TargetSelectionCandidateRecord>();
        private NativeArray<WeaponTargetSelectionJob.Input> _targetSelectionInputs;
        private NativeArray<Unity.Mathematics.float3> _targetSelectionPositions;
        private NativeArray<WeaponTargetSelectionJob.Result> _targetSelectionResults;

        public int RequestCount => _targetSelectionRequests.Count;
        public int LastCandidateCount { get; private set; }
        public int InputCapacity => _targetSelectionInputs.IsCreated ? _targetSelectionInputs.Length : 0;
        public int PositionCapacity => _targetSelectionPositions.IsCreated ? _targetSelectionPositions.Length : 0;
        public int ResultCapacity => _targetSelectionResults.IsCreated ? _targetSelectionResults.Length : 0;

        public TargetSelectionBatch(Func<IWeaponPresenter, int, bool> isRegistered)
        {
            _isRegistered = isRegistered;
        }

        public void Queue(WeaponComponent weapon, WeaponHardPoint hardPoint, int ownerGeneration)
        {
            _targetSelectionRequests.Add(new TargetSelectionRequest
            {
                Owner = weapon,
                Weapon = weapon,
                OwnerGeneration = ownerGeneration,
                HardPoint = hardPoint
            });
        }

        public void Dispose()
        {
            _targetSelectionRequests.Clear();
            _targetSelectionCandidates.Clear();
            if (_targetSelectionInputs.IsCreated) _targetSelectionInputs.Dispose();
            if (_targetSelectionPositions.IsCreated) _targetSelectionPositions.Dispose();
            if (_targetSelectionResults.IsCreated) _targetSelectionResults.Dispose();
        }

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
                    if (!_isRegistered(request.Owner, request.OwnerGeneration)) continue;
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
                    if (!_isRegistered(request.Owner, request.OwnerGeneration)) continue;

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
            LastCandidateCount = _targetSelectionCandidates.Count;
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
            EmpireAtWar.Utils.NativeArrayBuffer.EnsureCapacity(ref _targetSelectionInputs, requestCount);
            EmpireAtWar.Utils.NativeArrayBuffer.EnsureCapacity(ref _targetSelectionResults, requestCount);
            EmpireAtWar.Utils.NativeArrayBuffer.EnsureCapacity(ref _targetSelectionPositions, candidateCount);
        }



    }
}
