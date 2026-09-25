using System;
using System.Collections.Generic;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Entities.SuperWeapons;
using EmpireAtWar.Entities.SuperWeapons.Ui;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Views.Game;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class SuperWeaponTests
    {
        [Test]
        public void Model_OnePurchaseIsOneShot()
        {
            SuperWeaponModel model = new SuperWeaponModel();
            List<SuperWeaponState> states = new List<SuperWeaponState>();
            model.OnStateChanged += (_, state) => states.Add(state);

            model.StartCharging(SuperWeaponType.IonCannon);
            Assert.That(model.CanPurchase(SuperWeaponType.IonCannon), Is.False);
            model.CompleteCharging(SuperWeaponType.IonCannon);
            Assert.That(model.CanPurchase(SuperWeaponType.IonCannon), Is.False);
            model.Consume(SuperWeaponType.IonCannon);

            Assert.That(model.CanPurchase(SuperWeaponType.IonCannon), Is.True);
            Assert.That(states, Is.EqualTo(new[]
            {
                SuperWeaponState.Charging, SuperWeaponState.Ready, SuperWeaponState.Unavailable
            }));
        }

        [Test]
        public void Model_RejectsFiringAWeaponThatIsNotReady()
        {
            SuperWeaponModel model = new SuperWeaponModel();
            model.StartCharging(SuperWeaponType.PlasmaCannon);

            Assert.Throws<InvalidOperationException>(() => model.Consume(SuperWeaponType.PlasmaCannon));
            Assert.Throws<InvalidOperationException>(() => model.StartCharging(SuperWeaponType.PlasmaCannon));
        }

        [Test]
        public void CoreGamePrefab_HasOneWiredButtonPerSuperWeapon()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/Ui/SkirmishGame/CoreGameUi.prefab");
            SuperWeaponsView view = (SuperWeaponsView)prefab.GetComponent<CoreGameUi>().SuperWeaponsView;
            Assert.That(view, Is.Not.Null);
            SerializedProperty buttons = new SerializedObject(view).FindProperty("buttons");
            Assert.That(buttons.arraySize, Is.EqualTo(Enum.GetValues(typeof(SuperWeaponType)).Length));
            HashSet<SuperWeaponType> seen = new HashSet<SuperWeaponType>();
            for (int i = 0; i < buttons.arraySize; i++)
            {
                SuperWeaponButton weapon =
                    (SuperWeaponButton)buttons.GetArrayElementAtIndex(i).objectReferenceValue;
                Assert.That(weapon, Is.Not.Null);
                Assert.That(seen.Add(weapon.WeaponType), Is.True, $"Duplicate weapon: {weapon.WeaponType}");
                SerializedObject buttonData = new SerializedObject(weapon);
                Button button = (Button)buttonData.FindProperty("button").objectReferenceValue;
                Assert.That(button, Is.Not.Null);
                Assert.That(button.transition, Is.EqualTo(Selectable.Transition.ColorTint));
                Assert.That(buttonData.FindProperty("pendingHighlight").objectReferenceValue, Is.Not.Null);
            }
        }

        [Test]
        public void Data_DefinesPurchaseAndProfileForEverySuperWeapon()
        {
            FactionsData factions = AssetDatabase.LoadAssetAtPath<FactionsData>(
                "Assets/Settings/Data/Models/Factions/FactionsData.asset");
            SuperWeaponData data = AssetDatabase.LoadAssetAtPath<SuperWeaponData>(
                "Assets/Settings/Data/Models/SuperWeapons/SuperWeaponData.asset");
            foreach (SuperWeaponType type in Enum.GetValues(typeof(SuperWeaponType)))
            {
                FactionData purchase = factions.SuperWeaponFactionData[type];
                Assert.That(purchase.MaxCount, Is.EqualTo(1));
                Assert.That(purchase.Icon, Is.Not.Null);
                SuperWeaponProfile profile = data.GetProfile(type);
                Assert.That(profile.Weapon.ShotPrefab, Is.Not.Null, type.ToString());
                Assert.That(profile.Weapon.ShotsPerSalvo, Is.GreaterThan(0), type.ToString());
                Assert.That(Enum.IsDefined(typeof(WeaponType), profile.Weapon.WeaponType), Is.True, type.ToString());
            }
        }
    }
}
