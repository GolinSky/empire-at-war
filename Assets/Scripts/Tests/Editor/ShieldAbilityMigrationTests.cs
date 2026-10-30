using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using EmpireAtWar.Entities.Ship.Abilities;
using EmpireAtWar.Entities.Ship.Data;
using EmpireAtWar.Services.Audio;
using EmpireAtWar.Services.Saves;
using EmpireAtWar.Services.ShipAbilities;
using EmpireAtWar.Services.ShipAbilities.Abilities;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Abilities")]
    public sealed class ShieldAbilityMigrationTests
    {
        private const string CATALOG_PATH = "Assets/Settings/Data/Models/ShipAbilities/ShipAbilityCatalog.asset";

        [TestCase(13)]
        [TestCase(15)]
        [TestCase(17)]
        [TestCase(26)]
        [TestCase(34)]
        [TestCase(45)]
        [TestCase(53)]
        public void LegacySave_ValidatesAndRestoresSharedShieldWithoutShorteningCooldown(int legacyId)
        {
            AbilitySlotSave save = JsonUtility.FromJson<AbilitySlotSave>(
                $"{{\"id\":{legacyId},\"state\":2,\"timeLeft\":83.5}}");
            Assert.That(save.Id, Is.EqualTo(ShipAbilityId.BoostShieldPower));
            Assert.DoesNotThrow(() => Validate(new[] { save }, new[] { ShipAbilityId.BoostShieldPower }));

            ShipAbilityDefinition definition = Catalog().Get(save.Id);
            var slot = new ShipAbilitySlot(null, definition, save.Id);
            new ShipAbilityService(null, null).Restore(slot, save.State, save.TimeLeft);

            Assert.That(slot.State, Is.EqualTo(ShipAbilityState.Recovering));
            Assert.That(slot.TimeLeft, Is.EqualTo(83.5f));
            Assert.That(slot.Progress01, Is.EqualTo(1f));
            Assert.That(AbilitySlotSave.From(slot).Id, Is.EqualTo(ShipAbilityId.BoostShieldPower));
            Assert.That(JsonUtility.ToJson(AbilitySlotSave.From(slot)), Does.Contain("\"id\":3"));
        }

        [Test]
        public void LegacySave_PreservesSlotOrderAndUnrelatedIds()
        {
            AbilitySlotSave[] slots =
            {
                JsonUtility.FromJson<AbilitySlotSave>("{\"id\":16,\"state\":0,\"timeLeft\":0}"),
                JsonUtility.FromJson<AbilitySlotSave>("{\"id\":17,\"state\":0,\"timeLeft\":0}")
            };
            Assert.DoesNotThrow(() => Validate(slots,
                new[] { ShipAbilityId.HomeOneConcentrateFire, ShipAbilityId.BoostShieldPower }));
            Assert.That(slots.Length, Is.EqualTo(2));
            Assert.That(slots[0].Id, Is.EqualTo(ShipAbilityId.HomeOneConcentrateFire));
            Assert.That(slots[1].State, Is.EqualTo(ShipAbilityState.Ready));
        }

        [Test]
        public void LegacySave_CollidingShieldSlotsAreRejectedRatherThanDiscarded()
        {
            AbilitySlotSave[] slots =
            {
                JsonUtility.FromJson<AbilitySlotSave>("{\"id\":13,\"state\":2,\"timeLeft\":60}"),
                JsonUtility.FromJson<AbilitySlotSave>("{\"id\":26,\"state\":2,\"timeLeft\":90}")
            };
            var error = Assert.Throws<TargetInvocationException>(() =>
                Validate(slots, new[] { ShipAbilityId.BoostShieldPower }));
            Assert.That(error.InnerException, Is.TypeOf<InvalidDataException>());
            Assert.That(error.InnerException.Message, Does.Contain("multiple shield power slots"));
            Assert.That(slots.Length, Is.EqualTo(2));
            Assert.That(slots[1].TimeLeft, Is.EqualTo(90f));
        }

        [TestCase("Rebellion/NebulonB", 0)]
        [TestCase("Rebellion/MonCalCruiser", 0)]
        [TestCase("Rebellion/HomeOne", 1)]
        [TestCase("Rebellion/MC80Independence", 0)]
        [TestCase("Rebellion/CorellianBattlecruiser", 0)]
        [TestCase("Rebellion/AssaultFrigateMkII", 0)]
        [TestCase("Separatist/Wavecrest", 0)]
        public void MigratedShip_UsesSharedShieldInOriginalSlot(string ship, int slot)
        {
            ShipData data = AssetDatabase.LoadAssetAtPath<ShipData>(
                "Assets/Settings/Data/Ship/" + ship + "ShipData.asset");
            Assert.That(data.Abilities[slot], Is.EqualTo(ShipAbilityId.BoostShieldPower));
        }

        [Test]
        public void Catalog_ContainsOnlySharedShieldDefinitionAndExpectedBalance()
        {
            ShipAbilityCatalog catalog = Catalog();
            ShipAbilityDefinition shared = catalog.Get(ShipAbilityId.BoostShieldPower);
            Assert.That(shared.Settings, Is.TypeOf<BoostShieldPowerSettings>());
            Assert.That(shared.Duration, Is.EqualTo(15f));
            Assert.That(shared.RecoveryDelay, Is.EqualTo(40f));
            Assert.That(shared.CanCancel, Is.True);
            using var serialized = new SerializedObject(catalog);
            SerializedProperty entries = serialized.FindProperty("definitions.keyValue");
            var seen = new HashSet<int>();
            for (int i = 0; i < entries.arraySize; i++)
            {
                int id = entries.GetArrayElementAtIndex(i).FindPropertyRelative("key").intValue;
                Assert.That(Enum.IsDefined(typeof(ShipAbilityId), id), Is.True, $"Obsolete catalog id {id}");
                Assert.That(seen.Add(id), Is.True, $"Duplicate catalog id {id}");
                if (catalog.Get((ShipAbilityId)id).Settings is BoostShieldPowerSettings)
                {
                    Assert.That(id, Is.EqualTo(3));
                    SerializedProperty modifier = entries.GetArrayElementAtIndex(i)
                        .FindPropertyRelative("value.settings.statModifier");
                    Assert.That(modifier.FindPropertyRelative("shieldRegenMultiplier").floatValue, Is.EqualTo(4f));
                    Assert.That(modifier.FindPropertyRelative("damageMultiplier").floatValue, Is.EqualTo(0.75f));
                    Assert.That(modifier.FindPropertyRelative("speedMultiplier").floatValue, Is.EqualTo(0.75f));
                }
            }
        }

        [Test]
        public void Audio_ContainsUniqueCurrentAbilityIds()
        {
            ShipSfxData data = AssetDatabase.LoadAssetAtPath<ShipSfxData>(
                "Assets/Settings/Data/Models/Audio/ShipSfxData.asset");
            using var serialized = new SerializedObject(data);
            SerializedProperty entries = serialized.FindProperty("abilities");
            var seen = new HashSet<int>();
            for (int i = 0; i < entries.arraySize; i++)
            {
                int id = entries.GetArrayElementAtIndex(i).FindPropertyRelative("abilityId").intValue;
                Assert.That(Enum.IsDefined(typeof(ShipAbilityId), id), Is.True, $"Obsolete audio id {id}");
                Assert.That(seen.Add(id), Is.True, $"Duplicate audio id {id}");
            }
            Assert.That(seen, Does.Contain(3));
        }

        private static ShipAbilityCatalog Catalog() => AssetDatabase.LoadAssetAtPath<ShipAbilityCatalog>(CATALOG_PATH);

        private static void Validate(AbilitySlotSave[] slots, IReadOnlyList<ShipAbilityId> expected) =>
            typeof(BattleSaveValidator).GetMethod("ValidateAbilities", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { slots, expected, 42L });
    }
}
