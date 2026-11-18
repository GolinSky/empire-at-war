using System.Collections.Generic;
using System.IO;
using System.Reflection;
using EmpireAtWar.Components.Ship.Selection;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Models.Selection;
using EmpireAtWar.ViewComponents.Health;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Health")]
    public sealed class HealthOverlayAnchorTests
    {
        private const string SHIP_PREFAB_PATH =
            "Assets/Prefabs/Models/Ships/Separatist/View/MunificentShipView.prefab";
        private const string STATION_PREFAB_PATH =
            "Assets/Prefabs/Models/Stations/Republic/RepublicSpaceStationView.prefab";
        // UnitSelectionRing.png: alpha > 127 spans pixels 108..1145 within its 1254px sprite rect.
        private const float RING_VISIBLE_FRACTION = 1038f / 1254f;

        private static IEnumerable<string> ShipPrefabPaths =>
            Directory.GetFiles("Assets/Prefabs/Models/Ships", "*ShipView.prefab", SearchOption.AllDirectories);

        [TestCaseSource(nameof(ShipPrefabPaths))]
        public void ShipSelectionRing_InitializationFitsHullWithoutMovingHealthAnchor(string prefabPath)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                SelectionComponent selection = root.GetComponent<SelectionComponent>();
                Assert.That(selection, Is.Not.Null);
                SerializedObject serializedSelection = new SerializedObject(selection);
                SpriteRenderer ring = (SpriteRenderer)serializedSelection.FindProperty("selectedRenderer")
                    .objectReferenceValue;
                Vector3 healthAnchor = selection.WorldPosition;
                SharedSelectionData data = AssetDatabase.LoadAssetAtPath<SharedSelectionData>(
                    "Assets/Settings/Data/Selection/SharedSelectionData.asset");
                typeof(SelectionComponent).GetMethod("Construct", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(selection, new object[]
                    {
                        new SelectionModel(), data, default(PlayerId), null,
                        new SelectionMarkerTests.SelectionServiceStub()
                    });

                selection.Initialize();

                Bounds hullBounds = default;
                bool hasHull = false;
                foreach (MeshRenderer hull in root.GetComponentsInChildren<MeshRenderer>())
                {
                    if (!hull.enabled || hull.GetComponent<Shield>() != null ||
                        hull.localBounds.extents == Vector3.zero)
                        continue;

                    for (int corner = 0; corner < 8; corner++)
                    {
                        Vector3 sign = new Vector3((corner & 1) == 0 ? -1f : 1f,
                            (corner & 2) == 0 ? -1f : 1f, (corner & 4) == 0 ? -1f : 1f);
                        Vector3 point = hull.transform.TransformPoint(hull.localBounds.center +
                            Vector3.Scale(hull.localBounds.extents, sign));
                        if (!hasHull)
                        {
                            hullBounds = new Bounds(point, Vector3.zero);
                            hasHull = true;
                        }
                        else
                            hullBounds.Encapsulate(point);
                    }
                }

                Assert.That(hasHull, Is.True);
                float hullDiameter = Mathf.Max(hullBounds.size.x, hullBounds.size.z);
                Vector3 spriteSize = ring.sprite.bounds.size;
                float ringWidth = ring.transform.TransformVector(Vector3.right * spriteSize.x).magnitude;
                float ringHeight = ring.transform.TransformVector(Vector3.up * spriteSize.y).magnitude;
                Assert.That(ringWidth * RING_VISIBLE_FRACTION / hullDiameter, Is.InRange(1.1f * 0.8f, 1.2f * 0.8f));
                Assert.That(ringHeight * RING_VISIBLE_FRACTION / hullDiameter, Is.InRange(1.1f * 0.8f, 1.2f * 0.8f));
                Vector3 ringCenter = ring.transform.TransformPoint(ring.sprite.bounds.center);
                Assert.That(ringCenter.x, Is.EqualTo(hullBounds.center.x).Within(0.01f));
                Assert.That(ringCenter.z, Is.EqualTo(hullBounds.center.z).Within(0.01f));
                Assert.That(selection.WorldPosition, Is.EqualTo(healthAnchor));

                selection.Release();
                selection.Respawn();

                Assert.That(ring.transform.TransformVector(Vector3.right * spriteSize.x).magnitude,
                    Is.EqualTo(ringWidth).Within(0.001f));
                Assert.That(selection.WorldPosition, Is.EqualTo(healthAnchor));
                selection.Release();
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        [TestCase(SHIP_PREFAB_PATH)]
        [TestCase(STATION_PREFAB_PATH)]
        public void SelectionWorldPosition_UsesExplicitSelectionCanvas(string prefabPath)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                SelectionComponent selection = root.GetComponent<SelectionComponent>();
                Transform selectedCanvas = root.transform.Find("SelectedCanvas");

                Assert.That(selection, Is.Not.Null);
                Assert.That(selectedCanvas, Is.Not.Null);
                Assert.That(selection.WorldPosition, Is.EqualTo(selectedCanvas.position));
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        [Test]
        public void RepublicStation_SelectionAnchor_IsNotItsOffsetPrefabOrigin()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(STATION_PREFAB_PATH);
            try
            {
                SelectionComponent selection = root.GetComponent<SelectionComponent>();

                Assert.That(selection, Is.Not.Null);
                Assert.That(
                    Vector3.Distance(root.transform.position, selection.WorldPosition),
                    Is.GreaterThan(10f));
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
