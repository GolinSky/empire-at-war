using System;
using System.Collections.Generic;
using EmpireAtWar.Entities.Ship.Abilities;
using EmpireAtWar.Services.ShipAbilities;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Entities.BaseEntity;

namespace EmpireAtWar.Models.ShipUi
{
    public sealed class ShipUiEntry
    {
        public IReadOnlyList<ShipAbilitySlot> AbilitySlots { get; }
        public IHealthModelObserver Health { get; }
        public IEntity Entity { get; }

        public ShipUiEntry(IReadOnlyList<ShipAbilitySlot> abilitySlots,
            IHealthModelObserver health, IEntity entity)
        {
            AbilitySlots = abilitySlots;
            Health = health;
            Entity = entity;
        }
    }
}
