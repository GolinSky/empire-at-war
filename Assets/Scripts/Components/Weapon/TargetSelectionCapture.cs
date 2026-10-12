using System;
using System.Collections.Generic;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Utils;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Jobs;

namespace EmpireAtWar.Components.Weapon
{
    /// <summary>
    /// Stable slots for every hardpoint some weapon may target. Weapons keep slot indices; each selection batch
    /// refreshes the used slots once (health state once per target) and the selection job reads the slot arrays.
    /// Positions: fighters read their flight-model pose every batch; ship hardpoints are read from transforms in
    /// one parallel job, a quarter of them per batch, so they may be a few batches old. Commits re-check live targets.
    /// </summary>
    internal sealed class TargetSelectionCapture : IDisposable
    {
        public const byte SLOT_VALID = 1;
        public const byte SLOT_STRIKECRAFT = 2;
        internal const int SHIP_POSITION_STRIPES = 4;
        private const int POSITION_JOB_BATCH_SIZE = 64;
        private const int INITIAL_TRANSFORM_CAPACITY = 256;

        private readonly Dictionary<IHardPointModel, int> _slotsByUnit = new Dictionary<IHardPointModel, int>();
        private readonly List<IHardPointModel> _units = new List<IHardPointModel>();
        private readonly List<IHealthModelObserver> _healths = new List<IHealthModelObserver>();
        private readonly List<int> _references = new List<int>();
        private readonly Stack<int> _freeSlots = new Stack<int>();
        private readonly Dictionary<IHealthModelObserver, TargetState> _targets =
            new Dictionary<IHealthModelObserver, TargetState>();
        private readonly Dictionary<IEntity, EntityState> _entities = new Dictionary<IEntity, EntityState>();
        // Slots used by this batch's owners; only these are refreshed.
        private readonly List<int> _usedSlots = new List<int>();
        private int[] _usedStamps = Array.Empty<int>();
        private int _batchStamp = 1;
        private int[] _generations = Array.Empty<int>();
        // 1 = ship hardpoint read from its transform by stripe; 0 = fighter, whose Position is its flight-model pose.
        private byte[] _transformSlots = Array.Empty<byte>();
        // Ship slots acquired since the last Refresh; read once directly so no stripe leaves them unset.
        private readonly List<int> _unreadSlots = new List<int>();
        private int _stripe;
        // A regrown position buffer starts empty, so the next job reads every ship slot.
        private bool _readAllSlots;
        // Slots arrive in runs of one target, so the last lookup usually answers the next.
        private IHealthModelObserver _lastHealth;
        private TargetState _lastState;

        private NativeArray<float3> _positions;
        private NativeArray<byte> _flags;
        private NativeArray<byte> _transformSlotMask;
        // Index = slot. A freed slot keeps its old transform until reused; nothing reads it meanwhile.
        // Every IHardPointModel's Position is its Transform's position.
        private TransformAccessArray _transforms;

        public NativeArray<float3> Positions => _positions;
        public NativeArray<byte> Flags => _flags;
        public int SlotCount => _units.Count;
        public int PositionCapacity => _positions.IsCreated ? _positions.Length : 0;

        public int Acquire(IHardPointModel unit, IHealthModelObserver health)
        {
            if (_slotsByUnit.TryGetValue(unit, out int slot))
            {
                _references[slot]++;
                return slot;
            }

            if (!_transforms.isCreated) _transforms = new TransformAccessArray(INITIAL_TRANSFORM_CAPACITY);
            byte transformSlot = health.ShipClass.IsStrikecraft() ? (byte)0 : (byte)1;
            if (_freeSlots.Count > 0)
            {
                slot = _freeSlots.Pop();
                _units[slot] = unit;
                _healths[slot] = health;
                _references[slot] = 1;
                _transforms[slot] = unit.Transform;
                _transformSlots[slot] = transformSlot;
            }
            else
            {
                slot = _units.Count;
                _units.Add(unit);
                _healths.Add(health);
                _references.Add(1);
                _transforms.Add(unit.Transform);
                if (_transformSlots.Length <= slot) Array.Resize(ref _transformSlots, Math.Max(slot + 1, _transformSlots.Length * 2));
                _transformSlots[slot] = transformSlot;
            }

            if (transformSlot == 1) _unreadSlots.Add(slot);
            _slotsByUnit.Add(unit, slot);
            return slot;
        }

        public void Release(int slot)
        {
            if (--_references[slot] > 0) return;
            _slotsByUnit.Remove(_units[slot]);
            _units[slot] = null;
            _healths[slot] = null;
            _freeSlots.Push(slot);
        }

        public IHardPointModel GetUnit(int slot) => _units[slot];

        /// <summary>Generation captured by the last <see cref="Refresh"/>.</summary>
        public int GetGeneration(int slot) => _generations[slot];

        /// <summary>Marks an owner's candidate slots for this batch's <see cref="Refresh"/>.</summary>
        public void MarkUsed(int[] slots, int count)
        {
            if (_usedStamps.Length < _units.Count) Array.Resize(ref _usedStamps, Math.Max(_units.Count, _usedStamps.Length * 2));
            for (int i = 0; i < count; i++)
            {
                int slot = slots[i];
                if (_usedStamps[slot] == _batchStamp) continue;
                _usedStamps[slot] = _batchStamp;
                _usedSlots.Add(slot);
            }
        }

        /// <summary>
        /// Reads every marked hardpoint once for this selection batch; slots no owner of the batch uses keep stale
        /// data. Target states are shared with <see cref="IsWreck"/> until <see cref="ClearTargets"/> ends the batch.
        /// </summary>
        public void Refresh()
        {
            int count = _units.Count;
            int positionCapacity = PositionCapacity;
            NativeArrayBuffer.EnsureCapacity(ref _positions, count);
            NativeArrayBuffer.EnsureCapacity(ref _flags, count);
            NativeArrayBuffer.EnsureCapacity(ref _transformSlotMask, count);
            if (_generations.Length < _positions.Length) Array.Resize(ref _generations, _positions.Length);
            if (PositionCapacity != positionCapacity) _readAllSlots = true;
            if (_usedSlots.Count > 0)
            {
                NativeArray<byte>.Copy(_transformSlots, _transformSlotMask, count);
                new TargetSlotPositionJob
                {
                    Positions = _positions,
                    TransformSlots = _transformSlotMask,
                    Stripe = _readAllSlots ? 0 : _stripe,
                    StripeCount = _readAllSlots ? 1 : SHIP_POSITION_STRIPES
                }.ScheduleReadOnly(_transforms, POSITION_JOB_BATCH_SIZE).Complete();
                _readAllSlots = false;
                _stripe = (_stripe + 1) % SHIP_POSITION_STRIPES;

                for (int i = 0; i < _unreadSlots.Count; i++)
                {
                    int slot = _unreadSlots[i];
                    // Released before its first read; a fighter reusing the slot is read below.
                    if (_units[slot] != null && _transformSlots[slot] == 1) _positions[slot] = _units[slot].Position;
                }
                _unreadSlots.Clear();
            }

            for (int i = 0; i < _usedSlots.Count; i++)
            {
                int slot = _usedSlots[i];
                IHardPointModel unit = _units[slot];

                TargetState state = GetTarget(_healths[slot]);
                // Matches AttackData.CanTarget: destroyed hardpoints count only once the whole ship is a wreck.
                if (state.IsDestroyed || unit.IsDestroyed && state.HasLiveHardPoints)
                {
                    _flags[slot] = 0;
                    continue;
                }

                if (_transformSlots[slot] == 0) _positions[slot] = unit.Position;
                _generations[slot] = unit.Generation;
                _flags[slot] = (byte)(SLOT_VALID | (state.IsStrikecraft ? SLOT_STRIKECRAFT : 0));
                AttackSequenceDiagnostics.RecordTargetSnapshot();
            }
        }

        /// <summary>Matches WeaponComponent's live check: not cloaked, and not hidden by fog for local-team weapons.</summary>
        public bool CanAcquire(AttackData group, bool respectsLocalFog)
        {
            IEntity entity = group.TargetEntity;
            if (!_entities.TryGetValue(entity, out EntityState state))
            {
                state = new EntityState { IsCloaked = entity.IsCloaked() };
                _entities.Add(entity, state);
            }
            if (state.IsCloaked) return false;
            if (!respectsLocalFog) return true;
            if (state.FogRead == 0)
            {
                state.FogRead = 1;
                state.IsHiddenByFog = entity.IsHiddenByFog();
                _entities[entity] = state;
            }
            return !state.IsHiddenByFog;
        }

        public bool IsWreck(AttackData group)
        {
            TargetState state = GetTarget(group.TargetHealth);
            return !state.IsDestroyed && !state.HasLiveHardPoints;
        }

        public void ClearTargets()
        {
            _targets.Clear();
            _entities.Clear();
            _usedSlots.Clear();
            _batchStamp++;
            _lastHealth = null;
        }

        public void Dispose()
        {
            ClearTargets();
            if (_positions.IsCreated) _positions.Dispose();
            if (_flags.IsCreated) _flags.Dispose();
            if (_transformSlotMask.IsCreated) _transformSlotMask.Dispose();
            if (_transforms.isCreated) _transforms.Dispose();
        }

        private TargetState GetTarget(IHealthModelObserver health)
        {
            if (ReferenceEquals(health, _lastHealth)) return _lastState;
            if (!_targets.TryGetValue(health, out TargetState state))
            {
                state = new TargetState
                {
                    IsDestroyed = health.IsDestroyed,
                    HasLiveHardPoints = !health.IsDestroyed && health.HasLiveHardPoints,
                    IsStrikecraft = health.ShipClass.IsStrikecraft()
                };
                _targets.Add(health, state);
            }
            _lastHealth = health;
            _lastState = state;
            return state;
        }

        private struct EntityState
        {
            public bool IsCloaked;
            public bool IsHiddenByFog;
            public byte FogRead;
        }

        private struct TargetState
        {
            public bool IsDestroyed;
            public bool HasLiveHardPoints;
            public bool IsStrikecraft;
        }
    }
}
