using System;
using System.Collections.Generic;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Services.Tooltip;

namespace EmpireAtWar.Entities.Faction.Ui
{
    internal static class ResearchTooltipContent
    {
        private const string PERCENT_FORMAT = "+0.#%;-0.#%;0%";

        public static TooltipContent BuildSummary(FactionRoster roster, IFactionResearchModelObserver research)
        {
            var stats = new List<TooltipStat>();
            foreach (ResearchType type in research.ResearchTypes)
            {
                int count = research.GetCompletedTiers(type);
                if (count == 0) continue;

                ResearchTierData active = roster.ResearchLines[type].Tiers[count - 1];
                foreach (ResearchEffect effect in active.Effects)
                    stats.Add(new TooltipStat($"{active.FactionData.DisplayName} · {GetStatLabel(effect.Stat)}",
                        effect.Multiplier - 1f, PERCENT_FORMAT));
            }

            return new TooltipContent("Active research", stats: stats,
                description: stats.Count == 0 ? "No research completed yet." : "",
                status: "Open Research to inspect each upgrade.");
        }

        public static TooltipContent BuildLine(FactionRoster roster, IFactionResearchModelObserver research,
            ResearchType type)
        {
            ResearchTierData[] tiers = roster.ResearchLines[type].Tiers;
            int count = research.GetCompletedTiers(type);
            ResearchTierData tier = tiers[Math.Max(0, count - 1)];
            var stats = new List<TooltipStat>();
            var scope = new List<string>();
            foreach (ResearchEffect effect in tier.Effects)
            {
                string label = GetStatLabel(effect.Stat);
                stats.Add(new TooltipStat(label, effect.Multiplier - 1f, PERCENT_FORMAT));
                scope.Add(effect.Stat switch
                {
                    ResearchStat.Income => "Applies to income from facilities you own.",
                    ResearchStat.StationIncome => "Applies to your base and station income.",
                    _ => $"{label} applies to: {string.Join(", ", effect.ShipClasses)}."
                });
            }

            string status = count == tiers.Length
                ? "Highest tier completed."
                : $"Next tier requires station level {tiers[count].FactionData.AvailableLevel}.";
            return new TooltipContent(tier.FactionData.Name, stats: stats, description: scope.Count == 0 ? tier.FactionData.Description : string.Join("\n", scope),
                iconKey: tier.FactionData.IconKey, subtitle: count == 0 ? "Unresearched · Tier I preview" : "Active upgrade",
                status: status);
        }

        private static string GetStatLabel(ResearchStat stat) => stat switch
        {
            ResearchStat.Income => "Facility income",
            ResearchStat.StationIncome => "Base and station income",
            ResearchStat.WeaponDamage => "Weapon damage",
            ResearchStat.Speed => "Speed",
            ResearchStat.DamageTaken => "Damage taken",
            ResearchStat.ShieldCapacity => "Shield capacity",
            ResearchStat.HullCapacity => "Hull capacity",
            _ => throw new ArgumentOutOfRangeException(nameof(stat))
        };
    }
}
