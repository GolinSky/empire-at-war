using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EmpireAtWar.Components.Combat;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Ship.Movement;
using EmpireAtWar.Entities.Ship.Data;
using EmpireAtWar.Editor.Balance;
using EmpireAtWar.Editor.BattleSimulator;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Components.TeamColor;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Mvc;
using EmpireAtWar.ViewComponents.Health;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Ships")]
    public sealed class TitanShipTests
    {
        private const string DATA_PATH = "Assets/Settings/Data/Ship/Empire/TitanShipData.asset";
        private const string VIEW_PATH = "Assets/Prefabs/Models/Ships/Empire/TitanShipView.prefab";
        private const BindingFlags PRIVATE_INSTANCE = BindingFlags.Instance | BindingFlags.NonPublic;

        [Test]
        public void WeaponOwnershipAndRadar_MatchEveryMountedProfile()
        {
            var data = AssetDatabase.LoadAssetAtPath<ShipData>(DATA_PATH);
            var weapons = AssetDatabase.LoadAssetAtPath<WeaponsData>("Assets/Settings/Data/Models/Weapon/WeaponsData.asset");
            foreach (var entry in data.WeaponLoadout)
            {
                var definition = weapons.GetDefinition(entry.WeaponType);
                if (definition.Scope == WeaponScope.Unit) Assert.That(definition.Unit, Is.EqualTo(nameof(ShipType.Titan)));
                if (definition.Scope == WeaponScope.Faction) Assert.That(definition.Faction, Is.EqualTo(FactionType.Empire));
                Assert.That(data.Range, Is.GreaterThanOrEqualTo(weapons.GetProfile(entry.WeaponType).Range));
            }
        }

        [Test]
        public void SavedComposition_BindsAllTargetsAndRenderers()
        {
            var view = AssetDatabase.LoadAssetAtPath<GameObject>(VIEW_PATH);
            var ship = view.GetComponent<EmpireAtWar.Ship.Ship>();
            Assert.That(ship.Bindings.Injectables, Is.All.Not.Null);
            Assert.That(ship.Bindings.Injectables.Distinct().Count(), Is.EqualTo(ship.Bindings.Injectables.Length));
            foreach (var point in view.GetComponentsInChildren<HardPoint>(true))
                Assert.That(ship.Bindings.Injectables.Contains(point), Is.True, point.name);
            var renderers = new SerializedObject(view.GetComponent<UnitRenderStateComponent>()).FindProperty("meshRenderers");
            Assert.That(renderers.arraySize, Is.EqualTo(view.GetComponentsInChildren<MeshRenderer>(true).Length));
        }

        [Test]
        public void EveryHardpoint_InitializesAndCanBeDestroyedIndependently()
        {
            var data = AssetDatabase.LoadAssetAtPath<ShipData>(DATA_PATH);
            var view = AssetDatabase.LoadAssetAtPath<GameObject>(VIEW_PATH);
            var points = view.GetComponentsInChildren<HealthComponent>(true).Single().ShipUnits
                .Select(point => new HardPointModel(point.HardPointType, point.Id, point.UnlockLevel)).ToArray();
            var matrix = AssetDatabase.LoadAssetAtPath<DamageMatrixData>("Assets/Settings/Data/Models/Weapon/DamageMatrixData.asset");
            var health = new HealthModel(data, matrix, new CombatModifiers(), new HardPointHealthBudget());
            health.InitializeHardPoints(points);
            Assert.That(points.Length, Is.EqualTo(33));
            foreach (var point in points)
            {
                Assert.That(point.Health, Is.GreaterThan(0));
                point.ApplyDamage(point.Health);
                Assert.That(point.IsDestroyed, Is.True);
            }
            Assert.That(health.Hull, Is.EqualTo(data.Hull));
        }

        [TestCase("Republic/Venator")]
        [TestCase("Empire/VictoryII")]
        [TestCase("Rebellion/MonCalCruiser")]
        public void CombatSimulation_FiresAndReceivesDamageAgainstExistingCapitals(string opponentName)
        {
            var registry = BalanceInventory.BuildCombat();
            var titan = registry.Units.Single(unit => AssetDatabase.GetAssetPath(unit.Data) == DATA_PATH);
            var opponent = registry.Units.Single(unit => AssetDatabase.GetAssetPath(unit.Data) ==
                "Assets/Settings/Data/Ship/" + opponentName + "ShipData.asset");
            using (var factory = new SimUnitFactory(registry, new BalanceDraft()))
            {
                var a = factory.Build(titan);
                var b = factory.Build(opponent);
                var setup = new BattleSetup { Distance = 200, TimeLimit = 120, AlphaStrike = true };
                setup.SideA.Add(new BattleFleetEntry { UnitId = a.Id, ArcFactor = .5f });
                setup.SideB.Add(new BattleFleetEntry { UnitId = b.Id, ArcFactor = .5f });
                var battle = new BattleSimulation(setup, new Dictionary<string, SimUnitDefinition> { { a.Id, a }, { b.Id, b } }, factory.Matrix, 214);
                while (!battle.IsComplete) battle.Step();
                Assert.That(battle.Result.Units[0].Shots, Is.GreaterThan(0));
                Assert.That(battle.Result.Units[0].DamageDealt, Is.GreaterThan(0));
                Assert.That(battle.Result.Units[1].DamageDealt, Is.GreaterThan(0));
                TestContext.WriteLine($"Titan vs {opponentName}: winner {battle.Result.Winner}, {battle.Result.Duration:F2}s");
            }
        }

        [Test]
        public void SavedPrefab_AllWeaponsAreTargetableAndHaveMatchingHealth()
        {
            var root = AssetDatabase.LoadAssetAtPath<GameObject>(VIEW_PATH);
            var targets = root.GetComponentsInChildren<HardPoint>(true);
            Assert.That(targets.Length, Is.EqualTo(33));
            Assert.That(targets.Select(point => point.Id).Distinct().Count(), Is.EqualTo(33));
            Assert.That(targets.OfType<WeaponHardPoint>().Count(), Is.EqualTo(24));
            var data = AssetDatabase.LoadAssetAtPath<ShipData>(DATA_PATH);
            Assert.That(data.UsesHullHardPointBudget, Is.True);
            Assert.That(new SerializedObject(data).FindProperty("hardPointHealth"), Is.Null);
            Assert.That(targets.All(point => point.HealthWeight > 0f && point.HealthOverride == 0f), Is.True);
        }

        [Test]
        public void SavedHangar_HasNoSquadronsOrDestroyableBayBindings()
        {
            var data = new SerializedObject(AssetDatabase.LoadAssetAtPath<ShipData>(DATA_PATH));
            Assert.That(data.FindProperty("hangarBays").arraySize, Is.Zero);
            var root = AssetDatabase.LoadAssetAtPath<GameObject>(VIEW_PATH);
            var component = root.GetComponentsInChildren<MonoBehaviour>(true).Single(value => value.GetType().Name == "HangarComponent");
            var hangar = new SerializedObject(component);
            Assert.That(hangar.FindProperty("isDestroyable").boolValue, Is.False);
            Assert.That(hangar.FindProperty("bayHardPoints").arraySize, Is.Zero);
            Assert.That(hangar.FindProperty("bayLaunchPoints").arraySize, Is.Zero);
            Assert.That(root.GetComponentsInChildren<HardPoint>(true).Any(point => point.HardPointType == HardPointType.Hangar), Is.False);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void FourEngines_DestructionReducesSpeedAndAllDestroyedReachMinimum(int first)
        {
            var data = AssetDatabase.LoadAssetAtPath<ShipData>(DATA_PATH);
            var root = new GameObject("Titan engine regression") { hideFlags = HideFlags.HideAndDontSave };
            try
            {
                var ship = root.AddComponent<EmpireAtWar.Ship.Ship>();
                var movement = root.AddComponent<ShipMoveComponent>();
                var model = new ShipMoveModel(data, new CombatModifiers());
                var motion = new ShipMovementTweenPlayer(root.transform, root.transform,
                    root.AddComponent<LineRenderer>(), DG.Tweening.Ease.Linear);
                typeof(ShipMoveComponent).GetField("_motion", PRIVATE_INSTANCE).SetValue(movement, motion);
                typeof(MonoComponent<ShipMoveModel>).GetMethod("SetModel", PRIVATE_INSTANCE).Invoke(movement, new object[] { model });
                typeof(EmpireAtWar.Ship.Ship).GetField("_shipMoveComponent", PRIVATE_INSTANCE).SetValue(ship, movement);
                typeof(EmpireAtWar.Ship.Ship).GetField("<Data>k__BackingField", PRIVATE_INSTANCE).SetValue(ship, data);
                var engines = (List<HardPointModel>)typeof(EmpireAtWar.Ship.Ship).GetField("_engineUnits", PRIVATE_INSTANCE).GetValue(ship);
                var changed = (Action)Delegate.CreateDelegate(typeof(Action), ship,
                    typeof(EmpireAtWar.Ship.Ship).GetMethod("HandleEnginesData", PRIVATE_INSTANCE));
                for (int index = 0; index < 4; index++)
                {
                    var engine = new HardPointModel(HardPointType.Engines, index);
                    engine.SetHealth(2000, 1);
                    engines.Add(engine);
                    engine.OnHardPointHealthChanged += changed;
                }
                changed();
                Assert.That(model.Speed, Is.EqualTo(16f));
                engines[first].ApplyDamage(2000);
                Assert.That(model.Speed, Is.EqualTo(16f * (3f + data.MinMoveCoefficient) / 4f).Within(.001f));
                foreach (var engine in engines.Where((_, index) => index != first)) engine.ApplyDamage(2000);
                Assert.That(model.Speed, Is.EqualTo(16f * data.MinMoveCoefficient).Within(.001f));
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
    }
}
