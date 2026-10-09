using System.Linq;
using EmpireAtWar.Editor.Balance;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.ViewComponents.Health;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class BalanceInventoryTests
    {
        [Test]
        public void CurrentRoster_AllEntriesResolveToDataPrefabsAndApprovedFields()
        {
            BalanceRegistration inventory = BalanceInventory.Build();
            Assert.That(inventory.Errors, Is.Empty);
            FactionCatalog catalog = BalanceRegistration.One<FactionCatalog>();
            Assert.That(inventory.Units.Count(unit => unit.Kind == BalanceUnitKind.Ship), Is.EqualTo(catalog.Factions.Sum(faction => faction.Ships.Count)));
            Assert.That(inventory.Units.Count(unit => unit.Kind == BalanceUnitKind.Squadron), Is.EqualTo(catalog.Factions.Sum(faction => faction.Squadrons.Count)));
            Assert.That(inventory.Units.Where(unit => unit.Kind == BalanceUnitKind.Ship || unit.Kind == BalanceUnitKind.Squadron).All(unit => unit.Data != null && unit.Prefab != null && unit.Fields.ContainsKey("Price")), Is.True);
            Assert.That(inventory.Fields.Values.Any(field => field.Path.Contains("HullBottom") || field.Path.Contains("HullTop") || field.Path.Contains("MemberCount") || field.Path == "weaponLoadout"), Is.False);
            Assert.That(inventory.Fields.Values.All(field => !field.Key.Contains("Array.data") && !field.Key.Contains("rid:")), Is.True);
        }

        [Test]
        public void TargetLookup_PreservesAllRegisteredFieldsWithoutDuplicates()
        {
            BalanceRegistration inventory = BalanceInventory.Build();
            Assert.That(inventory.Errors, Is.Empty);
            foreach (var group in inventory.Fields.Values.GroupBy(field => field.Target))
                Assert.That(inventory.FieldsFor(group.Key), Is.EquivalentTo(group), group.Key.name);
            foreach (BalanceUnit unit in inventory.Units)
                foreach (var mount in unit.Mounts.Distinct())
                    foreach (BalanceField field in inventory.FieldsFor(mount))
                        Assert.That(unit.Fields["mount/" + field.Key], Is.EqualTo(field.Key));
        }

        [Test]
        public void WeaponConsumers_IncludeEveryBoundShipSquadronAndStructure()
        {
            BalanceRegistration inventory = BalanceInventory.Build();
            Assert.That(inventory.Errors, Is.Empty);
            foreach (BalanceField field in inventory.Fields.Values.Where(field => field.Stat.StartsWith("weapon/") && field.Stat.EndsWith("/damage")))
            {
                int id = int.Parse(field.Stat.Split('/')[1]);
                var expected = inventory.Units.Where(unit => unit.Mounts.OfType<WeaponHardPoint>().Any(mount => (int)mount.WeaponType == id)).Select(unit => unit.Id).Distinct().OrderBy(value => value);
                Assert.That(field.Users.Select(unit => unit.Id).OrderBy(value => value), Is.EqualTo(expected), field.Context);
            }
        }

        [Test]
        public void ManagedReferenceAliases_ProduceOneSettingsTargetInEveryCompareCell()
        {
            BalanceRegistration inventory = BalanceInventory.Build();
            Assert.That(inventory.Errors, Is.Empty);
            var aliases = inventory.Fields.Values.Where(field => field.AliasWarning.Contains("Laser Beam") && field.AliasWarning.Contains("Proton Beam")).ToList();
            Assert.That(aliases, Is.Not.Empty);
            Assert.That(aliases.Select(field => field.Stat).Distinct().Count(), Is.EqualTo(aliases.Count));
            foreach (BalanceField field in aliases)
                foreach (BalanceUnit unit in field.Users) Assert.That(unit.Fields[field.Stat], Is.EqualTo(field.Key));
        }

        [Test]
        public void UnitAndFactionEdits_UseDistinctOwnersWhileSharedFieldsLink()
        {
            BalanceRegistration inventory = BalanceInventory.Build();
            Assert.That(inventory.Errors, Is.Empty);
            BalanceUnit[] units = inventory.Units.Where(unit => unit.Kind == BalanceUnitKind.Ship).Take(2).ToArray();
            BalanceField hull = inventory.Fields[units[0].Fields["Hull"]];
            BalanceDraft draft = new BalanceDraft();
            draft.Set(hull.Snapshot(), "12345");
            Assert.That(draft.Value(units[1].Fields["Hull"], "unchanged"), Is.EqualTo("unchanged"));
            Assert.That(hull.Read(), Is.Not.EqualTo("12345"));
            Assert.That(inventory.Fields[units[0].Fields["Price"]].Owner, Is.EqualTo(BalanceFieldOwner.FactionEntry));
            Assert.That(hull.Owner, Is.EqualTo(BalanceFieldOwner.UnitData));
        }

        [Test]
        public void FullRegisteredPreset_CanCaptureCurrentSchemaWithoutRuntimeWrites()
        {
            BalanceRegistration inventory = BalanceInventory.Build();
            Assert.That(inventory.Errors, Is.Empty);
            UnityEngine.Object[] targets = inventory.Fields.Values.Select(field => field.Target).Distinct().ToArray();
            string[] before = targets.Select(target => UnityEditor.EditorJsonUtility.ToJson(target)).ToArray();
            BalancePreset preset = UnityEngine.ScriptableObject.CreateInstance<BalancePreset>();
            try
            {
                BalancePresetService.Save(preset, inventory, new BalanceDraft(), BalancePresetScope.FullRegisteredSet);
                Assert.That(preset.Values.Count, Is.EqualTo(inventory.Fields.Count));
                for (int i = 0; i < targets.Length; i++) Assert.That(UnityEditor.EditorJsonUtility.ToJson(targets[i]), Is.EqualTo(before[i]));
            }
            finally { UnityEngine.Object.DestroyImmediate(preset); }
        }
    }
}
