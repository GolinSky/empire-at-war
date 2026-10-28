using System.Linq;
using EmpireAtWar.Editor.Balance;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Services.ShipAbilities;
using EmpireAtWar.Services.ShipAbilities.Abilities;
using EmpireAtWar.ViewComponents.Health;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Balance")]
    public sealed class BalanceInventoryTests
    {
        [Test]
        public void AbilityInventory_RegistersCurrentSupportAbilitiesAndTheirTuningFields()
        {
            BalanceRegistration inventory = new BalanceRegistration();
            BalanceAbilityAdapter.Register(inventory);

            foreach (ShipAbilityId id in new[] { ShipAbilityId.DauntlessSensorJamming,
                         ShipAbilityId.FighterReinforcements, ShipAbilityId.LiberatorRapidDeployment,
                         ShipAbilityId.MC30cTorpedoSalvo, ShipAbilityId.FighterCoordination })
                foreach (string stat in new[] { "duration", "recoveryDelay", "range", "aiUse" })
                    Assert.That(inventory.Fields.Values.Any(field => field.Stat == $"ability/{(int)id}/{stat}"),
                        Is.True, $"{id}: {stat}");

            Assert.That(inventory.Fields.Values.Any(field =>
                field.Dependency == nameof(TorpedoSalvoSettings) && field.Stat.EndsWith("/fireDelayMultiplier")), Is.True);
            Assert.That(inventory.Fields.Values.Any(field =>
                field.Dependency == nameof(FighterCoordinationSettings) && field.Stat.EndsWith("/commandRadius")), Is.True);
        }

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
        public void RepeatedSquadrons_HaveIndependentHangarBayFields()
        {
            BalanceRegistration inventory = BalanceInventory.Build();
            BalanceUnit unit = inventory.Units.First(entry => entry.Data.name == "LiberatorShipData");
            UnityEngine.Object data = UnityEngine.Object.Instantiate(unit.Data);
            try
            {
                using var serialized = new UnityEditor.SerializedObject(data);
                var bays = serialized.FindProperty("hangarBays");
                bays.arraySize = 2;
                bays.GetArrayElementAtIndex(1).FindPropertyRelative("squadronType").intValue =
                    bays.GetArrayElementAtIndex(0).FindPropertyRelative("squadronType").intValue;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                BalanceRegistration registry = new BalanceRegistration();
                BalanceUnitAdapter.Hangar(registry, data, bays, "hangarBays", unit);
                var fields = registry.Fields.Values.Where(field => field.Stat.EndsWith("/reserve")).ToArray();
                Assert.That(fields.Length, Is.EqualTo(2));
                Assert.That(fields.Select(field => field.Key).Distinct().Count(), Is.EqualTo(2));
                Assert.That(fields.Select(field => field.Path).Distinct().Count(), Is.EqualTo(2));
                BalanceDraft draft = new BalanceDraft();
                draft.Set(fields[0].Snapshot(), "3");
                Assert.That(draft.Value(fields[1].Key, "unchanged"), Is.EqualTo("unchanged"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(data);
            }
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
            foreach (BalanceField field in inventory.Fields.Values.Where(field => field.Stat.StartsWith("weapon/") && field.Stat.EndsWith("/" + BalanceWeaponAdapter.DEFINITION_MARKER)))
            {
                int id = int.Parse(field.Stat.Split('/')[1]);
                var expected = inventory.Units.Where(unit => unit.Mounts.OfType<WeaponHardPoint>().Any(mount => (int)mount.WeaponType == id)).Select(unit => unit.Id).Distinct().OrderBy(value => value);
                Assert.That(field.Users.Select(unit => unit.Id).OrderBy(value => value), Is.EqualTo(expected), field.Context);
            }
        }

        [Test]
        public void LaserAndProtonBeamSettings_ProduceIndependentTargetsInEveryCompareCell()
        {
            BalanceRegistration inventory = BalanceInventory.Build();
            Assert.That(inventory.Errors, Is.Empty);
            var beams = inventory.Fields.Values.Where(field => field.Stat == "settings/1/damage" || field.Stat == "settings/10/damage").ToList();
            Assert.That(beams.Count, Is.EqualTo(2));
            Assert.That(beams.Select(field => field.Key).Distinct().Count(), Is.EqualTo(2));
            Assert.That(beams.All(field => field.AliasWarning.Length == 0), Is.True);
            foreach (BalanceField field in beams)
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
