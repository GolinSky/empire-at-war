using System.Collections.Generic;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Services.Tooltip;

namespace EmpireAtWar.Entities.Tooltip
{
    public static class HealthTooltipStats
    {
        public static void AddHullAndShields(List<TooltipStat> stats, IHealthTooltipObserver health)
        {
            stats.Add(new TooltipStat(label: UnitStatRows.HULL_LABEL, current: health.Hull, format: "0", max: health.MaxHull));
            if (health.MaxShields <= 0f) return;
            stats.Add(new TooltipStat(label: UnitStatRows.SHIELDS_LABEL, current: health.Shields, format: "0", max: health.MaxShields));
            if (health.ShieldRegeneration > 0f)
                stats.Add(new TooltipStat(label: UnitStatRows.SHIELD_REGEN_LABEL,
                    current: health.ShieldRegeneration / health.ShieldRegenerationInterval, format: "#,0.#"));
        }
    }
}
