using System;
using System.Collections.Generic;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.Units.Matchups;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Services.Tooltip;

namespace EmpireAtWar.Entities.Tooltip
{
    public static class UnitTooltipContent
    {
        private const string SEPARATOR = " · ";

        /// <summary>Roster items that are not units (research, station levels, super weapons, structures).</summary>
        public static TooltipContent Build(FactionData data, IEnumerable<TooltipStat> stats,
            IEnumerable<TooltipRequirement> requirements = null, string status = "") =>
            new TooltipContent(title: data.Name, description: data.Description, iconKey: data.IconKey, subtitle: data.Role,
                stats: stats, requirements: requirements, status: status, populationCost: data.UnitCapacity);

        /// <summary>
        /// Ships and squadrons: class and role tags as the subtitle, the short role text as the description, and
        /// strong/weak rows from <see cref="MatchupRules"/>. Numbers come from <paramref name="stats"/> only.
        /// </summary>
        public static TooltipContent Build(UnitSheet sheet, MatchupRules matchups, IEnumerable<TooltipStat> stats,
            IEnumerable<TooltipRequirement> requirements = null, string status = "")
        {
            var strong = new List<TooltipIcon>();
            var weak = new List<TooltipIcon>();
            UnitMatchupResolver.Resolve(matchups, sheet.ShipClass, sheet.Roles, sheet.Faction, strong, weak);
            FactionData data = sheet.FactionData;
            return new TooltipContent(title: data.Name, description: data.Role, iconKey: data.IconKey,
                subtitle: Subtitle(sheet), stats: stats, strongAgainst: strong, weakAgainst: weak,
                requirements: requirements, status: status, populationCost: data.UnitCapacity);
        }

        private static string Subtitle(UnitSheet sheet)
        {
            var parts = new List<string> { ClassLabel(sheet.ShipClass) };
            foreach (UnitRole role in Enum.GetValues(typeof(UnitRole)))
                if (role != UnitRole.None && (sheet.Roles & role) == role) parts.Add(RoleLabel(role));
            return string.Join(SEPARATOR, parts);
        }

        private static string ClassLabel(ShipClass shipClass) => shipClass switch
        {
            ShipClass.HeroStrikecraft => "Hero strikecraft",
            ShipClass.HeavyCruiser => "Heavy cruiser",
            ShipClass.BattleCruiser => "Battlecruiser",
            _ => shipClass.ToString()
        };

        private static string RoleLabel(UnitRole role) => role switch
        {
            UnitRole.Dps => "DPS",
            UnitRole.MissileSpecialist => "Missile specialist",
            UnitRole.IonSpecialist => "Ion specialist",
            UnitRole.AntiStrikecraft => "Anti-strikecraft",
            UnitRole.AntiCapital => "Anti-capital",
            _ => role.ToString()
        };
    }
}
