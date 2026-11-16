using System.Collections.Generic;
using EmpireAtWar.Components.Hangar;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.MiningFacility;
using EmpireAtWar.Entities.SpaceStation;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Players;
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
            public readonly float NavigationRadius;
            public readonly bool HasUnitType;
            public readonly UnitTypeId UnitTypeId;
            public readonly ShipClass ShipClass;
            public readonly bool IsPlayerBase;
            public readonly bool IsUnit;
            public readonly bool IsSecondStation;
            public readonly bool IsMiningFacility;
            public readonly bool IsCloaked;
            public readonly bool HasUnits;
            public readonly float Hull;
            public readonly float Shields;
            public readonly float OffenseScale;
            public readonly int HangarSquadrons;

            public Entry(
                GameEntity entity,
                Vector3 position,
                float navigationRadius,
                bool hasUnitType,
                UnitTypeId unitTypeId,
                ShipClass shipClass,
                bool isPlayerBase,
                bool isUnit,
                bool isSecondStation,
                bool isMiningFacility,
                bool isCloaked,
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
                NavigationRadius = navigationRadius;
                HasUnitType = hasUnitType;
                UnitTypeId = unitTypeId;
                ShipClass = shipClass;
                IsPlayerBase = isPlayerBase;
                IsUnit = isUnit;
                IsSecondStation = isSecondStation;
                IsMiningFacility = isMiningFacility;
                IsCloaked = isCloaked;
                HasUnits = hasUnits;
                Hull = hull;
                Shields = shields;
                OffenseScale = offenseScale;
                HangarSquadrons = hangarSquadrons;
            }
        }

        private static readonly Entry[] EMPTY_ENTRIES = System.Array.Empty<Entry>();

        private readonly IEntityLocator _entityLocator;
        private readonly List<Entry> _allLiving = new List<Entry>();
        private readonly List<Entry> _livingUnits = new List<Entry>();
        private readonly List<Entry> _structures = new List<Entry>();
        private readonly List<Entry> _bases = new List<Entry>();
        private readonly Dictionary<PlayerId, List<Entry>> _unitsByOwner = new Dictionary<PlayerId, List<Entry>>();

        private int _builtFrame = -1;
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
            if (!_dirty && _builtFrame == frame)
            {
                return;
            }

            _builtFrame = frame;
            _dirty = false;
            Rebuild();
        }

        private void HandleEntityChanged(GameEntity _) => _dirty = true;

        private void Rebuild()
        {
            _allLiving.Clear();
            _livingUnits.Clear();
            _structures.Clear();
            _bases.Clear();
            foreach (List<Entry> list in _unitsByOwner.Values)
            {
                list.Clear();
            }

            foreach (GameEntity entity in _entityLocator.Entities)
            {
                IHealthModelObserver health = entity.HealthModel;
                if (health.IsDestroyed)
                {
                    continue;
                }

                Vector3 position = entity.TryGetFacade(out IEntityTransformFacade transformFacade)
                    ? transformFacade.Transform.position
                    : Vector3.zero;
                float navigationRadius = entity.TryGetFacade(out INavigationFootprintFacade footprint)
                    ? footprint.NavigationRadius
                    : 0f;
                bool hasUnitType = entity.TryGetFacade(out IUnitTypeFacade unitFacade);
                UnitTypeId unitTypeId = hasUnitType ? unitFacade.UnitTypeId : default;
                int hangarSquadrons = entity.TryGetFacade(out IHangarAirWingFacade airWing)
                    ? airWing.LaunchedCount + airWing.PendingLaunchCount
                    : 0;

                Entry entry = new Entry(
                    entity: entity,
                    position: position,
                    navigationRadius: navigationRadius,
                    hasUnitType: hasUnitType,
                    unitTypeId: unitTypeId,
                    shipClass: health.ShipClass,
                    isPlayerBase: entity.IsPlayerBase(),
                    isUnit: entity.IsUnit(),
                    isSecondStation: entity.TryGetFacade(out ISecondStationFacade _),
                    isMiningFacility: entity.TryGetFacade(out IMiningFacilityFacade _),
                    isCloaked: entity.IsCloaked(),
                    hasUnits: health.HasUnits,
                    hull: health.Hull,
                    shields: health.Shields,
                    offenseScale: ForceCompositionBuilder.GetOffenseScale(health),
                    hangarSquadrons: hangarSquadrons);

                _allLiving.Add(entry);
                if (!entry.HasUnits)
                {
                    continue;
                }

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

                if (entry.IsPlayerBase)
                {
                    _bases.Add(entry);
                }
            }
        }
    }
}
