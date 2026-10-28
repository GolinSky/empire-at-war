using System.Collections;
using System.Reflection;
using System.Threading;
using EmpireAtWar.Components.Environment;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Entities.Planet;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.FogOfWar;
using EmpireAtWar.Services.ShipSpawning;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Map")]
    public sealed class BattleMapLoaderHazardTests
    {
        private const double LOAD_TIMEOUT_SECONDS = 60d;

        // Loading deliberately yields player frames; Unity 6000.4 does not reliably resume these in EditMode.
        [UnitySetUp]
        public IEnumerator EnterPlayerLoop()
        {
            yield return new EnterPlayMode();
        }

        [UnityTearDown]
        public IEnumerator ExitPlayerLoop()
        {
            yield return new ExitPlayMode();
        }

        [UnityTest]
        public IEnumerator LoadAsync_PublishesAndBuildsGeneratedHazardsAfterSiteFiltering()
        {
            GameObject root = new GameObject("Hazard loader regression");
            root.SetActive(false);
            MapGenerationSettings settings = Object.Instantiate(AssetDatabase.LoadAssetAtPath<MapGenerationSettings>(
                "Assets/Settings/Data/Models/Map/MapGenerationSettings.asset"));
            GameData game = ScriptableObject.CreateInstance<GameData>();
            using CancellationTokenSource cancellation = new CancellationTokenSource();
            try
            {
                SerializedObject serializedSettings = new SerializedObject(settings);
                SerializedProperty sizes = serializedSettings.FindProperty("sizes").FindPropertyRelative("keyValue");
                for (int i = 0; i < sizes.arraySize; i++)
                    sizes.GetArrayElementAtIndex(i).FindPropertyRelative("value")
                        .FindPropertyRelative("<RockDensity>k__BackingField").floatValue = 0f;
                serializedSettings.ApplyModifiedPropertiesWithoutUndo();
                GameObject scene = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/Prefabs/View/LifetimeScopes/SceneContext.prefab"), root.transform);
                MapLayoutView view = scene.GetComponentInChildren<MapLayoutView>(true);
                typeof(MapLayoutView).GetMethod("Construct", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(view, new object[] { new NoFogOfWar(), settings, scene.GetComponentInChildren<EmpireAtWar.Services.Camera.CameraService>(true) });
                BattleDefinitionBuilder builder = SkirmishPreset.Create(TestPlayers.CreateTeamGame().Players,
                    PlanetType.Coruscant, MapSize.Medium, BattleVictoryCondition.DestroyEnemyFleet, 1000f, true, 7919);
                builder.Features = BattleFeatures.None;
                game.Battle = builder.Build();
                BattleMapLoader loader = new BattleMapLoader(settings, view, game, new MapModel(), game.Battle,
                    new UnusedSpawnClearance());
                MapObserver observer = new MapObserver();
                loader.AddObserver(observer);

                var completion = loader.LoadAsync(cancellation.Token).GetAwaiter();
                double deadline = EditorApplication.timeSinceStartup + LOAD_TIMEOUT_SECONDS;
                while (!completion.IsCompleted && EditorApplication.timeSinceStartup < deadline)
                {
                    EditorApplication.QueuePlayerLoopUpdate();
                    yield return null;
                }
                if (!completion.IsCompleted)
                {
                    cancellation.Cancel();
                    double cancellationDeadline = EditorApplication.timeSinceStartup + 5d;
                    while (!completion.IsCompleted && EditorApplication.timeSinceStartup < cancellationDeadline)
                    {
                        EditorApplication.QueuePlayerLoopUpdate();
                        yield return null;
                    }
                    Assert.Fail($"Battle map loading did not complete within {LOAD_TIMEOUT_SECONDS} seconds.");
                }
                completion.GetResult();

                Assert.That(observer.Map, Is.Not.Null);
                Assert.That(observer.Map.Layout.Sites, Is.Empty, "Exercise the actual loader's site filtering.");
                Assert.That(observer.Map.Layout.EdgeMargin, Is.GreaterThan(0f), "Filtering must preserve exterior terrain settings.");
                Assert.That(observer.Map.Layout.Hazards, Is.Not.Empty,
                    "The published map must preserve hazards through the loader's layout reconstruction.");
                Assert.That(scene.GetComponentsInChildren<HazardCloudView>(true).Length,
                    Is.EqualTo(observer.Map.Layout.Hazards.Count), "Every published hazard needs a spawned visual.");
            }
            finally
            {
                cancellation.Cancel();
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(game);
                Object.DestroyImmediate(settings);
            }
        }

        private sealed class MapObserver : IObserver<BattleMap>
        {
            public BattleMap Map { get; private set; }
            public void UpdateState(BattleMap map) => Map = map;
        }

        private sealed class UnusedSpawnClearance : IShipSpawnClearance
        {
            public float GetPlanarRadius(ShipType shipType) => throw new System.InvalidOperationException();
            public bool IsClear(PlayerId owner, ShipType shipType, Vector3 position) => throw new System.InvalidOperationException();
            public void ReserveLanding(object ship, PlayerId owner, ShipType shipType, Vector3 position) => throw new System.InvalidOperationException();
            public void ReleaseLanding(object ship) => throw new System.InvalidOperationException();
        }
    }
}

