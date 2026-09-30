using System;
using System.Collections.Generic;
using EmpireAtWar.Entities.Ship.Abilities;
using EmpireAtWar.Services.ShipAbilities;
using EmpireAtWar.Models.Health;

namespace EmpireAtWar.Models.ShipUi
{
    public sealed class ShipUiEntry
    {
        public IReadOnlyList<ShipAbilitySlot> AbilitySlots { get; }
        public Action<ShipAbilityId> PressAbility { get; }
        public IHealthModelObserver Health { get; }
        public Action Focus { get; }
        public EmpireAtWar.Entities.BaseEntity.IEntity Entity { get; }

        public ShipUiEntry(IReadOnlyList<ShipAbilitySlot> abilitySlots,
            Action<ShipAbilityId> pressAbility, IHealthModelObserver health, Action focus,
            EmpireAtWar.Entities.BaseEntity.IEntity entity)
        {
            AbilitySlots = abilitySlots;
            PressAbility = pressAbility;
            Health = health;
            Focus = focus;
            Entity = entity;
        }
    }
}
