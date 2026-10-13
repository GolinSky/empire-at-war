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
    [EmpireAtWar.Lifecycle.LifecycleOrder(LateTick = -2000)]
    public sealed class FighterPoseBatch : ILateTickable, IOrderedDisposable
    {
        private const int INITIAL_CAPACITY = 256;

        private readonly Stack<int> _freeSlots = new Stack<int>();
        // Index = slot.
        private TransformAccessArray _fighters;
        private TransformAccessArray _bodies;
        private NativeArray<FighterPose> _poses;

        public int Register(Transform fighter, Transform body, Quaternion bodyRestRotation)
        {
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
            _poses[slot] = default;
            _freeSlots.Push(slot);
        }

        public void Set(int slot, Vector3 position, Vector3 forward, float bank)
        {
            FighterPose pose = _poses[slot];
            pose.Position = position;
            pose.Forward = forward;
            pose.Bank = bank;
            pose.IsActive = 1;
            _poses[slot] = pose;
        }

        public void LateTick()
        {
            if (!_fighters.isCreated || _fighters.length == 0) return;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using (BattleProfilerMarkers.SquadronPlacement.Auto())
#endif
            {
                JobHandle placed = new FighterPlacementJob { Poses = _poses }.Schedule(_fighters);
                new FighterBankJob { Poses = _poses }.Schedule(_bodies, placed).Complete();
            }
        }

        public void Dispose()
        {
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
