using System.Collections.Generic;
using EmpireAtWar.Components.Hangar;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.MiningFacility;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Players;
using Unity.Profiling;
using UnityEngine;
using GameEntity = EmpireAtWar.Entities.BaseEntity.IEntity;

namespace EmpireAtWar.Services.Enemy
{
    /// <summary>
    /// Battle-wide per-frame snapshot of living entities, pre-split by role and owner so multiple AIs
    /// read one cached pass instead of scanning <see cref="IEntityLocator.Entities"/> repeatedly.
    /// </summary>
    public sealed class AiWorldSnapshot : IInitializable, ILateDisposable
    {
        public readonly struct Entry
        {
            public readonly GameEntity Entity;
            public readonly long Id;
            public readonly PlayerId Owner;
            public readonly Vector3 Position;
            public readonly bool HasUnitType;
            public readonly UnitTypeId UnitTypeId;
            public readonly ShipClass ShipClass;
            public readonly bool IsPlayerBase;
            public readonly bool IsUnit;
            public readonly bool IsMiningFacility;
            public readonly bool HasUnits;
            public readonly float Hull;
            public readonly float Shields;
            public readonly float OffenseScale;
            public readonly int HangarSquadrons;

            public Entry(
                GameEntity entity,
                Vector3 position,
                bool hasUnitType,
                UnitTypeId unitTypeId,
                ShipClass shipClass,
                bool isPlayerBase,
                bool isUnit,
                bool isMiningFacility,
                bool hasUnits,
                float hull,
                float shields,
                float offenseScale,
                int hangarSquadrons)
            {
                Entity = entity;
                Id = entity.Id;
                Owner = entity.Owner;
                Position = position;
                HasUnitType = hasUnitType;
                UnitTypeId = unitTypeId;
                ShipClass = shipClass;
                IsPlayerBase = isPlayerBase;
                IsUnit = isUnit;
                IsMiningFacility = isMiningFacility;
                HasUnits = hasUnits;
                Hull = hull;
                Shields = shields;
                OffenseScale = offenseScale;
                HangarSquadrons = hangarSquadrons;
            }
        }

        private static readonly Entry[] EMPTY_ENTRIES = System.Array.Empty<Entry>();
        private static readonly ProfilerMarker REBUILD_MARKER = new ProfilerMarker("EnemyAI.WorldSnapshot.Rebuild");

        private readonly IEntityLocator _entityLocator;
        private readonly List<Entry> _allLiving = new List<Entry>();
        private readonly List<Entry> _livingUnits = new List<Entry>();
        private readonly List<Entry> _structures = new List<Entry>();
        private readonly List<Entry> _raidStructures = new List<Entry>();
        private readonly List<Entry> _bases = new List<Entry>();
        private readonly Dictionary<PlayerId, List<Entry>> _unitsByOwner = new Dictionary<PlayerId, List<Entry>>();

        private int _builtFrame = -1;
        private int _lastEntityCount = -1;
        private bool _dirty = true;

        public AiWorldSnapshot(IEntityLocator entityLocator)
        {
            _entityLocator = entityLocator;
        }

        public IReadOnlyList<Entry> AllLiving
        {
            get
            {
                EnsureCurrent();
                return _allLiving;
            }
        }

        public IReadOnlyList<Entry> LivingUnits
        {
            get
            {
                EnsureCurrent();
                return _livingUnits;
            }
        }

        public IReadOnlyList<Entry> Structures
        {
            get
            {
                EnsureCurrent();
                return _structures;
            }
        }

        public IReadOnlyList<Entry> RaidStructures
        {
            get
            {
                EnsureCurrent();
                return _raidStructures;
            }
        }

        public IReadOnlyList<Entry> Bases
        {
            get
            {
                EnsureCurrent();
                return _bases;
            }
        }

        public void Initialize()
        {
            _entityLocator.EntityAdded += HandleEntityChanged;
            _entityLocator.EntityRemoved += HandleEntityChanged;
        }

        public void LateDispose()
        {
            _entityLocator.EntityAdded -= HandleEntityChanged;
            _entityLocator.EntityRemoved -= HandleEntityChanged;
        }

        public void MarkDirty() => _dirty = true;

        public IReadOnlyList<Entry> GetOwnedUnits(PlayerId owner)
        {
            EnsureCurrent();
            return _unitsByOwner.TryGetValue(owner, out List<Entry> list) ? list : EMPTY_ENTRIES;
        }

        public void EnsureCurrent()
        {
            int frame = Time.frameCount;
            int entityCount = _entityLocator.Entities.Count;
            if (!_dirty && _builtFrame == frame && _lastEntityCount == entityCount)
            {
                return;
            }

            _builtFrame = frame;
            _lastEntityCount = entityCount;
            _dirty = false;
            Rebuild();
        }

        private void HandleEntityChanged(GameEntity _) => _dirty = true;

        private void Rebuild()
        {
            using var sample = REBUILD_MARKER.Auto();
            _allLiving.Clear();
            _livingUnits.Clear();
            _structures.Clear();
            _raidStructures.Clear();
            _bases.Clear();
            foreach (List<Entry> list in _unitsByOwner.Values)
            {
                list.Clear();
            }

            foreach (GameEntity entity in _entityLocator.Entities)
            {
                IHealthModelObserver health = entity.HealthModel;
                if (health.IsDestroyed || !health.HasUnits)
                {
                    continue;
                }

                Vector3 position = entity.TryGetFacade(out IEntityTransformFacade transformFacade)
                    ? transformFacade.Transform.position
                    : Vector3.zero;
                bool hasUnitType = entity.TryGetFacade(out IUnitTypeFacade unitFacade);
                UnitTypeId unitTypeId = hasUnitType ? unitFacade.UnitTypeId : default;
                bool isUnit = entity.IsUnit();
                bool isPlayerBase = entity.IsPlayerBase();
                bool isMiningFacility = !isUnit && !isPlayerBase && entity.TryGetFacade(out IMiningFacilityFacade _);
                float offenseScale = hasUnitType ? ForceCompositionBuilder.GetOffenseScale(health) : 1f;
                int hangarSquadrons = hasUnitType && unitTypeId.IsShip && entity.TryGetFacade(out IHangarAirWingFacade airWing)
                    ? airWing.LaunchedCount + airWing.PendingLaunchCount
                    : 0;

                Entry entry = new Entry(
                    entity: entity,
                    position: position,
                    hasUnitType: hasUnitType,
                    unitTypeId: unitTypeId,
                    shipClass: health.ShipClass,
                    isPlayerBase: isPlayerBase,
                    isUnit: isUnit,
                    isMiningFacility: isMiningFacility,
                    hasUnits: health.HasUnits,
                    hull: health.Hull,
                    shields: health.Shields,
                    offenseScale: offenseScale,
                    hangarSquadrons: hangarSquadrons);

                _allLiving.Add(entry);

                if (entry.HasUnitType)
                {
                    _livingUnits.Add(entry);
                    if (!_unitsByOwner.TryGetValue(entry.Owner, out List<Entry> owned))
                    {
                        owned = new List<Entry>();
                        _unitsByOwner.Add(entry.Owner, owned);
                    }
                    owned.Add(entry);
                }

                if (entry.ShipClass == ShipClass.Structure)
                {
                    _structures.Add(entry);
                }

                if (!entry.IsUnit && !entry.IsPlayerBase)
                {
                    _raidStructures.Add(entry);
                }

                if (entry.IsPlayerBase)
                {
                    _bases.Add(entry);
                }
            }
        }
    }
}
