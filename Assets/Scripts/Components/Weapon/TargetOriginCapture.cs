using System;
using System.Collections.Generic;
using EmpireAtWar.Utils;
using EmpireAtWar.ViewComponents.Health;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Jobs;

namespace EmpireAtWar.Components.Weapon
{
    internal sealed class TargetOriginCapture : IDisposable
    {
        private const int INITIAL_CAPACITY = 256;
        private const int JOB_BATCH_SIZE = 64;
        private readonly Dictionary<WeaponHardPoint, int> _slots = new Dictionary<WeaponHardPoint, int>();
        private readonly List<WeaponHardPoint> _hardPoints = new List<WeaponHardPoint>();
        private readonly List<IWeaponPresenter> _owners = new List<IWeaponPresenter>();
        private TransformAccessArray _transforms;
        private NativeArray<float3> _positions;
        private NativeArray<quaternion> _parentRotations;
        private JobHandle _read;

        public NativeArray<float3> Positions => _positions;
        public NativeArray<quaternion> ParentRotations => _parentRotations;

        public void Register(IWeaponPresenter owner, WeaponHardPoint hardPoint)
        {
            if (_slots.ContainsKey(hardPoint)) return;
            if (!_transforms.isCreated) _transforms = new TransformAccessArray(INITIAL_CAPACITY);
            _slots.Add(hardPoint, _hardPoints.Count);
            _hardPoints.Add(hardPoint);
            _owners.Add(owner);
            _transforms.Add(hardPoint.transform);
        }

        public int GetSlot(WeaponHardPoint hardPoint) => _slots[hardPoint];

        public void Unregister(IWeaponPresenter owner)
        {
            for (int i = _owners.Count - 1; i >= 0; i--)
            {
                if (!ReferenceEquals(_owners[i], owner)) continue;
                int last = _owners.Count - 1;
                _slots.Remove(_hardPoints[i]);
                _transforms.RemoveAtSwapBack(i);
                if (i != last)
                {
                    _hardPoints[i] = _hardPoints[last];
                    _owners[i] = _owners[last];
                    _slots[_hardPoints[i]] = i;
                }
                _hardPoints.RemoveAt(last);
                _owners.RemoveAt(last);
            }
        }

        public void BeginRead()
        {
            NativeArrayBuffer.EnsureCapacity(ref _positions, _hardPoints.Count);
            NativeArrayBuffer.EnsureCapacity(ref _parentRotations, _hardPoints.Count);
            if (_hardPoints.Count == 0) return;
            _read = new TargetOriginReadJob
            {
                Positions = _positions,
                ParentRotations = _parentRotations
            }.ScheduleReadOnly(_transforms, JOB_BATCH_SIZE);
        }

        public void CompleteRead() => _read.Complete();

        public void Dispose()
        {
            _read.Complete();
            if (_transforms.isCreated) _transforms.Dispose();
            if (_positions.IsCreated) _positions.Dispose();
            if (_parentRotations.IsCreated) _parentRotations.Dispose();
            _slots.Clear();
            _hardPoints.Clear();
            _owners.Clear();
        }
    }
}
