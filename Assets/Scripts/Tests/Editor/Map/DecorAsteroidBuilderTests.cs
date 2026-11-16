using System;
using System.Collections.Generic;
using System.Reflection;
using EmpireAtWar.Components.Environment;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Entities.Map.Generation;
using EmpireAtWar.Entities.Map.Hazards;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = System.Random;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Map")]
    public sealed class DecorAsteroidBuilderTests
    {
        private const string SETTINGS_PATH = "Assets/Settings/Data/Models/Map/MapGenerationSettings.asset";
        private const string SCENE_CONTEXT_PATH = "Assets/Prefabs/View/LifetimeScopes/SceneContext.prefab";

        [Test]
        public void Configure_KeepsOcclusionWithWorldBoundsAndReleasesReplacedMaterials()
        {
            GameObject owner = new GameObject("Decor configuration regression");
            Mesh mesh = new Mesh { bounds = new Bounds(Vector3.right, Vector3.one * 20f) };
            try
            {
                DecorAsteroidRenderer renderer = owner.AddComponent<DecorAsteroidRenderer>();
                Material source = AssetDatabase.LoadAssetAtPath<Material>(
                    "Assets/Art/Materials/Models/Asteroids/RockyPack/RockyPack_Decor.mat");
                const BindingFlags FIELDS = BindingFlags.Instance | BindingFlags.NonPublic;
                typeof(DecorAsteroidRenderer).GetField("meshes", FIELDS).SetValue(renderer, new[]
                {
                    new DecorAsteroidMesh(AsteroidSize.Debris, mesh, new[] { source }, Matrix4x4.identity),
                    new DecorAsteroidMesh(AsteroidSize.Large, mesh, new[] { source }, Matrix4x4.Scale(Vector3.one * 2f))
                });
                AsteroidSpot[] rocks =
                {
                    new AsteroidSpot(Vector3.right * 999f, Vector3.zero, AsteroidSize.Large, 1f),
                    new AsteroidSpot(Vector3.right * 1001f, Vector3.zero, AsteroidSize.Debris, 1f),
                    new AsteroidSpot(Vector3.right * 4000f, Vector3.zero, AsteroidSize.Large, 0.5f),
                    new AsteroidSpot(Vector3.right * 9000f, Vector3.zero, AsteroidSize.Debris, 2f)
                };
                Bounds[] expectedBounds = new Bounds[rocks.Length];
                for (int i = 0; i < rocks.Length; i++)
                {
                    float scale = rocks[i].Scale * (rocks[i].Size == AsteroidSize.Large ? 2f : 1f);
                    expectedBounds[i] = new Bounds(rocks[i].Position + mesh.bounds.center * scale,
                        mesh.bounds.size * scale);
                }
                float[] expectedOcclusion = DecorAsteroidOcclusion.Calculate(expectedBounds);
                renderer.Configure(rocks, null);
                int count = 0;
                foreach (Array group in (Array)typeof(DecorAsteroidRenderer).GetField("_cells", FIELDS).GetValue(renderer))
                    foreach (object cell in group)
                    {
                        Type type = cell.GetType();
                        var matrices = (Matrix4x4[])type.GetProperty("Matrices").GetValue(cell);
                        var bounds = (Bounds[])type.GetProperty("InstanceBounds").GetValue(cell);
                        var occlusion = (float[])type.GetProperty("Occlusion").GetValue(cell);
                        Assert.That(occlusion.Length, Is.EqualTo(matrices.Length));
                        for (int i = 0; i < matrices.Length; i++)
                        {
                            Vector3 position = matrices[i].GetColumn(3);
                            int index = Array.FindIndex(rocks, rock => rock.Position == position);
                            Assert.That(index, Is.GreaterThanOrEqualTo(0));
                            Assert.That(bounds[i], Is.EqualTo(expectedBounds[index]));
                            Assert.That(occlusion[i], Is.EqualTo(expectedOcclusion[index]).Within(0.00001f));
                            count++;
                        }
                    }
                Assert.That(count, Is.EqualTo(rocks.Length));
                var previous = (Material[][])typeof(DecorAsteroidRenderer).GetField("_materials", FIELDS).GetValue(renderer);
                renderer.Configure(Array.Empty<AsteroidSpot>(), null);
                foreach (Material[] group in previous)
                    foreach (Material material in group) Assert.That(material == null, Is.True);
                foreach (Array group in (Array)typeof(DecorAsteroidRenderer).GetField("_cells", FIELDS).GetValue(renderer))
                    Assert.That(group.Length, Is.Zero);
                Assert.That(source != null, Is.True, "Reconfiguration must not destroy the source material.");
            }
            finally
            {
                Object.DestroyImmediate(owner);
                Object.DestroyImmediate(mesh);
            }
        }

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
            Assert.That(meshes.arraySize, Is.EqualTo(10));
            HashSet<AsteroidSize> sizes = new HashSet<AsteroidSize>();
            HashSet<Mesh> uniqueMeshes = new HashSet<Mesh>();
            HashSet<Material> materials = new HashSet<Material>();
            for (int i = 0; i < meshes.arraySize; i++)
            {
                var entry = (DecorAsteroidMesh)meshes.GetArrayElementAtIndex(i).boxedValue;
                sizes.Add(entry.Size);
                Assert.That(entry.Mesh, Is.Not.Null);
                Assert.That(uniqueMeshes.Add(entry.Mesh), Is.True, entry.Mesh.name);
                Assert.That(entry.LowDetailMesh, Is.Not.Null);
                Assert.That(entry.LowDetailMesh.GetIndexCount(0), Is.LessThan(entry.Mesh.GetIndexCount(0)));
                Assert.That(entry.Materials.Length, Is.EqualTo(entry.Mesh.subMeshCount));
                Assert.That(entry.Materials, Has.All.Not.Null);
                foreach (Material material in entry.Materials) materials.Add(material);
                Assert.That(entry.LocalTransform.determinant, Is.Not.Zero);
            }
            Assert.That(sizes, Is.EquivalentTo(Enum.GetValues(typeof(AsteroidSize))));
            Assert.That(materials.Count, Is.EqualTo(1));
            Material gameplayMaterial = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/Art/Materials/Models/Asteroids/RockyPack/RockyPack_Surface.mat");
            foreach (Material material in materials)
            {
                Assert.That(material, Is.Not.SameAs(gameplayMaterial));
                Assert.That(material.shader.name, Is.EqualTo("EmpireAtWar/Environment/Decor Asteroid"));
                Assert.That(material.enableInstancing, Is.True);
                Assert.That(material.GetTexture("_BaseMap"), Is.SameAs(gameplayMaterial.GetTexture("_BaseMap")));
                Assert.That(material.GetTexture("_BumpMap"), Is.SameAs(gameplayMaterial.GetTexture("_BumpMap")));
                Assert.That(material.GetTexture("_OcclusionMap"), Is.SameAs(gameplayMaterial.GetTexture("_OcclusionMap")));
            }
            Assert.That(gameplayMaterial.shader.name, Is.EqualTo("Universal Render Pipeline/Lit"));
        }
    }
}
