using System.Collections.Generic;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Mvc;
using UnityEngine;

namespace EmpireAtWar.Components.AttackComponent
{
    /// <summary>
    /// Single source of weapon balance: family, caliber and range-class rules plus one definition per weapon.
    /// Profiles are derived on first use, never stored.
    /// </summary>
    [CreateAssetMenu(fileName = nameof(WeaponsData), menuName = "Data/Weapon/WeaponsData")]
    public class WeaponsData : Data
    {
        [Header("Rules")]
        [SerializeField] private List<WeaponFamilyRule> families = new List<WeaponFamilyRule>();
        [SerializeField] private List<WeaponCaliberRule> calibers = new List<WeaponCaliberRule>();
        [SerializeField] private List<WeaponRangeClassRule> rangeClasses = new List<WeaponRangeClassRule>();
        [Tooltip("Bolt color of faction-colored families for each faction.")]
        [SerializeField] private List<FactionBoltColor> factionColors = new List<FactionBoltColor>();

        [Header("Weapons")]
        [SerializeField] private List<WeaponDefinition> weapons = new List<WeaponDefinition>();

        private Dictionary<WeaponType, WeaponProfile> _profiles;
        private Dictionary<(WeaponType, FactionType), WeaponProfile> _factionProfiles;

        public IReadOnlyList<WeaponFamilyRule> Families => families;
        public IReadOnlyList<WeaponCaliberRule> Calibers => calibers;
        public IReadOnlyList<WeaponRangeClassRule> RangeClasses => rangeClasses;
        public IReadOnlyList<WeaponDefinition> Weapons => weapons;

        public IReadOnlyList<FactionBoltColor> FactionColors => factionColors;

        public WeaponProfile GetProfile(WeaponType weaponType)
        {
            if (_profiles == null) BuildProfiles();
            return _profiles[weaponType];
        }

        /// <summary>The profile as the given faction fires it: faction-colored families take that faction's bolt color.</summary>
        public WeaponProfile GetProfile(WeaponType weaponType, FactionType faction)
        {
            WeaponProfile profile = GetProfile(weaponType);
            if (!profile.UsesFactionColor) return profile;

            _factionProfiles ??= new Dictionary<(WeaponType, FactionType), WeaponProfile>();
            if (!_factionProfiles.TryGetValue((weaponType, faction), out WeaponProfile colored))
            {
                colored = profile.WithColor(GetFactionColor(faction));
                _factionProfiles.Add((weaponType, faction), colored);
            }

            return colored;
        }

        public Color GetFactionColor(FactionType faction)
        {
            foreach (FactionBoltColor entry in factionColors)
                if (entry.Faction == faction) return entry.Color;
            throw new KeyNotFoundException($"No bolt color for faction {faction}.");
        }

        public WeaponDefinition GetDefinition(WeaponType weaponType)
        {
            foreach (WeaponDefinition definition in weapons)
                if (definition.WeaponType == weaponType) return definition;
            throw new KeyNotFoundException($"No weapon definition for {weaponType}.");
        }

        public WeaponFamilyRule GetFamily(WeaponFamily family)
        {
            foreach (WeaponFamilyRule rule in families)
                if (rule.Family == family) return rule;
            throw new KeyNotFoundException($"No weapon family rule for {family}.");
        }

        public WeaponCaliberRule GetCaliber(WeaponCaliber caliber)
        {
            foreach (WeaponCaliberRule rule in calibers)
                if (rule.Caliber == caliber) return rule;
            throw new KeyNotFoundException($"No weapon caliber rule for {caliber}.");
        }

        public WeaponRangeClassRule GetRangeClass(WeaponRangeClass rangeClass)
        {
            foreach (WeaponRangeClassRule rule in rangeClasses)
                if (rule.RangeClass == rangeClass) return rule;
            throw new KeyNotFoundException($"No weapon range class rule for {rangeClass}.");
        }

        private void BuildProfiles()
        {
            _profiles = new Dictionary<WeaponType, WeaponProfile>();
            foreach (WeaponDefinition definition in weapons)
                _profiles.Add(definition.WeaponType, WeaponProfileFactory.Create(definition,
                    GetFamily(definition.Family), GetCaliber(definition.Caliber), GetRangeClass(definition.RangeClass)));
        }

        // Inspector and balance-editor edits rebuild the derived profiles.
        private void OnValidate()
        {
            _profiles = null;
            _factionProfiles = null;
        }
    }
}
