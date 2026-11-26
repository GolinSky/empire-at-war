using System.Collections.Generic;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.Units.Matchups;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Services.Tooltip;

namespace EmpireAtWar.Entities.Tooltip
{
    /// <summary>
    /// Turns <see cref="MatchupRules"/> into a unit's strong/weak tooltip rows. Strong = targets of rules the unit
    /// attacks in; weak = attackers of rules the unit is a target of. A class-to-class rule inside the unit's own group
    /// ("cruisers beat cruisers") is left out.
    /// </summary>
    public static class UnitMatchupResolver
    {
        public static void Resolve(MatchupRules rules, ShipClass shipClass, UnitRole roles, FactionType ownFaction,
            List<TooltipIcon> strong, List<TooltipIcon> weak)
        {
            MatchupGroup ownGroup = FindClassGroup(rules, shipClass);
            var strongGroups = new HashSet<MatchupGroup>();
            var weakGroups = new HashSet<MatchupGroup>();
            foreach (MatchupRule rule in rules.Rules)
            {
                if (rule.Attacker.Matches(shipClass, roles))
                    Collect(rules, rule.Target, ownGroup, rule.Attacker.HasClass(shipClass), strongGroups);
                if (rule.Target.Matches(shipClass, roles))
                    Collect(rules, rule.Attacker, ownGroup, rule.Target.HasClass(shipClass), weakGroups);
            }

            foreach (MatchupGroup group in rules.Groups)
            {
                if (strongGroups.Contains(group)) strong.Add(ToIcon(group, ownFaction));
                if (weakGroups.Contains(group)) weak.Add(ToIcon(group, ownFaction));
            }
        }

        private static void Collect(MatchupRules rules, MatchupSide side, MatchupGroup ownGroup, bool matchedByClass,
            HashSet<MatchupGroup> result)
        {
            foreach (MatchupGroup group in rules.Groups)
            {
                if (matchedByClass && group == ownGroup) continue;
                if (Covers(group.Members, side)) result.Add(group);
            }
        }

        private static bool Covers(MatchupSide group, MatchupSide side)
        {
            foreach (ShipClass shipClass in side.Classes)
                if (group.HasClass(shipClass)) return true;
            return side.Roles != UnitRole.None && group.HasRole(side.Roles);
        }

        private static MatchupGroup FindClassGroup(MatchupRules rules, ShipClass shipClass)
        {
            foreach (MatchupGroup group in rules.Groups)
                if (group.Members.HasClass(shipClass)) return group;
            return null;
        }

        private static TooltipIcon ToIcon(MatchupGroup group, FactionType ownFaction)
        {
            var keys = new List<string>();
            if (!string.IsNullOrEmpty(group.NeutralIconKey)) keys.Add(group.NeutralIconKey);
            foreach (MatchupIcon icon in group.Icons)
                if (icon.Faction != ownFaction) keys.Add(icon.IconKey);
            return new TooltipIcon(keys, group.Label);
        }
    }
}
