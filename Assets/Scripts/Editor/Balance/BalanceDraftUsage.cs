using System.Collections.Generic;
using System.Linq;

namespace EmpireAtWar.Editor.Balance
{
    public sealed class BalanceDraftUsage
    {
        private readonly BalanceRegistration _registry;
        private readonly BalanceDraft _draft;
        private Dictionary<string, List<BalanceUnit>> _weaponUsers;
        private Dictionary<string, List<BalanceUnit>> _abilityUsers;
        private IReadOnlyList<int> _weaponIds;

        public IReadOnlyList<int> WeaponIds => _weaponIds ??= _registry.Fields.Values
            .Where(field => field.Stat.StartsWith("weapon/") && field.Stat.EndsWith("/" + BalanceWeaponAdapter.DEFINITION_MARKER))
            .Select(field => int.Parse(field.Stat.Split('/')[1])).Distinct().OrderBy(id => id).ToArray();

        private Dictionary<string, List<BalanceUnit>> WeaponUsers => _weaponUsers ??= _registry.Fields.Values
            .Where(field => !field.SharedMountSource && field.Stat == "WeaponType")
            .GroupBy(field => field.DraftValue(_draft)).ToDictionary(group => group.Key,
                group => group.SelectMany(field => field.Users).Distinct().ToList());

        private Dictionary<string, List<BalanceUnit>> AbilityUsers => _abilityUsers ??= _registry.Units
            .Where(unit => unit.Fields.ContainsKey("Abilities"))
            .SelectMany(unit => _registry.Fields[unit.Fields["Abilities"]].DraftValue(_draft).Split(',')
                .Where(id => id.Length != 0).Select(id => (Id: id, Unit: unit)))
            .GroupBy(entry => entry.Id).ToDictionary(group => group.Key, group => group.Select(entry => entry.Unit).Distinct().ToList());

        public BalanceDraftUsage(BalanceRegistration registry, BalanceDraft draft)
        {
            _registry = registry;
            _draft = draft;
        }

        public IReadOnlyList<BalanceUnit> Users(BalanceField field)
        {
            if (!IsProfile(field)) return field.Users;
            string[] parts = field.Stat.Split('/');
            var lookup = parts[0] == "weapon" ? WeaponUsers : AbilityUsers;
            string ids = parts[1];
            if (!lookup.TryGetValue(ids, out List<BalanceUnit> users))
            {
                users = ids.Split('+').Where(lookup.ContainsKey).SelectMany(id => lookup[id]).Distinct().ToList();
                lookup.Add(ids, users);
            }
            return users;
        }

        public Dictionary<string, string> Fields(BalanceUnit unit)
        {
            var fields = unit.Fields.Where(entry => !IsProfile(_registry.Fields[entry.Value]) && !_registry.Fields[entry.Value].SharedMountSource)
                .ToDictionary(entry => entry.Key, entry => entry.Value);
            foreach (BalanceField field in _registry.Fields.Values.Where(IsProfile))
                if (Users(field).Contains(unit)) fields[field.Stat] = field.Key;
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
