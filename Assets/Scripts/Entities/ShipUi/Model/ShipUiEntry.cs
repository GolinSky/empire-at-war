using System;
using System.Collections.Generic;
using EmpireAtWar.Entities.Ship.Abilities;
using EmpireAtWar.Services.ShipAbilities;

namespace EmpireAtWar.Models.ShipUi
{
    public sealed class ShipUiEntry
    {
        public IReadOnlyList<ShipAbilitySlot> AbilitySlots { get; }
        public Action<ShipAbilityId> PressAbility { get; }

        public ShipUiEntry(IReadOnlyList<ShipAbilitySlot> abilitySlots, Action<ShipAbilityId> pressAbility)
        {
            AbilitySlots = abilitySlots;
            PressAbility = pressAbility;
        }
    }
}
