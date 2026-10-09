using System.Collections.Generic;
using System.Linq;

namespace EmpireAtWar.Editor.Balance
{
    public sealed class BalanceDraftUsage
    {
        private readonly BalanceRegistration _registry;
        private readonly Dictionary<string, List<BalanceUnit>> _profileUsers = new Dictionary<string, List<BalanceUnit>>();
        public IReadOnlyList<int> WeaponIds { get; }

        public BalanceDraftUsage(BalanceRegistration registry, BalanceDraft draft)
        {
            _registry = registry;
            WeaponIds = registry.Fields.Values.Where(field => field.Stat.StartsWith("weapon/") && field.Stat.EndsWith("/damage"))
                .Select(field => int.Parse(field.Stat.Split('/')[1])).Distinct().OrderBy(id => id).ToArray();
            var weaponUsers = registry.Fields.Values.Where(field => !field.SharedMountSource && field.Stat == "WeaponType")
                .GroupBy(field => field.DraftValue(draft)).ToDictionary(group => group.Key,
                    group => group.SelectMany(field => field.Users).Distinct().ToList());
            var abilityUsers = registry.Units.Where(unit => unit.Fields.ContainsKey("Abilities"))
                .SelectMany(unit => registry.Fields[unit.Fields["Abilities"]].DraftValue(draft).Split(',')
                    .Where(id => id.Length != 0).Select(id => (Id: id, Unit: unit)))
                .GroupBy(entry => entry.Id).ToDictionary(group => group.Key, group => group.Select(entry => entry.Unit).Distinct().ToList());
            foreach (BalanceField field in registry.Fields.Values.Where(IsProfile))
            {
                string[] parts = field.Stat.Split('/');
                var lookup = parts[0] == "weapon" ? weaponUsers : abilityUsers;
                _profileUsers[field.Key] = parts[1].Split('+').Where(lookup.ContainsKey).SelectMany(id => lookup[id]).Distinct().ToList();
            }
        }

        public IReadOnlyList<BalanceUnit> Users(BalanceField field) =>
            _profileUsers.TryGetValue(field.Key, out List<BalanceUnit> users) ? users : field.Users;

        public Dictionary<string, string> Fields(BalanceUnit unit)
        {
            var fields = unit.Fields.Where(entry => !IsProfile(_registry.Fields[entry.Value]) && !_registry.Fields[entry.Value].SharedMountSource)
                .ToDictionary(entry => entry.Key, entry => entry.Value);
            foreach (var entry in _profileUsers.Where(entry => entry.Value.Contains(unit)))
                fields[_registry.Fields[entry.Key].Stat] = entry.Key;
            return fields;
        }

        public string Validate(BalanceField field, string value)
        {
            string error = field.Validate(value);
            if (error.Length != 0) return error;
            return field.Stat == "WeaponType" && !WeaponIds.Contains(int.Parse(value))
                ? "Missing weapon profile: " + BalanceFieldView.Display(field, value) : "";
        }

        private static bool IsProfile(BalanceField field) => field.Stat.StartsWith("weapon/")
            || field.Stat.StartsWith("ability/") || field.Stat.StartsWith("settings/");
    }
}
