using EmpireAtWar.Entities.Map;
using EmpireAtWar.Entities.Planet;
using EmpireAtWar.Services.VContainer;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Map")]
    public sealed class PlanetObstaclePresentationTests
    {
        private const string SETTINGS_PATH = "Assets/Settings/Data/Models/Map/MapGenerationSettings.asset";
        private const float OBSTACLE_RADIUS = 720f;
        private const float TOLERANCE = 0.1f;

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
    }
}
