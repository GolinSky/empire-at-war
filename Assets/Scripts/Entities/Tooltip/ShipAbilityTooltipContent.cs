using System;
using System.Collections.Generic;
using EmpireAtWar.Entities.Ship.Abilities;
using EmpireAtWar.Services.Input;
using EmpireAtWar.Services.ShipAbilities;
using EmpireAtWar.Services.ShipAbilities.Abilities;
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
            int jammed = 0;
            int shieldsOffline = 0;
            int ionDisabled = 0;
            foreach (ShipAbilitySlot slot in slots)
            {
                if (slot.CanActivate) ready++;
                if (slot.State == ShipAbilityState.Active) active++;
                float timeLeft = slot.RunningAbility is IShipAbilityRemainingTime timed ? timed.RemainingTime : slot.TimeLeft;
                remaining = Math.Max(remaining, timeLeft);
                var modifiers = slot.Owner.Modifiers;
                if (modifiers.IsIonDisabled) ionDisabled++;
                if (modifiers.IsAbilitySuppressed)
                {
                    if (modifiers.IsShieldSuppressed) shieldsOffline++;
                    else jammed++;
                }
            }

            var stats = new List<TooltipStat>();
            if (definition.RequiresEnemyTarget)
                stats.Add(new TooltipStat("Target range", definition.Range));

            float effectRadius = definition.Settings switch
            {
                ConcentrateFireSettings concentrate => concentrate.CommandRadius,
                FighterCoordinationSettings coordination => coordination.CommandRadius,
                GravityWellSettings or SensorJammingSettings => definition.Range,
                HarmonicBombSettings bomb => bomb.Radius,
                _ => 0f
            };
            if (effectRadius > 0f) stats.Add(new TooltipStat("Effect radius", effectRadius));

            float duration = definition.Settings.GetEffectDuration(definition.Duration);
            if (definition.Settings is LuckyShotSettings luckyShot)
                stats.Add(new TooltipStat("Windup (s)", luckyShot.Windup, format: "0.##"));
            else if (definition.Settings is IonPulseSettings or IonShotSettings)
                stats.Add(new TooltipStat("Disable duration (s)", duration));
            else if (duration > 0f)
                stats.Add(new TooltipStat("Duration (s)", duration));
            if (definition.RecoveryDelay > 0f) stats.Add(new TooltipStat("Cooldown (s)", definition.RecoveryDelay));
            if (remaining > 0f) stats.Add(new TooltipStat("Remaining (s)", remaining));

            return new TooltipContent(title: definition.DisplayName, description: definition.Description, iconKey: icons.Register(definition.Icon),
                stats: stats,
                status: (ionDisabled > 0 ? $"Ion-disabled: {ionDisabled}. " : "") +
                    (shieldsOffline > 0 ? $"Shields and abilities disabled by hazards: {shieldsOffline}. " : "") +
                    (jammed > 0 ? $"Abilities disabled by hazards: {jammed}. " : "") +
                    $"{ready} of {slots.Count} ready · {active} active. " +
                    (definition.CanCancel && active > 0 ? "Select the unit on its own and click again to end the effect. " : "") +
                    (definition.RequiresEnemyTarget
                        ? $"Choose an enemy target with {TooltipBindings.Get(bindings, "Battle", "Command")}. " +
                          $"Cancel with {TooltipBindings.Get(bindings, "Ui", "Cancel")}."
                        : ""));
        }
    }
}
