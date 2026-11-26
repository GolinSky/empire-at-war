using System;
using System.Collections.Generic;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Services.Tooltip;

namespace EmpireAtWar.Entities.Tooltip
{
    /// <summary>The durability rows every unit tooltip shows, in one order and one format.</summary>
    public static class UnitStatRows
    {
        public const string HULL_LABEL = "Hull";
        public const string SHIELDS_LABEL = "Shields";
        public const string SHIELD_REGEN_LABEL = "Shield regen / s";
        private const string FORMAT = "#,0";
        private const string REGEN_FORMAT = "#,0.#";

        /// <summary>Menu rows: the base value plus the owner's researched bonus.</summary>
        public static void Add(List<TooltipStat> stats, UnitSheet sheet, IFactionResearchModelObserver research)
        {
            float hullBonus = ResearchBonus(sheet, sheet.Hull, ResearchStat.HullCapacity, research);
            float shieldBonus = ResearchBonus(sheet, sheet.Shields, ResearchStat.ShieldCapacity, research);
            stats.Add(new TooltipStat(HULL_LABEL, sheet.Hull, FORMAT, bonus: hullBonus));
            AddShields(stats, sheet, new TooltipStat(SHIELDS_LABEL, sheet.Shields, FORMAT, bonus: shieldBonus));
        }

        /// <summary>World rows: live value out of the base, plus whatever the live maximum adds to the base.</summary>
        public static void Add(List<TooltipStat> stats, UnitSheet sheet, IHealthTooltipObserver health)
        {
            stats.Add(new TooltipStat(HULL_LABEL, health.Hull, FORMAT, sheet.Hull, Round(health.MaxHull - sheet.Hull)));
            AddShields(stats, sheet,
                new TooltipStat(SHIELDS_LABEL, health.Shields, FORMAT, sheet.Shields, Round(health.MaxShields - sheet.Shields)));
        }

        private static void AddShields(List<TooltipStat> stats, UnitSheet sheet, TooltipStat shields)
        {
            if (sheet.Shields <= 0f) return;
            stats.Add(shields);
            stats.Add(new TooltipStat(SHIELD_REGEN_LABEL, sheet.ShieldRegenPerSecond, REGEN_FORMAT));
        }

        private static float ResearchBonus(UnitSheet sheet, float value, ResearchStat stat,
            IFactionResearchModelObserver research) =>
            sheet.TakesResearch ? Round(value * (research.GetMultiplier(stat, sheet.ShipClass) - 1f)) : 0f;

        private static float Round(float value) => (float)Math.Round(value);
    }
}
