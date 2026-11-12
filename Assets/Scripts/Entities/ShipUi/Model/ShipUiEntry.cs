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
        public Action Focus { get; }
        public IEntity Entity { get; }

        public ShipUiEntry(IReadOnlyList<ShipAbilitySlot> abilitySlots,
            IHealthModelObserver health, IEntity entity,
            Action focus)
        {
            AbilitySlots = abilitySlots;
            Health = health;
            Focus = focus;
            Entity = entity;
        }
    }
}
