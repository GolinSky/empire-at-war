using System.Collections.Generic;
using EmpireAtWar.Components.Combat;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.Ship.Data;
using EmpireAtWar.Extentions;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Services.ShipAbilities;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Entities.Ship.Abilities
{
    public sealed class ShipAbilityFacade : IShipAbilityFacade
    {
        private readonly List<ShipAbilitySlot> _slots = new List<ShipAbilitySlot>();
        private readonly IEntityLocator _entities;
        private readonly Transform _viewTransform;
        private readonly long _entityId;

        public IReadOnlyList<ShipAbilitySlot> Slots => _slots;
        public CombatModifiers Modifiers { get; }
        public Vector3 WorldPosition => _viewTransform.position;
        public IEntity Entity => _entities.GetEntity(_entityId);
        public IHealthModelObserver Health { get; }
        public float RadarRange { get; }

        public ShipAbilityFacade(ShipData data, ShipAbilityCatalog catalog,
            CombatModifiers modifiers, IHealthModelObserver health,
            [Inject(Id = EntityBindType.ViewTransform)] Transform viewTransform,
            IEntityLocator entities, IRadarModelObserver radar, long entityId)
        {
            Modifiers = modifiers;
            Health = health;
            RadarRange = radar.Range;
            _viewTransform = viewTransform;
            _entities = entities;
            _entityId = entityId;
            foreach (ShipAbilityId id in data.Abilities)
                _slots.Add(new ShipAbilitySlot(id, catalog.Get(id), this));
        }
    }
}
