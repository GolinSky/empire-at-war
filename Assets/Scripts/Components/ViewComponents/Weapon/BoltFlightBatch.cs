using System;
using System.Collections.Generic;
using EmpireAtWar.Services.Timing;
using EmpireAtWar.Utils;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Jobs;
using VContainer;
using EmpireAtWar.Lifecycle;

namespace EmpireAtWar.ViewComponents.Weapon
{
    /// <summary>
    /// Flies every bolt in one pass instead of one Update per bolt. Tracking and landing run on the main thread,
    /// since impact points and impact effects need managed state; one Burst job then places all flying bolts.
    /// </summary>
    public sealed class BoltFlightBatch : ITickable, IOrderedDisposable
    {
        private const int INITIAL_CAPACITY = 256;

        private readonly List<BoltShot> _bolts = new List<BoltShot>();
        // Index = bolt in _bolts.
        private TransformAccessArray _transforms;
        private NativeArray<BoltPose> _poses;

        public int Count => _bolts.Count;

        public void Add(BoltShot bolt)
        {
            if (!_transforms.isCreated) _transforms = new TransformAccessArray(INITIAL_CAPACITY);
            _bolts.Add(bolt);
            _transforms.Add(bolt.transform);
            bolt.IsInFlightBatch = true;
        }

        public void Tick() => Advance(Time.time, Time.deltaTime);

        public void Advance(float now, float deltaTime)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using (BattleProfilerMarkers.ProjectileTurretUpdate.Auto())
            {
#endif
            if (_bolts.Count == 0) return;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using (BattleProfilerMarkers.ProjectileTrack.Auto())
#endif
            {
                for (int i = _bolts.Count - 1; i >= 0; i--)
                {
                    BoltShot bolt = _bolts[i];
                    // Landed, returned to its pool or destroyed with the scene: it leaves the batch.
                    if (bolt != null && bolt.Advance(now, deltaTime)) continue;
                    if (bolt != null) bolt.IsInFlightBatch = false;
                    int last = _bolts.Count - 1;
                    _bolts[i] = _bolts[last];
                    _bolts.RemoveAt(last);
                    _transforms.RemoveAtSwapBack(i);
                }
            }

            if (_bolts.Count == 0) return;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using (BattleProfilerMarkers.ProjectileMove.Auto())
#endif
            {
                NativeArrayBuffer.EnsureCapacity(ref _poses, _bolts.Count);
                for (int i = 0; i < _bolts.Count; i++) _poses[i] = _bolts[i].Pose;
                new BoltPlacementJob { Poses = _poses }.Schedule(_transforms).Complete();
            }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            }
#endif
        }

        public void Dispose()
        {
            if (_transforms.isCreated) _transforms.Dispose();
            if (_poses.IsCreated) _poses.Dispose();
        }

        public void Clear()
        {
            foreach (BoltShot bolt in _bolts)
                if (bolt != null) bolt.IsInFlightBatch = false;
            _bolts.Clear();
            if (_transforms.isCreated)
                while (_transforms.length > 0) _transforms.RemoveAtSwapBack(_transforms.length - 1);
        }
    }
}
