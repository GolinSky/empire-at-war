using System;
using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Components.Environment;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Entities.Ship.Data;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace EmpireAtWar.Tests.Editor
{
    [Category("MapSpaceDust")]
    public sealed class MapSpaceDustTests
    {
        private const string PREFAB_PATH = "Assets/Prefabs/Vfx/MapSpaceDust.prefab";
        private const string SCENE_CONTEXT_PATH = "Assets/Prefabs/View/LifetimeScopes/SceneContext.prefab";
        private const int MAX_VERTEX_BUDGET = 18000;

        [Test]
        public void BattlePrefab_BindsDustAndItsRenderAssets()
        {
            GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(SCENE_CONTEXT_PATH);
            MapLayoutView map = root.GetComponentInChildren<MapLayoutView>(true);
            var mapProperties = new SerializedObject(map);
            var dust = (MapSpaceDust)mapProperties.FindProperty("spaceDust").objectReferenceValue;
            Assert.That(dust, Is.Not.Null, "Generated maps must initialize the ambient field.");
            Assert.That(dust.transform.IsChildOf(root.transform), Is.True);

            var dustProperties = new SerializedObject(dust);
            var renderer = (MeshRenderer)dustProperties.FindProperty("fieldRenderer").objectReferenceValue;
            Assert.That(renderer, Is.Not.Null);
            Assert.That(renderer.sharedMaterial, Is.Not.Null);
            Assert.That(renderer.sharedMaterial.shader.name, Is.EqualTo("EmpireAtWar/Vfx/Map Space Dust"));
            Assert.That(renderer.sharedMaterial.GetTexture("_MainTex"), Is.Not.Null);
            Assert.That(renderer.shadowCastingMode, Is.EqualTo(ShadowCastingMode.Off));
            Assert.That(renderer.receiveShadows, Is.False);
            Assert.That(renderer.GetComponent<MeshFilter>().sharedMesh, Is.Not.Null);
        }

        [TestCase(4000f)]
        [TestCase(6000f)]
        [TestCase(9000f)]
        public void Configure_UsesWorldMapBoundsAndPreservesFixedGeometry(float side)
        {
            GameObject instance = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH));
            try
            {
                instance.transform.SetPositionAndRotation(new Vector3(173f, 48f, -91f), Quaternion.Euler(0f, 31f, 0f));
                MapSpaceDust dust = instance.GetComponent<MapSpaceDust>();
                MeshRenderer renderer = instance.GetComponent<MeshRenderer>();
                Mesh mesh = instance.GetComponent<MeshFilter>().sharedMesh;
                var min = new Vector2(-side * 0.5f + 250f, -side * 0.5f - 400f);
                Vector2 max = min + Vector2.one * side;

                dust.Configure(min, max);

                var properties = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(properties);
                Assert.That(properties.GetVector("_MapBounds"), Is.EqualTo(new Vector4(min.x, min.y, max.x, max.y)));
                Vector4 altitude = properties.GetVector("_HeightRange");
                Assert.That(renderer.enabled, Is.True);
                Assert.That(renderer.bounds.center, Is.EqualTo(new Vector3(
                    (min.x + max.x) * 0.5f, (altitude.x + altitude.y) * 0.5f, (min.y + max.y) * 0.5f)));

                var cards = new List<Vector4>();
                mesh.GetUVs(1, cards);
                float cornerRadius = cards.Max(card => card.x) * Mathf.Sqrt(2f);
                Assert.That(renderer.bounds.min.y, Is.LessThanOrEqualTo(altitude.x - cornerRadius));
                Assert.That(renderer.bounds.max.y, Is.GreaterThanOrEqualTo(altitude.y + cornerRadius));
                Assert.That(renderer.bounds.min.x, Is.LessThanOrEqualTo(min.x - cornerRadius));
                Assert.That(renderer.bounds.max.z, Is.GreaterThanOrEqualTo(max.y + cornerRadius));

                dust.Configure(min + Vector2.one * 100f, max + Vector2.one * 100f);
                Assert.That(instance.GetComponent<MeshFilter>().sharedMesh, Is.SameAs(mesh));
                Assert.That(mesh.vertexCount, Is.LessThanOrEqualTo(MAX_VERTEX_BUDGET));
                renderer.GetPropertyBlock(properties);
                Assert.That(properties.GetVector("_MapBounds").x, Is.EqualTo(min.x + 100f));
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void AuthoredVolume_CoversEveryShipTierWithHullMargin()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
            var properties = new SerializedObject(prefab.GetComponent<MapSpaceDust>());
            Vector2 altitude = properties.FindProperty("heightRange").vector2Value;
            string[] tierAssets = AssetDatabase.FindAssets("t:ShipHeightTiersData", new[] { "Assets/Settings/Data/Ship" });
            Assert.That(tierAssets, Is.Not.Empty);
            foreach (string guid in tierAssets)
            {
                ShipHeightTiersData data = AssetDatabase.LoadAssetAtPath<ShipHeightTiersData>(AssetDatabase.GUIDToAssetPath(guid));
                foreach (ShipHeightTier tier in Enum.GetValues(typeof(ShipHeightTier)))
                {
                    float height = data.GetHeight(tier);
                    Assert.That(altitude.x, Is.LessThanOrEqualTo(height - 50f), $"{data.name}/{tier}: lower hull margin");
                    Assert.That(altitude.y, Is.GreaterThanOrEqualTo(height + 50f), $"{data.name}/{tier}: upper hull margin");
                }
            }
        }

        [Test]
        public void BakedField_HasBothLayersAcrossTheFullAltitudeRangeWithinBudget()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
            Mesh mesh = prefab.GetComponent<MeshFilter>().sharedMesh;
            Assert.That(mesh.vertexCount, Is.InRange(4, MAX_VERTEX_BUDGET));
            Assert.That(mesh.subMeshCount, Is.EqualTo(1), "Keep both layers in one draw.");
            Assert.That(mesh.triangles.Length, Is.EqualTo(mesh.vertexCount / 4 * 6));
            Vector3[] seeds = mesh.vertices;
            var cards = new List<Vector4>();
            mesh.GetUVs(1, cards);
            Assert.That(cards.Count, Is.EqualTo(seeds.Length));
            foreach (int kind in new[] { 0, 1 })
            {
                float[] heights = Enumerable.Range(0, seeds.Length / 4)
                    .Select(index => index * 4)
                    .Where(index => cards[index].y == kind)
                    .Select(index => seeds[index].y).ToArray();
                Assert.That(heights, Is.Not.Empty, $"Missing layer {kind}");
                Assert.That(heights.Min(), Is.InRange(0f, 0.1f));
                Assert.That(heights.Max(), Is.InRange(0.9f, 1f));
                for (int band = 0; band < 8; band++)
                    Assert.That(heights.Any(height => height >= band / 8f && height < (band + 1) / 8f),
                        Is.True, $"Layer {kind} has no particles in altitude band {band}");
            }
        }
    }
}
