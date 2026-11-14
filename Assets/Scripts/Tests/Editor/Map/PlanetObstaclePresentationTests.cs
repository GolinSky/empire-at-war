using System;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Entities.Planet;
using EmpireAtWar.Entities.Ship.Data;
using EmpireAtWar.Mvc;
using EmpireAtWar.Services.Factory;
using EmpireAtWar.Services.SuperWeapons;
using EmpireAtWar.Services.VContainer;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using VContainer;
using Object = UnityEngine.Object;
using LifetimeScope = VContainer.Unity.LifetimeScope;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Map")]
    public sealed class PlanetObstaclePresentationTests
    {
        private const string SETTINGS_PATH = "Assets/Settings/Data/Models/Map/MapGenerationSettings.asset";
        private const float OBSTACLE_RADIUS = 720f;
        private const float TOLERANCE = 0.1f;

        [TestCase(MapSize.Small, 0f)]
        [TestCase(MapSize.Medium, 0f)]
        [TestCase(MapSize.Large, 0f)]
        [TestCase(MapSize.Small, 360f)]
        [TestCase(MapSize.Medium, 720f)]
        [TestCase(MapSize.Large, 1440f)]
        public void Create_EveryPlanetKeepsSurfaceAndCloudsBelowShipCeiling(MapSize size, float obstacleRadius)
        {
            MapGenerationSettings settings = AssetDatabase.LoadAssetAtPath<MapGenerationSettings>(SETTINGS_PATH);
            using var containerSettings = new PopulatedVContainerSettings();
            GameObject root = new GameObject("Planet Ceiling Test");
            root.SetActive(false);
            try
            {
                PlanetTestScope owner = root.AddComponent<PlanetTestScope>();
                owner.autoRun = false;
                using (LifetimeScope.EnqueueParent(null)) owner.Build();
                root.SetActive(true);
                foreach (PlanetType type in Enum.GetValues(typeof(PlanetType)))
                {
                    var factory = new PlanetFactory(owner, settings);
                    try
                    {
                        PlanetView planet = factory.Create(type, size, new Vector3(123f, 0f, -456f), obstacleRadius);
                        Assert.That(planet.VisualTop, Is.LessThanOrEqualTo(settings.PlanetSurfaceCeiling + TOLERANCE),
                            $"{type}, {size}, obstacle {obstacleRadius}");
                        using SerializedObject view = new SerializedObject(planet);
                        foreach (string field in new[] { "planetRenderers", "cloudRenderers" })
                        {
                            SerializedProperty renderers = view.FindProperty(field);
                            for (int i = 0; i < renderers.arraySize; i++)
                            {
                                MeshRenderer renderer = (MeshRenderer)renderers.GetArrayElementAtIndex(i).objectReferenceValue;
                                Bounds bounds = renderer.localBounds;
                                foreach (Vector3 axis in new[] { Vector3.right, Vector3.up, Vector3.forward })
                                foreach (float sign in new[] { -1f, 1f })
                                {
                                    Vector3 surface = renderer.transform.TransformPoint(
                                        bounds.center + Vector3.Scale(axis, bounds.extents) * sign);
                                    Assert.That(surface.y, Is.LessThanOrEqualTo(settings.PlanetSurfaceCeiling + TOLERANCE),
                                        $"{type}: {field}[{i}] crosses the ceiling");
                                }
                            }
                        }
                        if (type == PlanetType.Kamino && obstacleRadius == 0f)
                            Assert.That(planet.transform.position.y,
                                Is.EqualTo(settings.GetPlanetPrefab(type).transform.position.y * settings.GetSize(size).PlanetScale)
                                    .Within(TOLERANCE), "A planet already below the ceiling must retain its authored depth.");
                    }
                    finally { factory.Dispose(); }
                }
                containerSettings.AssertProductionRootUntouched();
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void PlanetCeiling_IsBelowEveryShipHull()
        {
            MapGenerationSettings settings = AssetDatabase.LoadAssetAtPath<MapGenerationSettings>(SETTINGS_PATH);
            string[] ships = AssetDatabase.FindAssets("t:ShipData", new[] { "Assets/Settings/Data/Ship" });
            Assert.That(ships, Is.Not.Empty);
            foreach (string guid in ships)
            {
                ShipData ship = AssetDatabase.LoadAssetAtPath<ShipData>(AssetDatabase.GUIDToAssetPath(guid));
                Assert.That(settings.PlanetSurfaceCeiling, Is.LessThan(ship.Height + ship.HullBottom), ship.name);
            }
        }

        [TestCase(360f, 6f)]
        [TestCase(720f, 8.4f)]
        [TestCase(1440f, 10.5f)]
        public void EveryOrbitalPlanet_FitsFootprintBelowGameplayWithProportionalDepth(float radius, float mapScale)
        {
            MapGenerationSettings settings = AssetDatabase.LoadAssetAtPath<MapGenerationSettings>(SETTINGS_PATH);
            Vector3 center = new Vector3(123f, 50f, -456f);
            int checkedPlanets = 0;
            int cloudlessPlanets = 0;
            foreach (PlanetMapProfileEntry entry in settings.Profiles.Entries)
            {
                if (entry.Archetype.Kind != MapArchetypeKind.OrbitalRing) continue;
                checkedPlanets++;
                PlanetScope prefab = settings.GetPlanetPrefab(entry.Planet);
                Vector3 prefabPosition = prefab.transform.position;
                Vector3 prefabScale = prefab.transform.localScale;
                GameObject parent = new GameObject("Inactive Planet Fit Test");
                parent.SetActive(false);
                try
                {
                    PlanetScope instance = Object.Instantiate(prefab, parent.transform, false);
                    Assert.That(instance.gameObject.activeInHierarchy, Is.False);
                    instance.transform.position = center + Vector3.up * prefabPosition.y * mapScale;
                    instance.transform.localScale = prefabScale * mapScale;
                    using SerializedObject view = new SerializedObject(instance.View);
                    SerializedProperty surfaces = view.FindProperty("planetRenderers");
                    Assert.That(surfaces.arraySize, Is.GreaterThan(0), entry.Planet.ToString());
                    MeshRenderer first = (MeshRenderer)surfaces.GetArrayElementAtIndex(0).objectReferenceValue;
                    float originalHeight = first.transform.TransformPoint(first.localBounds.center).y - center.y;
                    float originalScale = instance.transform.localScale.x;
                    instance.View.FitObstacle(center, radius);
                    Vector3 visualCenter = first.transform.TransformPoint(first.localBounds.center);
                    Assert.That(visualCenter.x, Is.EqualTo(center.x).Within(TOLERANCE), entry.Planet.ToString());
                    Assert.That(visualCenter.z, Is.EqualTo(center.z).Within(TOLERANCE), entry.Planet.ToString());
                    Assert.That(visualCenter.y - center.y,
                        Is.EqualTo(originalHeight * instance.transform.localScale.x / originalScale).Within(TOLERANCE),
                        entry.Planet.ToString());
                    Assert.That(visualCenter.y + radius, Is.LessThan(center.y),
                        $"{entry.Planet}: surface and clouds must stay below gameplay");
                    float furthestSurface = 0f;
                    foreach (string field in new[] { "planetRenderers", "cloudRenderers" })
                    {
                        SerializedProperty renderers = view.FindProperty(field);
                        if (field == "cloudRenderers" && renderers.arraySize == 0) cloudlessPlanets++;
                        for (int i = 0; i < renderers.arraySize; i++)
                        {
                            MeshRenderer renderer = (MeshRenderer)renderers.GetArrayElementAtIndex(i).objectReferenceValue;
                            Assert.That(renderer, Is.Not.Null, $"{entry.Planet}: {field}[{i}]");
                            Bounds bounds = renderer.localBounds;
                            foreach (Vector3 axis in new[] { Vector3.right, Vector3.up, Vector3.forward })
                            foreach (float sign in new[] { -1f, 1f })
                            {
                                Vector3 localSurface = bounds.center + Vector3.Scale(axis, bounds.extents) * sign;
                                float distance = Vector3.Distance(renderer.transform.TransformPoint(localSurface), visualCenter);
                                Assert.That(distance, Is.LessThanOrEqualTo(radius + TOLERANCE),
                                    $"{entry.Planet}: {field}[{i}] extends past navigation obstacle");
                                furthestSurface = Mathf.Max(furthestSurface, distance);
                            }
                        }
                    }
                    Assert.That(furthestSurface, Is.EqualTo(radius).Within(TOLERANCE),
                        $"{entry.Planet}: visual body must fill the blocked footprint");
                    Assert.That(prefab.transform.position, Is.EqualTo(prefabPosition));
                    Assert.That(prefab.transform.localScale, Is.EqualTo(prefabScale));
                }
                finally { Object.DestroyImmediate(parent); }
            }
            Assert.That(checkedPlanets, Is.GreaterThan(0));
            Assert.That(cloudlessPlanets, Is.GreaterThan(0), "The capture must cover planets with no cloud layer.");
        }

        [Test]
        public void FitObstacle_MissingRequiredSurfaceReferenceFails()
        {
            MapGenerationSettings settings = AssetDatabase.LoadAssetAtPath<MapGenerationSettings>(SETTINGS_PATH);
            GameObject parent = new GameObject("Inactive Missing Surface Test");
            parent.SetActive(false);
            try
            {
                PlanetScope instance = Object.Instantiate(settings.GetPlanetPrefab(PlanetType.Bespin), parent.transform, false);
                using SerializedObject view = new SerializedObject(instance.View);
                view.FindProperty("planetRenderers").GetArrayElementAtIndex(0).objectReferenceValue = null;
                view.ApplyModifiedPropertiesWithoutUndo();
                Assert.Throws<UnassignedReferenceException>(() => instance.View.FitObstacle(Vector3.zero, OBSTACLE_RADIUS));
            }
            finally { Object.DestroyImmediate(parent); }
        }

        private sealed class PlanetTestScope : AssetLifetimeScope, IAssetService
        {
            protected override void Configure(IContainerBuilder builder)
            {
                Assets = this;
                builder.RegisterInstance<ISuperWeaponOriginRegistry>(new SuperWeaponOriginRegistry());
            }

            public TSource Load<TSource>(string key) where TSource : Object
            {
                Assert.That(key, Is.EqualTo(nameof(PlanetData)));
                return AssetDatabase.LoadAssetAtPath<TSource>("Assets/Settings/Data/Models/Planets/PlanetData.asset");
            }

            public TComponent LoadComponent<TComponent>(string key) where TComponent : Component =>
                throw new NotSupportedException();

            public GameObject LoadPrefab(string key) => throw new NotSupportedException();
        }
    }
}
