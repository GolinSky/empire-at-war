using System;
using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Combat;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.Ship.Abilities;
using EmpireAtWar.Entities.Ship.Data;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Services.ShipAbilities;
using EmpireAtWar.Services.ShipAbilities.Abilities;
using EmpireAtWar.ViewComponents.Health;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class RaiderCorvetteTests
    {
        private const string VIEW = "Assets/Prefabs/Models/Ships/RaiderCorvetteShipView.prefab";
        private const string DATA = "Assets/Settings/Data/Ship/RaiderCorvetteShipData.asset";

        [Test]
        public void SavedUnit_HasRequestedStatsNonTargetableLoadoutAndNoHangar()
        {
            ShipData data = AssetDatabase.LoadAssetAtPath<ShipData>(DATA);
            Assert.That(data.Hull, Is.EqualTo(600));
            Assert.That(data.Shields, Is.EqualTo(800));
            Assert.That(data.Speed, Is.EqualTo(35));
            Assert.That(data.Abilities, Is.EqualTo(new[] { ShipAbilityId.Pursuit }));
            Assert.That(data.HangarBays, Is.Empty);
            Assert.That(new SerializedObject(data).FindProperty("hardPointHealth").arraySize, Is.Zero);
            GameObject view = AssetDatabase.LoadAssetAtPath<GameObject>(VIEW);
            Assert.That(view.GetComponentsInChildren<HealthComponent>(true).Single().ShipUnits, Is.Empty);
            Assert.That(view.GetComponentsInChildren<MonoBehaviour>(true).Any(m => m.GetType().Name == "HangarComponent"), Is.False);
            WeaponHardPoint[] weapons = view.GetComponentsInChildren<WeaponHardPoint>(true);
            Assert.That(weapons.Length, Is.EqualTo(10));
            Assert.That(weapons.Select(w => w.Id).Distinct().Count(), Is.EqualTo(10));
            WeaponsData profiles = AssetDatabase.LoadAssetAtPath<WeaponsData>("Assets/Settings/Data/Models/Weapon/WeaponsData.asset");
            foreach (var expected in new[] { (WeaponType.BurstLaserCannon, 4, 2), (WeaponType.DualRepeatingPointDefense, 2, 6),
                (WeaponType.HeavyBurstIonBlaster, 2, 2), (WeaponType.HeavyBurstConcussionMissile, 2, 5) })
            {
                Assert.That(weapons.Count(w => w.WeaponType == expected.Item1), Is.EqualTo(expected.Item2));
                Assert.That(profiles.GetProfile(expected.Item1).ShotsPerSalvo, Is.EqualTo(expected.Item3));
                Assert.That(profiles.GetProfile(expected.Item1).ShotPrefab, Is.Not.Null);
            }
            Assert.That(profiles.GetProfile(WeaponType.HeavyBurstConcussionMissile).Interceptable, Is.True);
            Assert.That(profiles.GetProfile(WeaponType.DualRepeatingPointDefense).StrikecraftOnly, Is.True);
        }

        [Test]
        public void SavedArcs_CoverBowAndAuthoredPortStarboardDirections()
        {
            WeaponHardPoint[] weapons = AssetDatabase.LoadAssetAtPath<GameObject>(VIEW).GetComponentsInChildren<WeaponHardPoint>(true);
            foreach (WeaponHardPoint weapon in weapons)
            {
                bool narrow = weapon.WeaponType == WeaponType.HeavyBurstIonBlaster || weapon.WeaponType == WeaponType.HeavyBurstConcussionMissile;
                float center = narrow ? (weapon.transform.localPosition.x < 0 ? -45 : 45) : 0;
                Assert.That((weapon.MinYaw + weapon.MaxYaw) / 2, Is.EqualTo(center).Within(.001f), weapon.name);
                Assert.That(weapon.MaxYaw - weapon.MinYaw, Is.EqualTo(narrow ? 100 : 240).Within(.001f), weapon.name);
                Assert.That(weapon.MinYaw, Is.LessThan(0), weapon.name + " must engage ahead");
                Assert.That(weapon.MaxYaw, Is.GreaterThan(0), weapon.name + " must engage ahead");
            }
        }

        [Test]
        public void Pursuit_ActivatesWithoutTargetAndRestoresOtherModifiersOnStop()
        {
            ShipAbilityDefinition definition = AssetDatabase.LoadAssetAtPath<ShipAbilityCatalog>(
                "Assets/Settings/Data/Models/ShipAbilities/ShipAbilityCatalog.asset").Get(ShipAbilityId.Pursuit);
            Assert.That(definition.DisplayName, Is.EqualTo("Pursuit"));
            Assert.That(definition.RequiresEnemyTarget, Is.False);
            Assert.That(definition.Duration, Is.EqualTo(15));
            Assert.That(definition.RecoveryDelay, Is.EqualTo(60));
            Assert.That(definition.Settings, Is.TypeOf<BoostEnginePowerSettings>());
            var caster = new SelfCaster();
            var existing = new CombatStatModifier(1.5f, .8f, 1.2f, 2, .75f);
            caster.Modifiers.Add(existing);
            IShipAbility ability = definition.Settings.CreateAbility(new DiContainer());
            for (int activation = 0; activation < 2; activation++)
            {
                ability.Start(caster, definition, null);
                Assert.That(caster.Modifiers.DamageMultiplier, Is.EqualTo(3));
                Assert.That(caster.Modifiers.FireDelayMultiplier, Is.EqualTo(.4f).Within(.0001f));
                Assert.That(caster.Modifiers.SpeedMultiplier, Is.EqualTo(3));
                Assert.That(caster.Modifiers.ShieldRegenMultiplier, Is.Zero);
                Assert.That(caster.Modifiers.DamageTakenMultiplier, Is.EqualTo(.75f));
                ability.Stop();
                Assert.That(caster.Modifiers.DamageMultiplier, Is.EqualTo(1.5f));
                Assert.That(caster.Modifiers.FireDelayMultiplier, Is.EqualTo(.8f));
                Assert.That(caster.Modifiers.SpeedMultiplier, Is.EqualTo(1.2f));
                Assert.That(caster.Modifiers.ShieldRegenMultiplier, Is.EqualTo(2));
            }
        }

        [Test]
        public void SavedRegistrations_ResolveOwnDataIconsPlacementAndWreck()
        {
            Assert.That(AssetDatabase.LoadAssetAtPath<ShipsData>("Assets/Settings/Data/Ship/ShipsData.asset").GetShipDataPath(ShipType.RaiderCorvette), Is.EqualTo(AssetDatabase.AssetPathToGUID(DATA)));
            foreach (string path in new[] { VIEW, DATA })
                Assert.That(AddressableAssetSettingsDefaultObject.Settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(path)).address, Is.EqualTo(System.IO.Path.GetFileNameWithoutExtension(path)));
            Sprite icon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Textures/Ui/Icons/ShipIcon/RaiderCorvetteIcon.png");
            var faction = Entry("Assets/Settings/Data/Factions/Empire/EmpireFaction.asset", "ships.keyValue");
            Assert.That(faction.FindPropertyRelative("<Name>k__BackingField").stringValue, Is.EqualTo("Raider Corvette"));
            Assert.That(faction.FindPropertyRelative("<Icon>k__BackingField").objectReferenceValue, Is.EqualTo(icon));
            Assert.That(Entry("Assets/Settings/Data/Models/ShipUi/ShipUiData.asset", "shipIconWrapper.keyValue").objectReferenceValue, Is.EqualTo(icon));
            Assert.That(AssetDatabase.GetAssetPath(Entry("Assets/Settings/Data/Reinforcement/ReinforcementData.asset", "spawnShipWrapper.keyValue").objectReferenceValue), Is.EqualTo("Assets/Prefabs/Ui/Reinforcement/RaiderCorvetteReinforcementView.prefab"));
            var wreck = new SerializedObject(AssetDatabase.LoadAssetAtPath<ShipData>(DATA).Wreck);
            Assert.That(AssetDatabase.GetAssetPath(wreck.FindProperty("<Prefab>k__BackingField").objectReferenceValue), Is.EqualTo("Assets/Prefabs/Models/Wrecks/RaiderCorvetteWreckView.prefab"));
        }

        [TestCase("Assets/Prefabs/Models/Ships/RaiderCorvette.prefab")]
        [TestCase(VIEW)]
        [TestCase("Assets/Prefabs/Models/Wrecks/RaiderCorvetteWreckView.prefab")]
        [TestCase("Assets/Prefabs/Ui/Reinforcement/RaiderCorvetteReinforcementView.prefab")]
        public void SavedPrefabs_HaveCompleteScriptsGeometryAndReferences(string path)
        {
            GameObject view = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.That(view.transform.localScale, Is.EqualTo(Vector3.one));
            foreach (Transform transform in view.GetComponentsInChildren<Transform>(true))
                Assert.That(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject), Is.Zero, transform.name);
            foreach (MeshRenderer renderer in view.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.enabled))
            {
                Assert.That(renderer.GetComponents<MeshFilter>().Single().sharedMesh, Is.Not.Null, renderer.name);
                Assert.That(renderer.sharedMaterials.All(m => m != null), Is.True, renderer.name);
            }
            foreach (MonoBehaviour component in view.GetComponentsInChildren<MonoBehaviour>(true))
            {
                var property = new SerializedObject(component).GetIterator();
                while (property.Next(true))
                    if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue == null)
                        Assert.That(property.objectReferenceEntityIdValue, Is.EqualTo(EntityId.None), component.name + "/" + property.propertyPath);
            }
        }

        private static SerializedProperty Entry(string path, string field)
        {
            var array = new SerializedObject(AssetDatabase.LoadAssetAtPath<ScriptableObject>(path)).FindProperty(field);
            return Enumerable.Range(0, array.arraySize).Select(array.GetArrayElementAtIndex)
                .Single(p => p.FindPropertyRelative("key").intValue == (int)ShipType.RaiderCorvette).FindPropertyRelative("value");
        }

        private sealed class SelfCaster : IShipAbilityFacade
        {
            public CombatModifiers Modifiers { get; } = new CombatModifiers();
            public IReadOnlyList<ShipAbilitySlot> Slots => Array.Empty<ShipAbilitySlot>();
            public Vector3 WorldPosition => Vector3.zero;
            public float RadarRange => 0;
            public IEntity Entity => throw new NotSupportedException("Pursuit must not require an entity or attack target.");
            public IHealthModelObserver Health => throw new NotSupportedException("Pursuit only modifies combat stats.");
        }
    }
}
