using System.Linq;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Squadrons.Flight;
using EmpireAtWar.Components.Squadrons.Health;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Entities.Squadrons;
using EmpireAtWar.Entities.Squadrons.Data;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.ShipUi;
using EmpireAtWar.ViewComponents.Health;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Squadrons")]
    public sealed class SeparatistGinivexHeroTests
    {
        [TestCase(SquadronType.AsajjVentressGinivex, WeaponType.AsajjVentressLaser, 2800f, 1400f, 58f, 64f)]
        [TestCase(SquadronType.SevranceTannGinivex, WeaponType.SevranceTannLaser, 3000f, 1200f, 56f, 60f)]
        public void Heroes_AreUniqueSeparatistSingleFightersWithoutAbilities(
            SquadronType type, WeaponType weaponType, float hull, float shields, float cruise, float combat)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<FactionCatalog>(AssetDatabase.GUIDToAssetPath(
                AssetDatabase.FindAssets("t:FactionCatalog").Single()));
            Assert.That(catalog.Factions.Where(faction => faction.Squadrons.ContainsKey(type))
                .Select(faction => faction.FactionType), Is.EqualTo(new[] { FactionType.Separatist }));
            FactionData entry = catalog.Get(FactionType.Separatist).Squadrons[type];
            Assert.That(entry.IsHero, Is.True);
            Assert.That(entry.MaxCount, Is.EqualTo(1));
            Assert.That(entry.AvailableLevel, Is.EqualTo(2));
            Assert.That(entry.Price, Is.EqualTo(1500));
            Assert.That(entry.BuildTime, Is.EqualTo(25));
            Assert.That(entry.UnitCapacity, Is.EqualTo(2));
            Assert.That(entry.Icon, Is.Not.Null);
            Assert.That(entry.IconKey, Is.Not.Empty);
            var ui = AssetDatabase.LoadAssetAtPath<ShipUiData>("Assets/Settings/Data/Models/ShipUi/ShipUiData.asset");
            Assert.That(ui.GetSquadronIcon(type), Is.EqualTo(entry.Icon));

            var data = AssetDatabase.LoadAssetAtPath<SquadronData>(
                $"Assets/Settings/Data/Squadron/{type}SquadronData.asset");
            Assert.That(data, Is.Not.Null);
            Assert.That(data.MemberCount, Is.EqualTo(1));
            Assert.That(data.MemberHull, Is.EqualTo(hull));
            Assert.That(data.MemberShields, Is.EqualTo(shields));
            Assert.That(data.CruiseSpeed, Is.EqualTo(cruise));
            Assert.That(data.CombatSpeed, Is.EqualTo(combat));
            Assert.That(data.Abilities, Is.Empty);
            Assert.That(data.WeaponLoadout.Select(gun => (gun.WeaponType, gun.Count)),
                Is.EqualTo(new[] { (weaponType, 2) }));

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                $"Assets/Prefabs/Models/Squadrons/{type}SquadronView.prefab");
            Assert.That(prefab, Is.Not.Null);
            var flight = new SerializedObject(prefab.GetComponentInChildren<SquadronFlightComponent>(true));
            var health = new SerializedObject(prefab.GetComponentInChildren<SquadronHealthComponent>(true));
            SerializedProperty flightMembers = flight.FindProperty("fighters");
            SerializedProperty healthMembers = health.FindProperty("fighters");
            Assert.That(flightMembers.arraySize, Is.EqualTo(1));
            Assert.That(healthMembers.arraySize, Is.EqualTo(1));
            Object fighter = flightMembers.GetArrayElementAtIndex(0).objectReferenceValue;
            Assert.That(fighter, Is.Not.Null);
            Assert.That(healthMembers.GetArrayElementAtIndex(0).objectReferenceValue, Is.EqualTo(fighter));

            WeaponHardPoint[] guns = prefab.GetComponentsInChildren<WeaponHardPoint>(true);
            Assert.That(guns, Has.Length.EqualTo(2));
            Assert.That(guns.Select(gun => gun.WeaponType), Is.All.EqualTo(weaponType));
            var weapons = new SerializedObject(prefab.GetComponentInChildren<WeaponComponent>(true));
            SerializedProperty hardPoints = weapons.FindProperty("hardPoints");
            Assert.That(hardPoints.arraySize, Is.EqualTo(2));
            Assert.That(Enumerable.Range(0, hardPoints.arraySize)
                .Select(index => hardPoints.GetArrayElementAtIndex(index).objectReferenceValue),
                Is.EquivalentTo(guns));

            Assert.That(AssetDatabase.LoadAssetAtPath<GameObject>(
                $"Assets/Prefabs/Ui/Reinforcement/{type}ReinforcementView.prefab"), Is.Not.Null);
        }
    }
}
