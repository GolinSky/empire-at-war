using System.Collections.Generic;
using EmpireAtWar.Components.Combat;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Components.Squadrons.Flight;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.Ship.Abilities;
using EmpireAtWar.Entities.Squadrons.Data;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Services.ShipAbilities;
using UnityEngine;

namespace EmpireAtWar.Entities.Squadrons.EntityFacades
{
    public sealed class SquadronAbilityFacade : IShipAbilityFacade
    {
        private readonly IEntityLocator _entities;
        private readonly ISquadronFlightComponent _flight;
        private readonly long _entityId;
        private readonly List<ShipAbilitySlot> _slots = new List<ShipAbilitySlot>();

        public IReadOnlyList<ShipAbilitySlot> Slots => _slots;
        public CombatModifiers Modifiers { get; }
        public Vector3 WorldPosition => _flight.Centroid;
        public IEntity Entity => _entities.GetEntity(_entityId);
        public IHealthModelObserver Health { get; }
        public float RadarRange { get; }

        public SquadronAbilityFacade(IHealthModelObserver health, IEntityLocator entities,
            IRadarModelObserver radar, ISquadronFlightComponent flight, SquadronData data,
            ShipAbilityCatalog catalog, CombatModifiers modifiers, long entityId)
        {
            Health = health;
            Modifiers = modifiers;
            RadarRange = radar.Range;
            _entities = entities;
            _flight = flight;
            _entityId = entityId;
            foreach (ShipAbilityId id in data.Abilities)
                _slots.Add(new ShipAbilitySlot(id: id, definition: catalog.Get(id), owner: this));
        }
    }
}
