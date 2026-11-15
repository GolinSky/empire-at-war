using System.Reflection;
using System.Threading;
using EmpireAtWar.Components.Selection.Marquee;
using EmpireAtWar.Components.Squadrons.Icon;
using EmpireAtWar.Models.Players;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityCamera = UnityEngine.Camera;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Squadrons")]
    public sealed class SquadronIconPrefabTests
    {
        private const string SQUADRON_PREFAB_FOLDER = "Assets/Prefabs/Models/Squadrons";
        private const string FRAME_SHADER = "EmpireAtWar/UI/Squadron Frame";
        private const string ICON_SHADER = "EmpireAtWar/UI/Squadron Icon";
        private const BindingFlags PRIVATE_INSTANCE = BindingFlags.Instance | BindingFlags.NonPublic;

        [TestCase(false)]
        [TestCase(true)]
        public void NearbyShipMarkers_ShareGridWithShipsAndSquadrons(bool secondIsSquadron)
        {
            GameObject root = new GameObject("MarkerLayer", typeof(RectTransform));
            try
            {
                BattleMarkerLayer layer = CreateLayer(root);
                BattleMarker first = CreateMarker(layer, root.transform, 1, false);
                BattleMarker second = CreateMarker(layer, root.transform, 2, secondIsSquadron);
                SquadronIconGrouping grouping = GetGrouping(layer);
                UpdateGrouping(layer, grouping, 3);

                Assert.That(grouping.TryGetSlot(first.EntityId, out long firstGroup, out Vector2Int firstCell), Is.True);
                Assert.That(grouping.TryGetSlot(second.EntityId, out long secondGroup, out Vector2Int secondCell), Is.True);
                Assert.That(secondGroup, Is.EqualTo(firstGroup));
                Assert.That(secondCell, Is.Not.EqualTo(firstCell));

                var cameraObject = new GameObject("MarkerCamera");
                cameraObject.transform.SetParent(root.transform, false);
                UnityCamera camera = cameraObject.AddComponent<UnityCamera>();
                camera.transform.position = new Vector3(0f, 0f, -100f);
                camera.pixelRect = Screen.safeArea;
                float zoom = Mathf.Min(1f, camera.pixelWidth / 300f, camera.pixelHeight / 300f);
                new BattleMarkerProjection().Draw(layer.Markers, grouping, camera, zoom);

                Assert.That(first.IsShown, Is.True);
                Assert.That(second.IsShown, Is.True);
                Assert.That(second.ScreenRect.center.y, Is.EqualTo(first.ScreenRect.center.y).Within(0.001f));
                Assert.That(Mathf.Abs(second.ScreenRect.center.x - first.ScreenRect.center.x),
                    Is.EqualTo(first.ScreenRect.width).Within(0.001f));
                Assert.That(first.ScreenRect.width, Is.EqualTo(122.88f * zoom).Within(0.001f));
                Assert.That(first.VisualSize * zoom, Is.LessThan(first.ScreenRect.width));
                Assert.That(second.VisualSize * zoom, Is.LessThan(second.ScreenRect.width));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void HiddenOrUnregisteredShip_CannotRecoverSlotFromPendingSnapshot(bool unregister)
        {
            GameObject root = new GameObject("MarkerLayer", typeof(RectTransform));
            try
            {
                BattleMarkerLayer layer = CreateLayer(root);
                BattleMarker ship = CreateMarker(layer, root.transform, 1, false);
                CreateMarker(layer, root.transform, 2, true);
                CreateMarker(layer, root.transform, 3, true);
                SquadronIconGrouping grouping = GetGrouping(layer);
                UpdateGrouping(layer, grouping, 3);
                Assert.That(grouping.TryGetSlot(ship.EntityId, out _, out _), Is.True);

                Schedule(layer);
                Assert.That(grouping.HasPendingSnapshot, Is.True);
                if (unregister) ship.Unregister();
                else ship.SetVisible(false);
                Assert.That(grouping.TryGetSlot(ship.EntityId, out _, out _), Is.False);
                Consume(grouping);
                Assert.That(grouping.TryGetSlot(ship.EntityId, out _, out _), Is.False);
                UpdateGrouping(layer, grouping, 3);
                Assert.That(grouping.TryGetSlot(ship.EntityId, out _, out _), Is.False);
                Assert.That(grouping.TryGetSlot(2, out long group, out _), Is.True);
                Assert.That(grouping.TryGetSlot(3, out long otherGroup, out _), Is.True);
                Assert.That(otherGroup, Is.EqualTo(group));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ShipMotion_UsesAnchorAndResetsBaselineOnRegistration()
        {
            GameObject root = new GameObject("MarkerLayer", typeof(RectTransform));
            try
            {
                BattleMarkerLayer layer = CreateLayer(root);
                BattleMarker ship = CreateMarker(layer, root.transform, 1, false);
                ship.AnchorTransform.position = new Vector3(3f, 0f, 4f);
                ship.SampleMotion(0.1f);
                Assert.That(ship.Position, Is.EqualTo(new Vector3(3f, 0f, 4f)));
                Assert.That(ship.Velocity, Is.EqualTo(new Vector3(30f, 0f, 40f)));
                ship.SampleMotion(0.1f);
                Assert.That(ship.Velocity, Is.EqualTo(Vector3.zero));

                ship.Unregister();
                ship.AnchorTransform.position = new Vector3(500f, 0f, 500f);
                ship.Register(1, new PlayerId(0));
                ship.SampleMotion(0.1f);
                Assert.That(ship.Velocity, Is.EqualTo(Vector3.zero), "Reused markers must not inherit teleport velocity.");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static BattleMarkerLayer CreateLayer(GameObject root)
        {
            BattleMarkerLayer layer = root.AddComponent<BattleMarkerLayer>();
            typeof(BattleMarkerLayer).GetField("_root", PRIVATE_INSTANCE).SetValue(layer, root.transform);
            return layer;
        }

        [TestCase(false)]
        [TestCase(true)]
        public void RemovingEdgeMarker_PreservesSurvivorPositionsAndPitchThroughFinalSurvivor(bool removedIsShip)
        {
            GameObject root = new GameObject("MarkerLayer", typeof(RectTransform));
            try
            {
                BattleMarkerLayer layer = CreateLayer(root);
                BattleMarker first = CreateMarker(layer, root.transform, 1, true);
                BattleMarker second = CreateMarker(layer, root.transform, 2, true);
                BattleMarker removed = CreateMarker(layer, root.transform, 3, !removedIsShip);
                first.SetPosition(new Vector3(-10f, 0f, 0f));
                if (removedIsShip) removed.AnchorTransform.position = new Vector3(10f, 0f, 0f);
                else removed.SetPosition(new Vector3(10f, 0f, 0f));
                SquadronIconGrouping grouping = GetGrouping(layer);
                UpdateGrouping(layer, grouping, 3);
                UnityCamera camera = CreateProjectionCamera(root.transform, out float zoom);
                var projection = new BattleMarkerProjection();
                projection.Draw(layer.Markers, grouping, camera, zoom);
                Assert.That(first.IsShown && second.IsShown && removed.IsShown, Is.True);
                Rect firstBefore = first.ScreenRect;
                Rect secondBefore = second.ScreenRect;

                removed.Unregister();
                projection.Draw(layer.Markers, grouping, camera, zoom);
                Assert.That(first.ScreenRect, Is.EqualTo(firstBefore));
                Assert.That(second.ScreenRect, Is.EqualTo(secondBefore));
                for (int frame = 0; frame < 20; frame++)
                {
                    UpdateGrouping(layer, grouping, 1);
                    projection.Draw(layer.Markers, grouping, camera, zoom);
                    Assert.That(first.ScreenRect, Is.EqualTo(firstBefore));
                    Assert.That(second.ScreenRect, Is.EqualTo(secondBefore));
                }

                first.Unregister();
                projection.Draw(layer.Markers, grouping, camera, zoom);
                Assert.That(second.ScreenRect, Is.EqualTo(secondBefore));
                for (int frame = 0; frame < 20; frame++)
                {
                    UpdateGrouping(layer, grouping, 1);
                    projection.Draw(layer.Markers, grouping, camera, zoom);
                    Assert.That(second.IsShown, Is.True);
                    Assert.That(second.ScreenRect, Is.EqualTo(secondBefore));
                }
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void DepartingSquadron_DoesNotDragStationaryGridBeforeOrAfterSplit()
        {
            GameObject root = new GameObject("MarkerLayer", typeof(RectTransform));
            try
            {
                BattleMarkerLayer layer = CreateLayer(root);
                BattleMarker first = CreateMarker(layer, root.transform, 1, true);
                BattleMarker second = CreateMarker(layer, root.transform, 2, true);
                BattleMarker departing = CreateMarker(layer, root.transform, 3, true);
                SquadronIconGrouping grouping = GetGrouping(layer);
                UpdateGrouping(layer, grouping, 3);
                Assert.That(grouping.TryGetSlot(first.EntityId, out long originalGroup, out _), Is.True);
                UnityCamera camera = CreateProjectionCamera(root.transform, out float zoom);
                var projection = new BattleMarkerProjection();
                projection.Draw(layer.Markers, grouping, camera, zoom);
                Rect firstBefore = first.ScreenRect;
                Rect secondBefore = second.ScreenRect;

                departing.SetPosition(new Vector3(30f, 0f, 0f));
                for (int frame = 0; frame < 10; frame++)
                {
                    projection.Draw(layer.Markers, grouping, camera, zoom);
                    Assert.That(first.ScreenRect, Is.EqualTo(firstBefore));
                    Assert.That(second.ScreenRect, Is.EqualTo(secondBefore));
                }

                departing.SetPosition(new Vector3(200f, 0f, 0f));
                UpdateGrouping(layer, grouping, 4);
                Assert.That(grouping.TryGetSlot(departing.EntityId, out long departingGroup, out _), Is.True);
                Assert.That(departingGroup, Is.Not.EqualTo(originalGroup));
                for (int frame = 0; frame < 10; frame++)
                {
                    projection.Draw(layer.Markers, grouping, camera, zoom);
                    Assert.That(first.ScreenRect, Is.EqualTo(firstBefore));
                    Assert.That(second.ScreenRect, Is.EqualTo(secondBefore));
                }
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ReusedReplicaEntityId_DoesNotRestoreDeadMarkersGridState()
        {
            GameObject root = new GameObject("MarkerLayer", typeof(RectTransform));
            try
            {
                BattleMarkerLayer layer = CreateLayer(root);
                BattleMarker survivor = CreateMarker(layer, root.transform, 1, true);
                BattleMarker reused = CreateMarker(layer, root.transform, 2, true);
                SquadronIconGrouping grouping = GetGrouping(layer);
                UpdateGrouping(layer, grouping, 3);
                UnityCamera camera = CreateProjectionCamera(root.transform, out float zoom);
                var projection = new BattleMarkerProjection();
                projection.Draw(layer.Markers, grouping, camera, zoom);
                Rect survivorBefore = survivor.ScreenRect;
                Assert.That(reused.ProjectionGroup, Is.Not.Zero);

                // Reliable online death can arrive while the local grouping job is still pending.
                Schedule(layer);
                reused.Unregister();
                Consume(grouping);
                projection.Draw(layer.Markers, grouping, camera, zoom);
                Assert.That(survivor.ScreenRect, Is.EqualTo(survivorBefore));

                // The squadron pool restores the host's same entity ID at a new spawn location.
                reused.ResetPosition(new Vector3(200f, 0f, 0f));
                reused.Register(2, new PlayerId(0));
                reused.SetVisible(true);
                Assert.That(reused.ProjectionGroup, Is.Zero);
                UpdateGrouping(layer, grouping, 3);
                Assert.That(grouping.TryGetSlot(2, out _, out _), Is.False);
                projection.Draw(layer.Markers, grouping, camera, zoom);
                Assert.That(survivor.ScreenRect, Is.EqualTo(survivorBefore));

                reused.SetPosition(Vector3.zero);
                UpdateGrouping(layer, grouping, 3);
                Assert.That(grouping.TryGetSlot(1, out long survivorGroup, out _), Is.True);
                Assert.That(grouping.TryGetSlot(2, out long reusedGroup, out _), Is.True);
                Assert.That(reusedGroup, Is.EqualTo(survivorGroup));
                projection.Draw(layer.Markers, grouping, camera, zoom);
                Assert.That(survivor.ScreenRect, Is.EqualTo(survivorBefore));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void Grid_IgnoresSmallAnchorJitterButFollowsSharedMovementAndCamera()
        {
            GameObject root = new GameObject("MarkerLayer", typeof(RectTransform));
            try
            {
                BattleMarkerLayer layer = CreateLayer(root);
                BattleMarker first = CreateMarker(layer, root.transform, 1, true);
                BattleMarker second = CreateMarker(layer, root.transform, 2, true);
                SquadronIconGrouping grouping = GetGrouping(layer);
                UpdateGrouping(layer, grouping, 3);
                UnityCamera camera = CreateProjectionCamera(root.transform, out float zoom);
                var projection = new BattleMarkerProjection();
                projection.Draw(layer.Markers, grouping, camera, zoom);
                Rect firstBefore = first.ScreenRect;
                Rect secondBefore = second.ScreenRect;

                for (int frame = 0; frame < 20; frame++)
                {
                    Vector3 jitter = new Vector3(frame % 2 == 0 ? 0.25f : -0.25f, 0f, 0f);
                    first.SetPosition(jitter);
                    second.SetPosition(jitter);
                    projection.Draw(layer.Markers, grouping, camera, zoom);
                    Assert.That(first.ScreenRect, Is.EqualTo(firstBefore));
                    Assert.That(second.ScreenRect, Is.EqualTo(secondBefore));
                }

                first.SetPosition(new Vector3(20f, 0f, 0f));
                second.SetPosition(new Vector3(20f, 0f, 0f));
                projection.Draw(layer.Markers, grouping, camera, zoom);
                Vector2 movement = first.ScreenRect.center - firstBefore.center;
                Assert.That(movement.x, Is.GreaterThan(15f).And.LessThanOrEqualTo(20f));
                Assert.That(movement.y, Is.EqualTo(0f).Within(0.001f));
                Assert.That(Vector2.Distance(second.ScreenRect.center - secondBefore.center, movement),
                    Is.LessThan(0.001f));
                Rect stopped = first.ScreenRect;
                for (int frame = 0; frame < 20; frame++)
                {
                    projection.Draw(layer.Markers, grouping, camera, zoom);
                    Assert.That(first.ScreenRect, Is.EqualTo(stopped), "No trailing drift after shared movement stops.");
                }

                camera.transform.position += new Vector3(10f, 0f, 0f);
                projection.Draw(layer.Markers, grouping, camera, zoom);
                Assert.That(first.ScreenRect.center.x, Is.EqualTo(stopped.center.x - 10f).Within(0.001f));
                Assert.That(first.ScreenRect.center.y, Is.EqualTo(stopped.center.y).Within(0.001f));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void CameraPanPastViewportEdge_PreservesStationaryGrids(bool mixedShips)
        {
            GameObject root = new GameObject("MarkerLayer", typeof(RectTransform));
            try
            {
                BattleMarkerLayer layer = CreateLayer(root);
                UnityCamera camera = CreateProjectionCamera(root.transform, out float zoom);
                var markers = new BattleMarker[4];
                float separation = Mathf.Max(100f, 250f * zoom);
                for (int i = 0; i < markers.Length; i++)
                {
                    BattleMarker marker = CreateMarker(layer, root.transform, i + 1, !mixedShips || i % 2 == 0);
                    Vector3 position = new Vector3(i < 2 ? -separation * 0.5f : separation * 0.5f, 0f, 0f);
                    if (marker.IsSquadron) marker.SetPosition(position);
                    else marker.AnchorTransform.position = position;
                    marker.SampleMotion(0.1f);
                    marker.SampleMotion(0.1f); // Establish zero velocity even when Editor deltaTime is zero.
                    markers[i] = marker;
                }

                SquadronIconGrouping grouping = GetGrouping(layer);
                UpdateGrouping(layer, grouping, 3);
                var projection = new BattleMarkerProjection();
                projection.Draw(layer.Markers, grouping, camera, zoom);
                var originalRects = new Rect[markers.Length];
                var originalGroups = new long[markers.Length];
                var originalCells = new Vector2Int[markers.Length];
                for (int i = 0; i < markers.Length; i++)
                {
                    Assert.That(markers[i].IsShown, Is.True);
                    originalRects[i] = markers[i].ScreenRect;
                    Assert.That(grouping.TryGetSlot(markers[i].EntityId, out originalGroups[i], out originalCells[i]), Is.True);
                }
                Assert.That(originalGroups[1], Is.EqualTo(originalGroups[0]));
                Assert.That(originalGroups[3], Is.EqualTo(originalGroups[2]));
                Assert.That(originalGroups[2], Is.Not.EqualTo(originalGroups[0]));

                // Leave the leftmost icon partly outside the viewport: the grid must follow
                // its world anchor instead of being pushed inward and repacked.
                float pan = Mathf.Min(originalRects[0].center.x, originalRects[1].center.x) -
                    camera.pixelRect.xMin - originalRects[0].width * 0.25f;
                Vector3 originalCameraPosition = camera.transform.position;
                camera.transform.position = originalCameraPosition + new Vector3(pan, 0f, 0f);
                projection.Draw(layer.Markers, grouping, camera, zoom);
                for (int i = 0; i < markers.Length; i++)
                {
                    Assert.That(markers[i].IsShown, Is.True);
                    Assert.That(markers[i].ScreenRect.center.x, Is.EqualTo(originalRects[i].center.x - pan).Within(0.001f));
                    Assert.That(markers[i].ScreenRect.center.y, Is.EqualTo(originalRects[i].center.y).Within(0.001f));
                    Assert.That(markers[i].ScreenRect.size, Is.EqualTo(originalRects[i].size));
                }

                camera.transform.position = originalCameraPosition + new Vector3(camera.pixelWidth * 2f, 0f, 0f);
                projection.Draw(layer.Markers, grouping, camera, zoom);
                foreach (BattleMarker marker in markers) Assert.That(marker.IsShown, Is.False);

                camera.transform.position = originalCameraPosition;
                UpdateGrouping(layer, grouping, 3);
                projection.Draw(layer.Markers, grouping, camera, zoom);
                for (int i = 0; i < markers.Length; i++)
                {
                    Assert.That(markers[i].IsShown, Is.True);
                    Assert.That(markers[i].ScreenRect, Is.EqualTo(originalRects[i]));
                    Assert.That(grouping.TryGetSlot(markers[i].EntityId, out long group, out Vector2Int cell), Is.True);
                    Assert.That(group, Is.EqualTo(originalGroups[i]));
                    Assert.That(cell, Is.EqualTo(originalCells[i]));
                }
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static UnityCamera CreateProjectionCamera(Transform parent, out float zoom)
        {
            var cameraObject = new GameObject("MarkerCamera");
            cameraObject.transform.SetParent(parent, false);
            UnityCamera camera = cameraObject.AddComponent<UnityCamera>();
            camera.transform.position = new Vector3(0f, 0f, -100f);
            camera.pixelRect = Screen.safeArea;
            camera.orthographic = true;
            camera.orthographicSize = camera.pixelHeight * 0.5f;
            camera.aspect = (float)camera.pixelWidth / camera.pixelHeight;
            zoom = Mathf.Min(1f, camera.pixelWidth / 600f, camera.pixelHeight / 600f);
            return camera;
        }

        private static BattleMarker CreateMarker(BattleMarkerLayer layer, Transform parent, long id, bool isSquadron)
        {
            var anchor = new GameObject("Anchor");
            anchor.transform.SetParent(parent, false);
            var icon = new GameObject("Marker", typeof(RectTransform));
            var marker = new BattleMarker(layer, (RectTransform)icon.transform, null, null,
                visualSize: isSquadron ? 72f : 110.88f, isSquadron: isSquadron)
            {
                AnchorTransform = anchor.transform,
                SampleVisibility = () => { }
            };
            marker.Register(id, new PlayerId(0));
            marker.SetVisible(true);
            return marker;
        }

        private static SquadronIconGrouping GetGrouping(BattleMarkerLayer layer) =>
            (SquadronIconGrouping)typeof(BattleMarkerLayer).GetField("_grouping", PRIVATE_INSTANCE).GetValue(layer);

        private static void Schedule(BattleMarkerLayer layer)
        {
            typeof(BattleMarkerLayer).GetField("_dirty", PRIVATE_INSTANCE).SetValue(layer, true);
            typeof(BattleMarkerLayer).GetField("_elapsed", PRIVATE_INSTANCE).SetValue(layer, 0.1f);
            layer.Tick();
        }

        private static void UpdateGrouping(BattleMarkerLayer layer, SquadronIconGrouping grouping, int ticks)
        {
            for (int i = 0; i < ticks; i++)
            {
                Schedule(layer);
                Consume(grouping);
            }
        }

        private static void Consume(SquadronIconGrouping grouping) =>
            Assert.That(SpinWait.SpinUntil(grouping.TryComplete, 5000), Is.True, "Grouping job did not complete.");

        [Test]
        public void ClickAndMarquee_UseTheDisplayedRectangleWhileJoiningAGrid()
        {
            GameObject root = new GameObject("Marker", typeof(RectTransform));
            try
            {
                var marker = new BattleMarker(null, (RectTransform)root.transform, null, null);
                var viewport = new Rect(0f, 0f, 400f, 400f);
                marker.Display(new Vector2(100f, 100f), viewport, 1f, 132f, 1f);
                marker.Display(new Vector2(300f, 100f), viewport, 1f, 84f, 0.25f, 1);
                Assert.That(marker.ScreenRect.center.x, Is.EqualTo(150f));
                Assert.That(marker.ScreenRect.width, Is.EqualTo(120f));
                Assert.That(marker.ContainsScreenPoint(new Vector2(209f, 100f)), Is.True);
                Assert.That(marker.OverlapsMarquee(new MarqueeRectangle(
                    new MarqueePoint(209f, 98f), new MarqueePoint(212f, 102f))), Is.True);
                Assert.That(marker.ContainsScreenPoint(new Vector2(300f, 100f)), Is.False);

                marker.Hide();
                Assert.That(marker.ContainsScreenPoint(new Vector2(150f, 100f)), Is.False);
                Assert.That(marker.OverlapsMarquee(new MarqueeRectangle(
                    new MarqueePoint(149f, 98f), new MarqueePoint(151f, 102f))), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void SquadronViews_MarkerRootIsAssignedAndHasNoOwnCanvas()
        {
            int checkedCount = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { SQUADRON_PREFAB_FOLDER }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                SquadronIconComponent icon = prefab.GetComponent<SquadronIconComponent>();
                if (icon == null) continue;

                SerializedProperty root = new SerializedObject(icon).FindProperty("iconRoot");
                RectTransform marker = (RectTransform)root.objectReferenceValue;
                Assert.That(marker, Is.Not.Null, path);
                // A canvas per marker rebuilds one batch per squadron every frame; the shared layer owns the canvas.
                Assert.That(marker.GetComponentsInChildren<Canvas>(true), Is.Empty, path);
                Assert.That(marker.gameObject.layer, Is.EqualTo(LayerMask.NameToLayer("WorldUi")), path);
                // The frame shader reads centre offsets from UV1: batched into the shared canvas, vertices are screen positions.
                Graphic frame = (Graphic)new SerializedObject(icon).FindProperty("frameImage").objectReferenceValue;
                if (frame.material.shader.name == FRAME_SHADER)
                    Assert.That(frame.GetComponent<SquadronFrameMeshEffect>(), Is.Not.Null, path);
                Image unitIcon = (Image)new SerializedObject(icon).FindProperty("iconImage").objectReferenceValue;
                Assert.That(unitIcon, Is.Not.Null, path);
                Assert.That(unitIcon.enabled, Is.True, path);
                Assert.That(unitIcon.material.shader.name, Is.EqualTo(ICON_SHADER), path);
                Assert.That(unitIcon.sprite, Is.Not.Null, path);
                string unitName = prefab.name.Replace("SquadronView", "");
                string iconFolder = unitName == "YWingBomber" || unitName == "TIEAvenger"
                    ? "ShipIcon"
                    : "SquadronIcon";
                // Icons are grouped by faction: <iconFolder>/<Faction>/<Unit>Icon.png.
                Assert.That(AssetDatabase.GetAssetPath(unitIcon.sprite),
                    Does.StartWith($"Assets/Art/Textures/Ui/Icons/{iconFolder}/").And.EndWith($"/{unitName}Icon.png"), path);
                Assert.That(unitIcon.preserveAspect, Is.True, path);
                Assert.That(unitIcon.rectTransform.sizeDelta, Is.EqualTo(new Vector2(-13.5f, -13.5f)), path);
                Assert.That(unitIcon.rectTransform.anchoredPosition, Is.EqualTo(new Vector2(0f, -1.5f)), path);
                int unitImageCount = 0;
                foreach (Image image in marker.GetComponentsInChildren<Image>(true))
                {
                    Assert.That(image.name, Is.Not.EqualTo("Silhouette"), path);
                    if (image == frame || image.name == "Background") continue;
                    Assert.That(image, Is.SameAs(unitIcon), path);
                    unitImageCount++;
                }
                Assert.That(unitImageCount, Is.EqualTo(1), path);
                checkedCount++;
            }

            Assert.That(checkedCount, Is.GreaterThan(0));
        }
    }
}
