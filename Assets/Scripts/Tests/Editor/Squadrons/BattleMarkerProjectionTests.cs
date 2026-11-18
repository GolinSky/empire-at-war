using System.Collections.Generic;
using System.Threading;
using EmpireAtWar.Components.Squadrons.Icon;
using EmpireAtWar.Models.Players;
using NUnit.Framework;
using UnityEngine;
using UnityCamera = UnityEngine.Camera;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Squadrons")]
    public sealed class BattleMarkerProjectionTests
    {
        private const float ZOOM = 1f;
        private const float FRAME = 0.016f;
        private const float TICK = 0.1f;

        private GameObject _root;
        private UnityCamera _camera;
        private TestLayer _layer;
        private BattleMarkerProjection _projection;
        private SquadronIconGrouping _grouping;
        private readonly List<IBattleHull> _hulls = new List<IBattleHull>();

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("Root", typeof(RectTransform));
            var cameraObject = new GameObject("MarkerCamera");
            cameraObject.transform.SetParent(_root.transform, false);
            _camera = cameraObject.AddComponent<UnityCamera>();
            _camera.transform.position = new Vector3(0f, 0f, -100f);
            _camera.pixelRect = Screen.safeArea;
            _camera.orthographic = true;
            _camera.orthographicSize = _camera.pixelHeight * 0.5f;
            _camera.aspect = (float)_camera.pixelWidth / _camera.pixelHeight;
            _grouping = new SquadronIconGrouping();
            _layer = new TestLayer(_grouping, _root.transform);
            _projection = new BattleMarkerProjection();
        }

        [TearDown]
        public void TearDown()
        {
            _grouping.Dispose();
            Object.DestroyImmediate(_root);
        }

        [Test]
        public void ClusteredSquadronsOccupyAdjacentMatrixCellsAtTheGridPitch()
        {
            BattleMarker first = CreateSquadron(1, 0f);
            BattleMarker second = CreateSquadron(2, 30f);
            Cluster(10);
            Draw();

            Assert.That(first.IsShown && second.IsShown, Is.True);
            Assert.That(second.ScreenRect.center.y, Is.EqualTo(first.ScreenRect.center.y).Within(0.001f));
            Assert.That(Mathf.Abs(second.ScreenRect.center.x - first.ScreenRect.center.x),
                Is.EqualTo(BattleMarkerProjection.Pitch(ZOOM)).Within(0.001f));
        }

        [Test]
        public void SquadronsFarApartDoNotStackOrShareACluster()
        {
            BattleMarker first = CreateSquadron(1, 0f);
            BattleMarker second = CreateSquadron(2, 600f);
            Cluster(10);
            Draw();

            Assert.That(first.ScreenRect.Overlaps(second.ScreenRect), Is.False);
        }

        [Test]
        public void HitRectangleIsTheVisualCell()
        {
            BattleMarker marker = CreateSquadron(1, 0f);
            Cluster(2);
            Draw();
            Assert.That(marker.ScreenRect.width, Is.EqualTo(marker.VisualSize * ZOOM).Within(0.001f));
        }

        [Test]
        public void CameraPanMovesIconsExactlyWithTheWorld()
        {
            BattleMarker first = CreateSquadron(1, 0f);
            BattleMarker second = CreateSquadron(2, 30f);
            Cluster(10);
            Draw();
            Vector2 firstBefore = first.ScreenRect.center;
            Vector2 secondBefore = second.ScreenRect.center;

            _camera.transform.position += new Vector3(40f, 0f, 0f);
            Draw();
            Assert.That(first.ScreenRect.center, Is.EqualTo(firstBefore + new Vector2(-40f, 0f)).Using(NEAR));
            Assert.That(second.ScreenRect.center, Is.EqualTo(secondBefore + new Vector2(-40f, 0f)).Using(NEAR));
        }

        [Test]
        public void MembershipChangeDoesNotJumpTheRemainingIcons()
        {
            BattleMarker first = CreateSquadron(1, 0f);
            BattleMarker second = CreateSquadron(2, 20f);
            BattleMarker leaving = CreateSquadron(3, 40f);
            Cluster(10);
            Draw();
            Vector2 before = first.ScreenRect.center;
            Vector2 cellJump = SquadronClusterLayout.CellOffset(0, 3, BattleMarkerProjection.Pitch(ZOOM)) -
                               SquadronClusterLayout.CellOffset(0, 2, BattleMarkerProjection.Pitch(ZOOM));

            leaving.SetVisible(false);
            Cluster(1);
            Draw();
            Vector2 after = first.ScreenRect.center;
            Assert.That(second.IsShown, Is.True);
            Assert.That(cellJump.magnitude, Is.GreaterThan(1f), "The layout really changed.");
            Assert.That((after - before).magnitude, Is.LessThan(cellJump.magnitude * 0.25f));
        }

        [Test]
        public void GridMovesOffAShipHullInsteadOfCoveringIt()
        {
            BattleMarker marker = CreateSquadron(1, 0f);
            _hulls.Add(new TestHull(_root.transform, new Bounds(new Vector3(0f, 60f, 0f), new Vector3(200f, 100f, 10f))));
            Cluster(2);
            Draw();

            Assert.That(_projection.TryProject(marker.Position, out Vector2 centroid), Is.True);
            Assert.That(marker.ScreenRect.yMax, Is.LessThanOrEqualTo(centroid.y + 0.01f),
                "The grid sits below the hull instead of over it.");
        }

        [Test]
        public void ShipMarkersAreObstaclesForSquadronGrids()
        {
            BattleMarker ship = CreateShip(100, new Vector3(0f, 0f, 0f));
            BattleMarker squadron = CreateSquadron(1, 0f);
            Cluster(2);
            Draw();

            Assert.That(ship.IsShown, Is.True);
            Assert.That(squadron.ScreenRect.Overlaps(ship.ScreenRect), Is.False);
        }

        private void Cluster(int ticks)
        {
            for (int i = 0; i < ticks; i++)
            {
                _projection.Prepare(_camera);
                _grouping.BeginSnapshot(_layer.Markers.Count);
                foreach (IBattleMarker entry in _layer.Markers)
                {
                    var marker = (BattleMarker)entry;
                    if (marker.IsSquadron && marker.IsVisible && _projection.TryProject(marker.Position, out Vector2 screen))
                        _grouping.Add(marker.EntityId, 0, screen);
                }

                _grouping.Schedule(TICK, BattleMarkerProjection.JoinPixels(ZOOM), BattleMarkerProjection.LeavePixels(ZOOM),
                    BattleMarkerProjection.Pitch(ZOOM));
                Assert.That(SpinWait.SpinUntil(_grouping.TryComplete, 5000), Is.True);
            }
        }

        private void Draw() => _projection.Draw(_layer.Markers, _hulls, _grouping, _camera, ZOOM, FRAME);

        private BattleMarker CreateSquadron(long id, float x) => CreateMarker(id, true, new Vector3(x, 0f, 0f));

        private BattleMarker CreateShip(long id, Vector3 position) => CreateMarker(id, false, position);

        private BattleMarker CreateMarker(long id, bool isSquadron, Vector3 position)
        {
            var anchor = new GameObject("Anchor");
            anchor.transform.SetParent(_root.transform, false);
            anchor.transform.position = position;
            var icon = new GameObject("Marker", typeof(RectTransform));
            var marker = new BattleMarker(_layer, (RectTransform)icon.transform, null, null,
                visualSize: isSquadron ? 72f : 110.88f, screenOffset: isSquadron ? 60f : 80f, isSquadron: isSquadron)
            {
                AnchorTransform = anchor.transform,
                SampleVisibility = () => { }
            };
            marker.SetPosition(position);
            marker.Register(id, new PlayerId(0));
            marker.SetVisible(true);
            return marker;
        }

        private static readonly IEqualityComparer<Vector2> NEAR = new NearComparer();

        private sealed class NearComparer : IEqualityComparer<Vector2>
        {
            public bool Equals(Vector2 a, Vector2 b) => (a - b).magnitude < 0.01f;
            public int GetHashCode(Vector2 value) => 0;
        }

        private sealed class TestHull : IBattleHull
        {
            public TestHull(Transform parent, Bounds bounds)
            {
                Transform = new GameObject("Hull").transform;
                Transform.SetParent(parent, false);
                LocalBounds = bounds;
            }

            public bool IsActive => true;
            public Transform Transform { get; }
            public Bounds LocalBounds { get; }
        }

        private sealed class TestLayer : IBattleMarkerLayer
        {
            private readonly List<IBattleMarker> _markers = new List<IBattleMarker>();
            private readonly SquadronIconGrouping _grouping;
            private readonly Transform _parent;

            public TestLayer(SquadronIconGrouping grouping, Transform parent)
            {
                _grouping = grouping;
                _parent = parent;
            }

            public IReadOnlyList<IBattleMarker> Markers => _markers;
            public BattleMarker CreateShipMarker(Sprite icon, Transform anchor) => throw new System.NotSupportedException();
            public void Attach(RectTransform root, bool isSquadron) => root.SetParent(_parent, false);
            public void Register(IBattleMarker marker) => _markers.Add(marker);
            public void Unregister(IBattleMarker marker) => _markers.Remove(marker);
            public void InvalidateMembership(long entityId) => _grouping.Forget(entityId);
            public void RegisterHull(IBattleHull hull) { }
            public void UnregisterHull(IBattleHull hull) { }
        }
    }
}
