using System.Collections.Generic;
using EmpireAtWar.Editor;
using EmpireAtWar.Editor.AI;
using EmpireAtWar.Entities.Ship.Data;
using EmpireAtWar.Entities.Ship.EntityFacades;
using EmpireAtWar.Services.Vision;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using ShipView = EmpireAtWar.Ship.Ship;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Vision")]
    public sealed class VisionHullGeometryTests
    {
        private static readonly Vector2[] _wedge =
        {
            new Vector2(-20f, -50f), new Vector2(20f, -50f), new Vector2(0f, 50f)
        };

        [Test]
        public void MeasureHull_PersistentPrefabUsesAuthoredActivation()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/Models/Ships/Empire/View/AcclamatorAssaultShipView.prefab");
            Assert.That(EditorUtility.IsPersistent(prefab), Is.True);

            Assert.That(ShipVisionHullBaker.MeasureHull(prefab).Length, Is.GreaterThanOrEqualTo(3));
        }

        [Test]
        public void MeasureHull_ExcludesDisabledRenderersAndInactiveAncestors()
        {
            GameObject root = new GameObject(nameof(VisionHullGeometryTests));
            Mesh mesh = new Mesh
            {
                vertices = new[] { Vector3.zero, Vector3.right, Vector3.forward },
                triangles = new[] { 0, 1, 2 }
            };
            try
            {
                ShipView ship = root.AddComponent<ShipView>();
                MeshRenderer visible = CreateHullRenderer(root.transform, mesh, Vector3.zero);
                MeshRenderer disabled = CreateHullRenderer(root.transform, mesh, Vector3.right * 100f);
                disabled.enabled = false;
                GameObject inactiveParent = new GameObject("Inactive helper parent");
                inactiveParent.transform.SetParent(root.transform, false);
                MeshRenderer inactive = CreateHullRenderer(inactiveParent.transform, mesh, Vector3.forward * 100f);
                inactiveParent.SetActive(false);
                Assert.That(inactive.gameObject.activeSelf, Is.True);
                SerializedObject serialized = new SerializedObject(ship);
                SerializedProperty renderers = serialized.FindProperty("explosionHullRenderers");
                renderers.arraySize = 3;
                renderers.GetArrayElementAtIndex(0).objectReferenceValue = visible;
                renderers.GetArrayElementAtIndex(1).objectReferenceValue = disabled;
                renderers.GetArrayElementAtIndex(2).objectReferenceValue = inactive;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                Vector2[] hull = ShipVisionHullBaker.MeasureHull(root);

                Assert.That(hull.Length, Is.EqualTo(3));
                foreach (Vector2 point in hull)
                {
                    Assert.That(point.x, Is.InRange(0f, 1f));
                    Assert.That(point.y, Is.InRange(0f, 1f));
                }
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(mesh);
            }
        }

        private static MeshRenderer CreateHullRenderer(Transform parent, Mesh mesh, Vector3 position)
        {
            GameObject hull = new GameObject("Hull");
            hull.transform.SetParent(parent, false);
            hull.transform.localPosition = position;
            hull.AddComponent<MeshFilter>().sharedMesh = mesh;
            return hull.AddComponent<MeshRenderer>();
        }

        [Test]
        public void SavedShipData_EveryMappedShipHasNondegenerateBakedHull()
        {
            string[] guids = AssetDatabase.FindAssets("t:" + nameof(ShipData),
                new[] { "Assets/Settings/Data/Ship" });
            Assert.That(guids, Is.Not.Empty);
            foreach (string guid in guids)
            {
                ShipData data = AssetDatabase.LoadAssetAtPath<ShipData>(AssetDatabase.GUIDToAssetPath(guid));
                GameObject prefab = WeaponLoadoutBaker.LoadShipView(data);
                Assert.That(prefab, Is.Not.Null, data.name);
                Assert.That(data.VisionHull.Count, Is.GreaterThanOrEqualTo(3), data.name);
                Vector2[] measuredHull = ShipVisionHullBaker.MeasureHull(prefab);
                Assert.That(data.VisionHull.Count, Is.EqualTo(measuredHull.Length),
                    $"{data.name}: re-bake vision hull after changing ship geometry.");
                float twiceArea = 0f;
                Vector2 previous = data.VisionHull[data.VisionHull.Count - 1];
                for (int i = 0; i < data.VisionHull.Count; i++)
                {
                    Vector2 current = data.VisionHull[i];
                    Assert.That(current.x, Is.EqualTo(measuredHull[i].x).Within(0.001f),
                        $"{data.name}: stale vision hull vertex {i} (x).");
                    Assert.That(current.y, Is.EqualTo(measuredHull[i].y).Within(0.001f),
                        $"{data.name}: stale vision hull vertex {i} (z).");
                    twiceArea += previous.x * current.y - previous.y * current.x;
                    previous = current;
                }
                Assert.That(Mathf.Abs(twiceArea), Is.GreaterThan(0f), data.name);
            }
        }

        [Test]
        public void IntersectsCircle_ExposedBowWithHiddenCenter_RevealsHull()
        {
            Vector2 source = new Vector2(0f, 55f);
            Assert.That(source.sqrMagnitude, Is.GreaterThan(100f));
            Assert.That(VisionHullGeometry.IntersectsCircle(_wedge, source, 10f), Is.True);
        }

        [Test]
        public void IntersectsCircle_EdgeCrossesVisionWithAllVerticesOutside_RevealsHull()
        {
            Vector2 source = new Vector2(0f, -55f);
            foreach (Vector2 point in _wedge)
                Assert.That((point - source).sqrMagnitude, Is.GreaterThan(100f));

            Assert.That(VisionHullGeometry.IntersectsCircle(_wedge, source, 10f), Is.True);
        }

        [Test]
        public void IntersectsCircle_CircleInsideHull_RevealsHull()
        {
            Assert.That(VisionHullGeometry.IntersectsCircle(_wedge, new Vector2(0f, -20f), 1f), Is.True);
        }

        [Test]
        public void IntersectsCircle_EmptyCornerOfWedgeBounds_StaysHidden()
        {
            Assert.That(VisionHullGeometry.IntersectsCircle(_wedge, new Vector2(19f, 45f), 2f), Is.False);
        }

        [TestCase(5f, true)]
        [TestCase(4.9f, false)]
        public void IntersectsCircle_TouchingEdgeIsVisible(float radius, bool expected)
        {
            Assert.That(VisionHullGeometry.IntersectsCircle(_wedge, new Vector2(0f, -55f), radius),
                Is.EqualTo(expected));
        }

        [Test]
        public void WorldHull_RotationTranslationAndScalePreserveOffsetAndRefreshCachedPoints()
        {
            GameObject owner = new GameObject(nameof(VisionHullGeometryTests));
            try
            {
                Vector2[] localHull = { new Vector2(2f, 3f), new Vector2(5f, 3f), new Vector2(2f, 7f) };
                var footprint = new ShipVisionFootprint(owner.transform, 37, localHull);
                IReadOnlyList<Vector2> first = footprint.WorldHull;
                Assert.That(first[0], Is.EqualTo(new Vector2(2f, 3f)));
                owner.transform.SetPositionAndRotation(new Vector3(100f, 900f, 200f),
                    Quaternion.Euler(0f, 90f, 0f));
                owner.transform.localScale = new Vector3(2f, 4f, 3f);

                IReadOnlyList<Vector2> moved = footprint.WorldHull;
                Assert.That(moved, Is.SameAs(first));
                Assert.That(footprint.WorldHull, Is.SameAs(moved));
                Assert.That(footprint.EntityId, Is.EqualTo(37));
                Assert.That(moved[0].x, Is.EqualTo(109f).Within(0.001f));
                Assert.That(moved[0].y, Is.EqualTo(196f).Within(0.001f));
                Assert.That(moved[1].y, Is.EqualTo(190f).Within(0.001f));
                Assert.That(moved[2].x, Is.EqualTo(121f).Within(0.001f));
                Rect bounds = footprint.WorldBounds;
                Assert.That(bounds.xMin, Is.EqualTo(109f).Within(0.001f));
                Assert.That(bounds.xMax, Is.EqualTo(121f).Within(0.001f));
                Assert.That(bounds.yMin, Is.EqualTo(190f).Within(0.001f));
                Assert.That(bounds.yMax, Is.EqualTo(196f).Within(0.001f));
            }
            finally
            {
                Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void WorldHull_RotatingWedgeKeepsExposedHullVisibleWithoutRevealingEmptyCorners()
        {
            GameObject owner = new GameObject(nameof(VisionHullGeometryTests));
            try
            {
                var footprint = new ShipVisionFootprint(owner.transform, 37, _wedge);
                Assert.That(VisionHullGeometry.IntersectsCircle(footprint.WorldHull, new Vector2(0f, 55f), 10f),
                    Is.True);
                owner.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
                Assert.That(VisionHullGeometry.IntersectsCircle(footprint.WorldHull, new Vector2(55f, 0f), 10f),
                    Is.True);
                Assert.That(VisionHullGeometry.IntersectsCircle(footprint.WorldHull, new Vector2(45f, -19f), 2f),
                    Is.False);
                Assert.That(VisionHullGeometry.IntersectsCircle(footprint.WorldHull, new Vector2(0f, 55f), 10f),
                    Is.False);
            }
            finally
            {
                Object.DestroyImmediate(owner);
            }
        }
    }
}
