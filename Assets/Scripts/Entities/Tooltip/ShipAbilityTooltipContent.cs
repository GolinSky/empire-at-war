using System;
using System.Collections.Generic;
using EmpireAtWar.Entities.Ship.Abilities;
using EmpireAtWar.Services.Input;
using EmpireAtWar.Services.Tooltip;

namespace EmpireAtWar.Entities.Tooltip
{
    public static class ShipAbilityTooltipContent
    {
        public static TooltipContent Build(IReadOnlyList<ShipAbilitySlot> slots, IInputBindings bindings,
            TooltipIconData icons)
        {
            var definition = slots[0].Definition;
            int ready = 0;
            int active = 0;
            float remaining = 0f;
            foreach (ShipAbilitySlot slot in slots)
            {
                if (slot.State == ShipAbilityState.Ready) ready++;
                if (slot.State == ShipAbilityState.Active) active++;
                remaining = Math.Max(remaining, slot.TimeLeft);
            }
            return new TooltipContent(definition.DisplayName, definition.Description, icons.Register(definition.Icon),
                stats: new[]
                {
                    new TooltipStat("Range", definition.Range),
                    new TooltipStat("Duration (s)", definition.Duration),
                    new TooltipStat("Cooldown (s)", definition.RecoveryDelay),
                    new TooltipStat("Remaining (s)", remaining)
                }, status: $"{ready} of {slots.Count} ready · {active} active. " +
                    (definition.RequiresEnemyTarget ? "Choose an enemy target. " : "Applies to eligible selected units. ") +
                    (definition.CanCancel ? $"{(active > 0 ? "On" : "Off")}: click while active to switch off. " : "") +
                    $"Cancel targeting: {TooltipBindings.Get(bindings, "Ui", "Cancel")}");
        }
    }
}
