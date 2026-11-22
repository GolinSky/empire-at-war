using System.Collections.Generic;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Services.Tooltip;

namespace EmpireAtWar.Entities.Tooltip
{
    public static class HealthTooltipStats
    {
        public static void AddHullAndShields(List<TooltipStat> stats, IHealthTooltipObserver health)
        {
            stats.Add(new TooltipStat(label: "Hull", current: health.Hull, format: "0", max: health.MaxHull));
            if (health.MaxShields > 0f)
                stats.Add(new TooltipStat(label: "Shields", current: health.Shields, format: "0", max: health.MaxShields));
        }
    }
}
