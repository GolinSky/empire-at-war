using Object = UnityEngine.Object;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using EmpireAtWar.Repository.Data;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.MiniMap;
using EmpireAtWar.Entities.Ship.Data;
using EmpireAtWar.Models.MiniMap;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Models.SkirmishCamera;
using EmpireAtWar.Repository;
using EmpireAtWar.Services.Player;
using EmpireAtWar.ViewComponents.Health;
using NUnit.Framework;
using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class AddressableHandleLoaderTests
    {
        [Test]
        public void EveryMappedAssetExists()
        {
            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.ScriptableObject>(
                "Assets/Settings/AssetMappingData.asset");
            var entries = new SerializedObject(asset).FindProperty("assetMappings.keyValue");
            for (int i = 0; i < entries.arraySize; i++)
            {
                var entry = entries.GetArrayElementAtIndex(i);
                string guid = entry.FindPropertyRelative("value.m_AssetGUID").stringValue;
                Assert.That(AssetDatabase.GUIDToAssetPath(guid), Is.Not.Empty,
                    entry.FindPropertyRelative("key").stringValue);
            }
        }

        [Test]
        public void FailedMappingLoadRejectsEverySubsequentRequest()
        {
            var loader = new AddressableHandleLoader();
            typeof(AddressableHandleLoader).GetField("_mappingLoadAttempted",
                BindingFlags.Instance | BindingFlags.NonPublic).SetValue(loader, true);

            for (int attempt = 0; attempt < 2; attempt++)
            {
                var exception = Assert.Throws<InvalidOperationException>(
                    () => loader.Load<UnityEngine.Object>("MissingAsset"));
                Assert.That(exception.Message, Does.Contain("AssetMappingData"));
            }
        }
    }
}


namespace EmpireAtWar.Tests.Editor
{
    public sealed class MiniMapUiLifecycleTests
    {
        private const string PREFAB_PATH = "Assets/Prefabs/Ui/MiniMap/MiniMapUi.prefab";

        [Test]
        public void MiniMapSurfaceHasNoMissingPrefabAndKeepsItsBindings()
        {
            var root = PrefabUtility.LoadPrefabContents(PREFAB_PATH);
            try
            {
                var surface = root.transform.Find("Panel/MiniMap");
                Assert.That(surface, Is.Not.Null);
                Assert.That(PrefabUtility.IsPrefabAssetMissing(surface.gameObject), Is.False);
                var ui = new SerializedObject(root.GetComponent<MiniMapUi>());
                foreach (string field in new[] { "miniMapRectTransform", "tooltipHover", "iconParent",
                             "mapImage", "cameraFootprintView", "obstacleView", "moveTargetView", "fogImage" })
                    Assert.That(ui.FindProperty(field).objectReferenceValue, Is.Not.Null, field);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ExplicitInitializationSubscribesOnceAndBothTeardownOrdersUnsubscribe(bool destroyFirst)
        {
            MiniMapData model = ScriptableObject.CreateInstance<MiniMapData>();
            model.MapRange = new Vector2Range();
            typeof(Vector2Range).BaseType.GetField("<Min>k__BackingField",
                BindingFlags.Instance | BindingFlags.NonPublic).SetValue(model.MapRange, Vector2.zero);
            typeof(Vector2Range).BaseType.GetField("<Max>k__BackingField",
                BindingFlags.Instance | BindingFlags.NonPublic).SetValue(model.MapRange, Vector2.one * 100f);
            MiniMapUi prefab = AssetDatabase.LoadAssetAtPath<MiniMapUi>(PREFAB_PATH);
            MiniMapUi ui = UnityEngine.Object.Instantiate(prefab);
            try
            {
                ui.AssignPresenter(new PresenterStub());
                ui.SetData(model, new ColorsStub());
                ui.Initialize();
                ui.Initialize();
                foreach (string eventName in new[] { "OnMarkAdded", "OnMarkerAdded", "OnMarkerRemoved" })
                    Assert.That(SubscriberCount(model, eventName), Is.EqualTo(1), eventName);

                if (destroyFirst) UnityEngine.Object.DestroyImmediate(ui.gameObject);
                ui.Dispose();
                ui.Dispose();

                foreach (string eventName in new[] { "OnMarkAdded", "OnMarkerAdded", "OnMarkerRemoved" })
                    Assert.That(SubscriberCount(model, eventName), Is.Zero, eventName);
            }
            finally
            {
                if (ui != null) UnityEngine.Object.DestroyImmediate(ui.gameObject);
                UnityEngine.Object.DestroyImmediate(model);
            }
        }

        private static int SubscriberCount(MiniMapData model, string eventName)
        {
            var handlers = (Delegate)typeof(MiniMapData)
                .GetField(eventName, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(model);
            return handlers == null ? 0 : handlers.GetInvocationList().Length;
        }

        private sealed class PresenterStub : IMiniMapPresenter
        {
            public void MoveCamera(Vector3 point) { }
            public void OrderMove(Vector3 point) { }
        }

        private sealed class ColorsStub : IPlayerColors
        {
            public Color GetColor(PlayerId owner) => Color.white;
        }
    }
}


namespace EmpireAtWar.Tests.Editor
{
    public sealed class ShipAssetIntegrityTests
    {
        [TestCase("AcclamatorAssault")]
        [TestCase("VictoryI")]
        [TestCase("VictoryII")]
        [TestCase("ISDIII")]
        public void RepairedShip_EveryHardPointIsTargetableAndHasHealth(string ship)
        {
            GameObject view = AssetDatabase.LoadAssetAtPath<GameObject>(
                $"Assets/Prefabs/Models/Ships/Empire/{ship}ShipView.prefab");
            ShipData data = AssetDatabase.LoadAssetAtPath<ShipData>(
                $"Assets/Settings/Data/Ship/Empire/{ship}ShipData.asset");
            HardPoint[] hardPoints = view.GetComponentsInChildren<HardPoint>(true);
            HealthComponent health = view.GetComponentsInChildren<HealthComponent>(true).Single();

            Assert.That(hardPoints, Is.Not.Empty);
            Assert.That(health.ShipUnits, Is.EquivalentTo(hardPoints));
            Assert.That(health.ShipUnits.Distinct().Count(), Is.EqualTo(health.ShipUnits.Count));
            foreach (HardPoint hardPoint in hardPoints)
            {
                HardPointHealth entry = data.HardPointHealth.Single(h => h.HardPointType == hardPoint.HardPointType);
                Assert.That(entry.Health, Is.GreaterThan(0), hardPoint.name);
            }
        }

        [TestCase("Republic", "Arquitens")]
        [TestCase("Republic", "HeavyDreadnought")]
        [TestCase("Separatist", "Munificent")]
        [TestCase("Separatist", "Recusant")]
        public void NonCarrier_HasNoPhantomHangarHealth(string faction, string ship)
        {
            ShipData data = AssetDatabase.LoadAssetAtPath<ShipData>(
                $"Assets/Settings/Data/Ship/{faction}/{ship}ShipData.asset");
            GameObject view = AssetDatabase.LoadAssetAtPath<GameObject>(
                $"Assets/Prefabs/Models/Ships/{faction}/{ship}ShipView.prefab");

            Assert.That(data.HangarBays, Is.Empty);
            Assert.That(data.HardPointHealth.Any(h => h.HardPointType == HardPointType.Hangar), Is.False);
            Assert.That(view.GetComponentsInChildren<HardPoint>(true)
                .Any(h => h.HardPointType == HardPointType.Hangar), Is.False);
        }

        [Test]
        public void Endurance_ContainsOnlyOperationalBays()
        {
            ShipData data = AssetDatabase.LoadAssetAtPath<ShipData>(
                "Assets/Settings/Data/Ship/Rebellion/EnduranceShipData.asset");

            Assert.That(data.HangarBays.Count, Is.EqualTo(2));
            Assert.That(data.HangarBays.All(b => b.MaxActive > 0 && b.Reserve > 0), Is.True);
        }

        [Test]
        public void Arquitens_UsesItsOwnWreckPrefab()
        {
            ShipData data = AssetDatabase.LoadAssetAtPath<ShipData>(
                "Assets/Settings/Data/Ship/Republic/ArquitensShipData.asset");

            Assert.That(data.Wreck, Is.Not.Null);
            Assert.That(data.Wreck.Prefab, Is.Not.Null);
            Assert.That(AssetDatabase.GetAssetPath(data.Wreck.Prefab),
                Is.EqualTo("Assets/Prefabs/Models/Wrecks/ArquitensWreckView.prefab"));
        }
    }
}
public static class FocusedBugChecks {
 public static string Main() {
  var loader = new EmpireAtWar.Tests.Editor.AddressableHandleLoaderTests();
  loader.EveryMappedAssetExists(); loader.FailedMappingLoadRejectsEverySubsequentRequest();
  var ui = new EmpireAtWar.Tests.Editor.MiniMapUiLifecycleTests();
  ui.MiniMapSurfaceHasNoMissingPrefabAndKeepsItsBindings();
  ui.ExplicitInitializationSubscribesOnceAndBothTeardownOrdersUnsubscribe(false);
  ui.ExplicitInitializationSubscribesOnceAndBothTeardownOrdersUnsubscribe(true);
  var ships = new EmpireAtWar.Tests.Editor.ShipAssetIntegrityTests();
  foreach (var ship in new[] { "AcclamatorAssault", "VictoryI", "VictoryII", "ISDIII" }) ships.RepairedShip_EveryHardPointIsTargetableAndHasHealth(ship);
  ships.NonCarrier_HasNoPhantomHangarHealth("Republic", "Arquitens");
  ships.NonCarrier_HasNoPhantomHangarHealth("Republic", "HeavyDreadnought");
  ships.NonCarrier_HasNoPhantomHangarHealth("Separatist", "Munificent");
  ships.NonCarrier_HasNoPhantomHangarHealth("Separatist", "Recusant");
  ships.Endurance_ContainsOnlyOperationalBays(); ships.Arquitens_UsesItsOwnWreckPrefab();
  return "15 focused NUnit assertion checks passed against loaded project assets/runtime.";
 }
}

namespace EmpireAtWar.Repository
{
    public class AddressableHandleLoader : IAssetHandleLoader
    {
        private const string MAPPING_DATA_KEY = nameof(AssetMappingData);

        private AssetMappingData _mappingData;

        private bool _mappingLoadAttempted;

        public AsyncOperationHandle<TSource> Load<TSource>(string key) where TSource : Object
        {
            return Addressables.LoadAssetAsync<TSource>(ResolveKey(key));
        }

        private string ResolveKey(string key)
        {
            if (key == MAPPING_DATA_KEY)
                return key;

            AssetMappingData mappingData = GetMappingData();
            return mappingData.GetAssetKey(key);
        }

        // The mapping handle is kept for the loader's lifetime, which is the application's.
        private AssetMappingData GetMappingData()
        {
            if (!_mappingLoadAttempted)
            {
                _mappingLoadAttempted = true;
                _mappingData = Addressables
                    .LoadAssetAsync<AssetMappingData>(MAPPING_DATA_KEY)
                    .WaitForCompletion();
            }

            if (_mappingData == null)
                throw new System.InvalidOperationException($"[{nameof(AddressableHandleLoader)}] Required {MAPPING_DATA_KEY} could not be loaded.");

            return _mappingData;
        }
    }
}
