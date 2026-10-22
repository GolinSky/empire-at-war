using System;
using System.Collections.Generic;
using EmpireAtWar.Services.Timing;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Jobs;
using VContainer;
using EmpireAtWar.Lifecycle;

namespace EmpireAtWar.ViewComponents.Squadrons
{
    /// <summary>
    /// Writes every fighter's flight pose in Burst jobs once per frame instead of two transform writes per fighter.
    /// Its LateTick runs before the attack coordinator's, so fighter guns fire from this frame's pose.
    /// </summary>
    [LifecycleOrder(LateTick = LifecycleOrders.FIGHTER_POSE_LATE_TICK,
        Dispose = LifecycleOrders.FIGHTER_POSE_DISPOSE)]
    public sealed class FighterPoseBatch : IPostTickable, ILateTickable, IOrderedDisposable
    {
        private const int INITIAL_CAPACITY = 256;

        private readonly Stack<int> _freeSlots = new Stack<int>();
        // Index = slot.
        private TransformAccessArray _fighters;
        private TransformAccessArray _bodies;
        private NativeArray<FighterPose> _poses;
        private JobHandle _placement;
        private bool _hasPendingPlacement;
        private bool _isDisposed;

        public int Register(Transform fighter, Transform body, Quaternion bodyRestRotation)
        {
            if (_isDisposed) throw new ObjectDisposedException(nameof(FighterPoseBatch));
            CompletePlacement();
            if (!_fighters.isCreated)
            {
                _fighters = new TransformAccessArray(INITIAL_CAPACITY);
                _bodies = new TransformAccessArray(INITIAL_CAPACITY);
            }

            int slot;
            if (_freeSlots.Count > 0)
            {
                slot = _freeSlots.Pop();
                _fighters[slot] = fighter;
                _bodies[slot] = body;
            }
            else
            {
                slot = _fighters.length;
                _fighters.Add(fighter);
                _bodies.Add(body);
                Grow(slot + 1);
            }

            _poses[slot] = new FighterPose { BodyRestRotation = bodyRestRotation };
            return slot;
        }

        public void Unregister(int slot)
        {
            // The battle owner disposes before entity scopes release their registrations.
            if (_isDisposed) return;
            CompletePlacement();
            _poses[slot] = default;
            _freeSlots.Push(slot);
        }

        public void Set(int slot, Vector3 position, Vector3 forward, float bank)
        {
            CompletePlacement();
            FighterPose pose = _poses[slot];
            pose.Position = position;
            pose.Forward = forward;
            pose.Bank = bank;
            pose.IsActive = 1;
            _poses[slot] = pose;
        }

        public void PostTick()
        {
            SchedulePlacement();
            if (_hasPendingPlacement) JobHandle.ScheduleBatchedJobs();
        }

        public void LateTick()
        {
            // Direct callers and mutations after PostTick still publish the latest poses.
            if (!_hasPendingPlacement) SchedulePlacement();
            CompletePlacement();
        }

        private void SchedulePlacement()
        {
            CompletePlacement();
            if (!_fighters.isCreated || _fighters.length == 0) return;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using (BattleProfilerMarkers.SquadronPlacement.Auto())
#endif
            {
                _placement = new FighterPlacementJob { Poses = _poses }.Schedule(_fighters);
                _hasPendingPlacement = true;
                try
                {
                    _placement = new FighterBankJob { Poses = _poses }.Schedule(_bodies, _placement);
                }
                catch
                {
                    CompletePlacement();
                    throw;
                }
            }
        }

        internal void CompletePlacement()
        {
            if (!_hasPendingPlacement) return;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using (BattleProfilerMarkers.SquadronPlacement.Auto())
#endif
                _placement.Complete();
            _hasPendingPlacement = false;
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            CompletePlacement();
            _isDisposed = true;
            if (_fighters.isCreated) _fighters.Dispose();
            if (_bodies.isCreated) _bodies.Dispose();
            if (_poses.IsCreated) _poses.Dispose();
        }

        // Keeps poses already set this frame, unlike a plain reallocation.
        private void Grow(int required)
        {
            if (_poses.IsCreated && _poses.Length >= required) return;
            int capacity = _poses.IsCreated ? _poses.Length * 2 : INITIAL_CAPACITY;
            while (capacity < required) capacity *= 2;
            NativeArray<FighterPose> grown = new NativeArray<FighterPose>(capacity, Allocator.Persistent);
            if (_poses.IsCreated)
            {
                NativeArray<FighterPose>.Copy(_poses, grown, _poses.Length);
                _poses.Dispose();
            }

            _poses = grown;
        }
    }
}
