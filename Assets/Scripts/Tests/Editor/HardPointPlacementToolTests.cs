using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Editor.Ship.HardPoints;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.ViewComponents.Health;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class HardPointPlacementToolTests
    {
        private GameObject _root;
        private EntityBindings _bindings;
        private HealthComponent _health;
        private WeaponComponent _weapons;
        private ShipHardPointRegistry _registry;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("TestShipView");
            _health = _root.AddComponent<HealthComponent>();
            _health.ShipUnits = new List<HardPoint>();
            _bindings = _root.AddComponent<EntityBindings>();
            GameObject weaponsObject = new GameObject("Weapons");
            weaponsObject.transform.SetParent(_root.transform, false);
            _weapons = weaponsObject.AddComponent<WeaponComponent>();

            SerializedObject bindings = new SerializedObject(_bindings);
            SetList(bindings.FindProperty("<Health>k__BackingField"), _health);
            SetList(bindings.FindProperty("<Weapons>k__BackingField"), _weapons);
            bindings.ApplyModifiedPropertiesWithoutUndo();

            _registry = new ShipHardPointRegistry(_bindings);
            WeaponHardPoint first = (WeaponHardPoint)_registry.AddNew(HardPointType.Weapon, default(WeaponType),
                weaponsObject.transform, "Turbolaser_HP_TL01", new Vector3(5f, 1f, 2f));
            SerializedObject arc = new SerializedObject(first);
            arc.FindProperty("yAxisRange.<Min>k__BackingField").floatValue = 10f;
            arc.FindProperty("yAxisRange.<Max>k__BackingField").floatValue = 170f;
            arc.ApplyModifiedPropertiesWithoutUndo();
            _registry.AddNew(HardPointType.Engines, null, _root.transform, "HP_ENGINE_01", Vector3.back);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_root);

        [TestCase("HP_TL01", true)]
        [TestCase("HP_ENGINE_02", true)]
        [TestCase("HP_TL03_Blast", false)]
        [TestCase("HP_IC01_EmitDamage", false)]
        [TestCase("HP_IC_BL_Blast01", false)]
        [TestCase("Hull", false)]
        public void IsHardPointBone_AcceptsNumberedHardPointBonesOnly(string name, bool expected) =>
            Assert.That(HardPointBoneScanner.IsHardPointBone(name), Is.EqualTo(expected));

        [Test]
        public void BoneNames_ResolvePrefixTypeAndGroup()
        {
            Assert.That(HardPointBoneScanner.GetPrefix("HP_ENGINE_02"), Is.EqualTo("HP_ENGINE"));
            Assert.That(HardPointBoneScanner.GuessType("HP_ENGINE_02"), Is.EqualTo(HardPointType.Engines));
            Assert.That(HardPointBoneScanner.GuessType("HP_SPAWN_01"), Is.EqualTo(HardPointType.Hangar));
            Assert.That(HardPointBoneScanner.GuessType("HP_TL07"), Is.EqualTo(HardPointType.Weapon));
            Assert.That(HardPointNaming.GetGroup(_health.ShipUnits[0]), Is.EqualTo("Turbolaser"));
            Assert.That(HardPointNaming.GetGroup(_health.ShipUnits[1]), Is.EqualTo("HP_ENGINE"));
        }

        [Test]
        public void FindFreeBones_SkipsBonesThatAlreadyHaveHardPoints()
        {
            Transform used = CreateBone("HP_TL01", Vector3.zero);
            Transform free = CreateBone("HP_TL02", Vector3.one);
            CreateBone("HP_TL02_Blast", Vector3.one);

            List<Transform> bones = HardPointBoneScanner.FindFreeBones(_root.transform, _registry.HardPoints);

            Assert.That(bones, Is.EqualTo(new[] { free }));
            Assert.That(HardPointBoneScanner.FindBone(_health.ShipUnits[0], new[] { used, free }), Is.SameAs(used));
            Assert.That(HardPointBoneScanner.FindTemplate("HP_TL02", _registry.HardPoints), Is.SameAs(_health.ShipUnits[0]));
            Assert.That(HardPointNaming.ForBone(_health.ShipUnits[0], free), Is.EqualTo("Turbolaser_HP_TL02"));
        }

        [Test]
        public void AddClone_RegistersInEveryListWithSequentialIds()
        {
            HardPoint clone = _registry.AddClone(_health.ShipUnits[0], "Turbolaser_HP_TL02", Vector3.one);

            Assert.That(clone, Is.InstanceOf<WeaponHardPoint>());
            Assert.That(clone.transform.parent, Is.SameAs(_weapons.transform));
            Assert.That(_health.ShipUnits.Last(), Is.SameAs(clone));
            Assert.That(_bindings.Injectables, Is.EquivalentTo(_health.ShipUnits));
            Assert.That(WeaponHardPoints(), Is.EqualTo(_health.ShipUnits.OfType<WeaponHardPoint>()));
            Assert.That(_health.ShipUnits.Select(unit => unit.Id), Is.EqualTo(new[] { 0, 1, 2 }));
            Assert.That(ShipHardPointValidator.Validate(_registry, _bindings, null), Has.Count.EqualTo(1),
                "Only the missing ShipData note remains.");
        }

        [Test]
        public void AddMirror_FlipsPositionAndArc()
        {
            WeaponHardPoint mirror = (WeaponHardPoint)_registry.AddMirror(_health.ShipUnits[0], "Turbolaser_01");

            Assert.That(mirror.transform.position, Is.EqualTo(new Vector3(-5f, 1f, 2f)));
            Assert.That(mirror.MinYaw, Is.EqualTo(-170f));
            Assert.That(mirror.MaxYaw, Is.EqualTo(-10f));
        }

        [Test]
        public void Remove_ClearsEveryListAndRenumbers()
        {
            HardPoint weapon = _health.ShipUnits[0];

            _registry.Remove(weapon);

            Assert.That(_health.ShipUnits.Select(unit => unit.name), Is.EqualTo(new[] { "HP_ENGINE_01" }));
            Assert.That(_health.ShipUnits[0].Id, Is.EqualTo(0));
            Assert.That(_bindings.Injectables, Is.EquivalentTo(_health.ShipUnits));
            Assert.That(WeaponHardPoints(), Is.Empty);
        }

        [Test]
        public void Validate_ReportsHardPointsMissingFromLists()
        {
            GameObject stray = new GameObject("Stray");
            stray.transform.SetParent(_root.transform, false);
            HardPoint unlisted = stray.AddComponent<WeaponHardPoint>();

            Assert.That(ShipHardPointValidator.Validate(_registry, _bindings, null),
                Has.Some.Contains("Stray is not in HealthComponent.ShipUnits"));

            _registry.Register(unlisted);

            Assert.That(ShipHardPointValidator.Validate(_registry, _bindings, null), Has.Count.EqualTo(1));
            Assert.That(WeaponHardPoints(), Does.Contain(unlisted));
        }

        private List<WeaponHardPoint> WeaponHardPoints()
        {
            SerializedProperty list = new SerializedObject(_weapons).FindProperty("hardPoints");
            return Enumerable.Range(0, list.arraySize)
                .Select(index => (WeaponHardPoint)list.GetArrayElementAtIndex(index).objectReferenceValue).ToList();
        }

        private Transform CreateBone(string name, Vector3 position)
        {
            Transform bone = new GameObject(name).transform;
            bone.SetParent(_root.transform, false);
            bone.position = position;
            return bone;
        }

        private static void SetList(SerializedProperty list, Object value)
        {
            list.arraySize = 1;
            list.GetArrayElementAtIndex(0).objectReferenceValue = value;
        }
    }
}
