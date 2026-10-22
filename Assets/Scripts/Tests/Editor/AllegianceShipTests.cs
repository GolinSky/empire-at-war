using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Combat;
using EmpireAtWar.Components.Hangar;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Editor.Balance;
using EmpireAtWar.Editor.BattleSimulator;
using EmpireAtWar.Entities.Ship.Data;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Components.TeamColor;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Services.ShipAbilities;
using EmpireAtWar.ViewComponents.Health;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class AllegianceShipTests
    {
        private const string VIEW = "Assets/Prefabs/Models/Ships/Empire/AllegianceShipView.prefab";
        private const string DATA = "Assets/Settings/Data/Ship/Empire/AllegianceShipData.asset";

        [Test]
        public void SpawnInjection_RegistersEveryCurrentHardpointWithoutNullDonorReferences()
        {
            var view = AssetDatabase.LoadAssetAtPath<GameObject>(VIEW);
            var bindings = view.GetComponent<EntityBindings>();
            var points = view.GetComponentsInChildren<HealthComponent>(true).Single().ShipUnits;
            var team = view.GetComponentsInChildren<UnitRenderStateComponent>(true).Single();
            Assert.That(bindings.Injectables, Is.All.Not.Null);
            Assert.That(bindings.Injectables, Is.EquivalentTo(points.Cast<MonoBehaviour>().Append(team)));
            var builder = new ContainerBuilder();
            Assert.DoesNotThrow(() =>
            {
                foreach (var injectable in bindings.Injectables)
                    builder.RegisterComponent(injectable).Keyed(injectable);
            });
        }

        [Test]
        public void SavedShip_HasRequestedBalanceAndNoSquadrons()
        {
            var data = AssetDatabase.LoadAssetAtPath<ShipData>(DATA);
            Assert.That(data.Hull, Is.EqualTo(20000));
            Assert.That(data.Shields, Is.EqualTo(16000));
            Assert.That(data.Speed, Is.EqualTo(12));
            Assert.That(data.Abilities, Is.EquivalentTo(new[] {ShipAbilityId.PowerToMainBatteries, ShipAbilityId.TractorBeam}));
            var view = AssetDatabase.LoadAssetAtPath<GameObject>(VIEW);
            Assert.That(view.GetComponentsInChildren<HangarComponent>(true), Is.Empty);
            var hangar = new HangarModel(data);
            Assert.That(hangar.BayCount, Is.Zero);
            Assert.That(hangar.TryLaunch(1000, out _), Is.False);
            var health = view.GetComponentsInChildren<HealthComponent>(true).Single();
            var guns = view.GetComponentsInChildren<WeaponHardPoint>(true);
            Assert.That(health.ShipUnits.Select(p => p.Id), Is.EquivalentTo(Enumerable.Range(0, 28)));
            Assert.That(guns.Length, Is.EqualTo(21));
            Assert.That(guns.All(health.ShipUnits.Contains), Is.True);
            foreach (var pair in new[] {(WeaponType.DualHeavyTurboLaser, 3), (WeaponType.QuadTurboLaser, 6),
                (WeaponType.DualMediumTurboLaser, 6), (WeaponType.MediumIonCannon, 6)})
                Assert.That(guns.Count(g => g.WeaponType == pair.Item1), Is.EqualTo(pair.Item2));
            Assert.That(guns.Count(g => new SerializedObject(g).FindProperty("mainBattery").boolValue), Is.EqualTo(9));
            foreach (var pair in new[] {(HardPointType.Engines, 3), (HardPointType.ShieldGenerator, 2),
                (HardPointType.TractorBeam, 1), (HardPointType.CommandArray, 1)})
                Assert.That(health.ShipUnits.Count(p => p.HardPointType == pair.Item1), Is.EqualTo(pair.Item2));
        }

        [Test]
        public void EverySubsystem_InitializesAndCanBeDestroyedIndependently()
        {
            var data = AssetDatabase.LoadAssetAtPath<ShipData>(DATA);
            var view = AssetDatabase.LoadAssetAtPath<GameObject>(VIEW);
            var points = view.GetComponentsInChildren<HealthComponent>(true).Single().ShipUnits
                .Select(p => new HardPointModel(p.HardPointType, p.Id, p.UnlockLevel)).ToArray();
            var matrix = AssetDatabase.LoadAssetAtPath<DamageMatrixData>("Assets/Settings/Data/Models/Weapon/DamageMatrixData.asset");
            var health = new HealthModel(data, matrix, new CombatModifiers());
            health.InitializeHardPoints(points);
            foreach (var point in points)
            {
                Assert.That(point.Health, Is.GreaterThan(0), point.HardPointType.ToString());
                point.ApplyDamage(point.Health);
                Assert.That(point.IsDestroyed, Is.True);
            }
            Assert.That(health.Hull, Is.EqualTo(data.Hull));
        }

        [Test]
        public void Registrations_ResolveOwnAssetsAndRequestedEconomy()
        {
            Assert.That(System.Enum.IsDefined(typeof(ShipType), 213), Is.True);
            var ships = AssetDatabase.LoadAssetAtPath<ShipsData>("Assets/Settings/Data/Ship/Shared/ShipsData.asset");
            Assert.That(ships.GetShipDataPath((ShipType)213), Is.EqualTo(AssetDatabase.AssetPathToGUID(DATA)));
            foreach (var path in new[] {VIEW, DATA})
                Assert.That(AddressableAssetSettingsDefaultObject.Settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(path)).address,
                    Is.EqualTo(System.IO.Path.GetFileNameWithoutExtension(path)));
            var array = new SerializedObject(AssetDatabase.LoadAssetAtPath<ScriptableObject>("Assets/Settings/Data/Factions/Empire/EmpireFaction.asset"))
                .FindProperty("ships.keyValue");
            var unit = Enumerable.Range(0, array.arraySize).Select(i => array.GetArrayElementAtIndex(i))
                .Single(p => p.FindPropertyRelative("key").intValue == 213).FindPropertyRelative("value");
            Assert.That(unit.FindPropertyRelative("<Price>k__BackingField").intValue, Is.EqualTo(9200));
            Assert.That(unit.FindPropertyRelative("<BuildTime>k__BackingField").intValue, Is.EqualTo(85));
            Assert.That(unit.FindPropertyRelative("<Icon>k__BackingField").objectReferenceValue,
                Is.EqualTo(AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Textures/Ui/Icons/ShipIcon/AllegianceIcon.png")));
            var matchups = new SerializedObject(unit.FindPropertyRelative("matchups").objectReferenceValue);
            Assert.That(matchups.FindProperty("strongAgainst").GetArrayElementAtIndex(1).FindPropertyRelative("label").stringValue,
                Is.EqualTo("Capital ships"));
        }

        [Test]
        public void HullSizeAndWeaponOrigins_MatchTheImportedGeometry()
        {
            var visual = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Models/Ships/Empire/Allegiance.prefab");
            var view = AssetDatabase.LoadAssetAtPath<GameObject>(VIEW);
            var data = AssetDatabase.LoadAssetAtPath<ShipData>(DATA);
            var bounds = VisibleBounds(visual);
            var collider = view.GetComponents<BoxCollider>().Single();
            Assert.That(Vector3.Distance(collider.size, bounds.size), Is.LessThan(.001f));
            Assert.That(bounds.size.z, Is.EqualTo(119.3834f * 2200 / 1137).Within(.02f));
            Assert.That(data.NavigationRadius, Is.GreaterThan(new Vector2(bounds.extents.x, bounds.extents.z).magnitude));
            var transforms = view.GetComponentsInChildren<Transform>(true);
            foreach (var weapon in view.GetComponentsInChildren<WeaponHardPoint>(true))
            {
                string socket = weapon.name.Substring("Battery_".Length);
                var turret = transforms.SingleOrDefault(t => t.name == socket + "_Turret");
                var candidates = turret == null ? transforms : turret.GetComponentsInChildren<Transform>(true);
                Assert.That(candidates.Any(t => t != weapon.transform && (t.name.Contains("FP") || t.name.StartsWith("FB_")) &&
                    Vector3.Distance(t.position, weapon.transform.position) < .0001f), Is.True, weapon.name);
            }
        }

        private static Bounds VisibleBounds(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.enabled).ToArray();
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
            return bounds;
        }

        [TestCase("Assets/Prefabs/Models/Ships/Empire/Allegiance.prefab")]
        [TestCase(VIEW)]
        [TestCase("Assets/Prefabs/Models/Wrecks/AllegianceWreckView.prefab")]
        [TestCase("Assets/Prefabs/Ui/Reinforcement/AllegianceReinforcementView.prefab")]
        public void SavedPrefabs_HaveNoBrokenReferencesOrMissingMeshes(string path)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.That(prefab.transform.localScale, Is.EqualTo(Vector3.one));
            foreach (var transform in prefab.GetComponentsInChildren<Transform>(true))
                Assert.That(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject), Is.Zero, transform.name);
            foreach (var component in prefab.GetComponentsInChildren<Component>(true))
            {
                var property = new SerializedObject(component).GetIterator();
                while (property.Next(true))
                    if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue == null)
                        Assert.That(property.objectReferenceEntityIdValue.Equals(default(EntityId)), Is.True, component.name + "." + property.propertyPath);
            }
            foreach (var renderer in prefab.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.enabled))
            {
                Assert.That(renderer.GetComponents<MeshFilter>().Single().sharedMesh, Is.Not.Null, renderer.name);
                Assert.That(renderer.sharedMaterials.All(m => m != null), Is.True, renderer.name);
            }
        }

        [TestCase("Republic/Venator")]
        [TestCase("Empire/VictoryII")]
        [TestCase("Rebellion/MonCalCruiser")]
        public void CombatSimulation_FiresAndReceivesDamageAgainstExistingCapitals(string opponentName)
        {
            var registry = BalanceInventory.BuildCombat();
            var allegiance = registry.Units.Single(u => AssetDatabase.GetAssetPath(u.Data) == DATA);
            var opponent = registry.Units.Single(u => AssetDatabase.GetAssetPath(u.Data) == "Assets/Settings/Data/Ship/" + opponentName + "ShipData.asset");
            using (var factory = new SimUnitFactory(registry, new BalanceDraft()))
            {
                var a = factory.Build(allegiance);
                var b = factory.Build(opponent);
                var setup = new BattleSetup {Distance = 200, TimeLimit = 120, AlphaStrike = true};
                setup.SideA.Add(new BattleFleetEntry {UnitId = a.Id, ArcFactor = .5f});
                setup.SideB.Add(new BattleFleetEntry {UnitId = b.Id, ArcFactor = .5f});
                var battle = new BattleSimulation(setup, new Dictionary<string, SimUnitDefinition> {{a.Id, a}, {b.Id, b}}, factory.Matrix, 213);
                while (!battle.IsComplete) battle.Step();
                Assert.That(battle.Result.Units[0].Shots, Is.GreaterThan(0));
                Assert.That(battle.Result.Units[0].DamageDealt, Is.GreaterThan(0));
                Assert.That(battle.Result.Units[1].DamageDealt, Is.GreaterThan(0));
                TestContext.WriteLine($"Allegiance vs {opponentName}: winner {battle.Result.Winner}, {battle.Result.Duration:F2}s");
            }
        }
    }
}

