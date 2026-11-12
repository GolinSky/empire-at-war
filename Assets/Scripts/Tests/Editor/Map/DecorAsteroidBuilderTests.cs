using System;
using System.Collections.Generic;
using EmpireAtWar.Components.Environment;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Entities.Map.Generation;
using EmpireAtWar.Entities.Map.Hazards;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Random = System.Random;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Map")]
    public sealed class DecorAsteroidBuilderTests
    {
        private const string SETTINGS_PATH = "Assets/Settings/Data/Models/Map/MapGenerationSettings.asset";
        private const string SCENE_CONTEXT_PATH = "Assets/Prefabs/View/LifetimeScopes/SceneContext.prefab";

        [TestCase(MapSize.Small)]
        [TestCase(MapSize.Medium)]
        [TestCase(MapSize.Large)]
        public void Ring_IsDeterministicOutsideGameplayBoundsAndWithinBudget(MapSize mapSize)
        {
            MapGenerationSettings settings = AssetDatabase.LoadAssetAtPath<MapGenerationSettings>(SETTINGS_PATH);
            MapSizeSettings size = settings.GetSize(mapSize);
            DecorAsteroidBuilder builder = new DecorAsteroidBuilder(settings);
            for (int seed = 0; seed < 10; seed++)
            {
                FieldDensity density = new FieldDensity(Array.Empty<MapStation>(), Array.Empty<MapNode>(),
                    Array.Empty<MapLane>(), settings, size, new Random(seed), Array.Empty<HazardShape>());
                IReadOnlyList<AsteroidSpot> rocks = builder.Build(size, density, new Random(seed));
                IReadOnlyList<AsteroidSpot> repeat = builder.Build(size, density, new Random(seed));
                Assert.That(rocks.Count, Is.InRange(1, DecorAsteroidBuilder.MAX_INSTANCES));
                Assert.That(repeat.Count, Is.EqualTo(rocks.Count));
                for (int i = 0; i < rocks.Count; i++)
                {
                    Vector3 p = rocks[i].Position;
                    float outside = Mathf.Max(Mathf.Max(size.Bounds.Min.x - p.x, p.x - size.Bounds.Max.x),
                        Mathf.Max(size.Bounds.Min.y - p.z, p.z - size.Bounds.Max.y));
                    Assert.That(outside, Is.GreaterThan(0f).And.LessThan(size.EdgeMargin));
                    Assert.That(density.IsNaturalField(p), Is.True);
                    Assert.That(p, Is.EqualTo(repeat[i].Position));
                    Assert.That(rocks[i].Scale, Is.EqualTo(repeat[i].Scale));
                    Assert.That(rocks[i].Rotation, Is.EqualTo(repeat[i].Rotation));
                    Assert.That(rocks[i].Size, Is.EqualTo(repeat[i].Size));
                }
                TestContext.WriteLine($"{mapSize}, seed {seed}: {rocks.Count} exterior instances");
            }
        }

        [Test]
        public void BattlePrefab_BindsEveryDecorRockSize()
        {
            GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(SCENE_CONTEXT_PATH);
            MapLayoutView view = root.GetComponentInChildren<MapLayoutView>(true);
            SerializedObject map = new SerializedObject(view);
            var renderer = (DecorAsteroidRenderer)map.FindProperty("decorRenderer").objectReferenceValue;
            Assert.That(renderer, Is.Not.Null);
            SerializedProperty meshes = new SerializedObject(renderer).FindProperty("meshes");
            HashSet<AsteroidSize> sizes = new HashSet<AsteroidSize>();
            for (int i = 0; i < meshes.arraySize; i++)
            {
                var entry = (DecorAsteroidMesh)meshes.GetArrayElementAtIndex(i).boxedValue;
                sizes.Add(entry.Size);
                Assert.That(entry.Mesh, Is.Not.Null);
                Assert.That(entry.Materials.Length, Is.EqualTo(entry.Mesh.subMeshCount));
                Assert.That(entry.Materials, Has.All.Not.Null);
                Assert.That(entry.LocalTransform.determinant, Is.Not.Zero);
            }
            Assert.That(sizes, Is.EquivalentTo(Enum.GetValues(typeof(AsteroidSize))));
        }
    }
}
