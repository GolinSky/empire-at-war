using System;
using System.Collections.Generic;
using System.Reflection;
using EmpireAtWar.Components.Squadrons.Icon;
using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Entities.CinematicCamera.Model;
using EmpireAtWar.Entities.MiniMap;
using EmpireAtWar.Entities.StructureStatusIcons;
using EmpireAtWar.Models.MiniMap;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Models.SkirmishCamera;
using EmpireAtWar.Presenters.ReinforcementZones;
using EmpireAtWar.Services.Camera;
using EmpireAtWar.Services.CaptureSites;
using EmpireAtWar.Services.Input;
using EmpireAtWar.Services.ReinforcementZones;
using EmpireAtWar.Ui.Base;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityCamera = UnityEngine.Camera;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Ui")]
    public sealed class StructureStatusIconPresenterTests
    {
        private const BindingFlags PRIVATE_INSTANCE = BindingFlags.Instance | BindingFlags.NonPublic;
        private StructureStatusIconPresenter _presenter;
        private StructureStatusIconSources _sources;
        private SiteEvents _events;
        private Gates _gates;
        private MiniMapUi _map;
        private MiniMapUiController _controller;
        private BattleMarkerLayer _layer;
        private UnityCamera _camera;

        [SetUp]
        public void SetUp()
        {
            _camera = new GameObject("StructureIconCamera", typeof(UnityCamera)).GetComponent<UnityCamera>();
            _camera.enabled = false;
            _camera.orthographic = true;
            _camera.orthographicSize = 5f;
            _camera.nearClipPlane = 0.3f;
            _camera.farClipPlane = 100f;
            _camera.pixelRect = new Rect(50f, 25f, 800f, 600f);
            var cameraService = _camera.gameObject.AddComponent<CameraService>();
            SetField(cameraService, "sceneCamera", _camera);
            _layer = new GameObject("StructureIconLayer", typeof(RectTransform), typeof(BattleMarkerLayer))
                .GetComponent<BattleMarkerLayer>();
            ((RectTransform)_layer.transform).sizeDelta = new Vector2(800f, 600f);
            SetField(_layer, "_root", (RectTransform)_layer.transform);
            _map = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<MiniMapUi>(
                "Assets/Prefabs/Ui/MiniMap/MiniMapUi.prefab"));
            _map.gameObject.SetActive(true);
            var range = new Vector2Range();
            typeof(Vector2Range).BaseType.GetField("<Min>k__BackingField", PRIVATE_INSTANCE)
                .SetValue(range, Vector2.one * -100f);
            typeof(Vector2Range).BaseType.GetField("<Max>k__BackingField", PRIVATE_INSTANCE)
                .SetValue(range, Vector2.one * 100f);
            SetField(_map, "_mapRange", range);
            _map.Show();
            MiniMapData data = AssetDatabase.LoadAssetAtPath<MiniMapData>(
                "Assets/Settings/Data/Models/MiniMap/MiniMapData.asset");
            _controller = new MiniMapUiController(null, cameraService, null, null, null, null,
                null, null, Array.Empty<IMiniMapOverlaySource>(), data, null, null, null);
            SetField(_controller, "_miniMapView", _map);
            _events = new SiteEvents();
            _sources = new StructureStatusIconSources(new[] { _events }, new RelayEvents(),
                TestPlayers.CreateLocalPlayer(TestPlayers.CreateDuel()));
            _sources.Initialize();
            _gates = new Gates();
            _presenter = new StructureStatusIconPresenter(_sources, _controller, data, _layer,
                _gates, cameraService, _gates, _gates);
        }

        [TearDown]
        public void TearDown()
        {
            if (_presenter != null) _presenter.LateDispose();
            if (_sources != null) _sources.LateDispose();
            if (_map != null) UnityEngine.Object.DestroyImmediate(_map.gameObject);
            if (_layer != null) UnityEngine.Object.DestroyImmediate(_layer.gameObject);
            if (_camera != null) UnityEngine.Object.DestroyImmediate(_camera.gameObject);
        }

        [Test]
        public void NewTransitionDoesNotConsumeThePreviousDisplayedInterval()
        {
            ShowAt(450f, 325f);
            Advance(20f);
            AssertClocks(2f, 0f);
            Advance(0.75f);
            AssertClocks(1.25f, 0.75f);
            ShowAt(450f, 325f, CaptureSiteStatusTransition.Destroyed);
            Advance(20f);
            AssertClocks(2f, 0f);
            Assert.That(View("_world").Root.gameObject.activeSelf, Is.True);
            Advance(0.5f);
            AssertClocks(1.5f, 0.5f);
        }

        [TestCase("hud")]
        [TestCase("cinematic")]
        [TestCase("input")]
        public void HiddenHudPausesBothClocksAndResumingWaitsForDisplayedFrame(string gate)
        {
            ShowAt(450f, 325f);
            Advance(0f);
            Advance(0.5f);
            SetGate(gate, true);
            Advance(10f);
            AssertClocks(1.5f, 0.5f);
            Assert.That(View("_world").Root.gameObject.activeSelf, Is.False);
            Assert.That(View("_map").Root.gameObject.activeSelf, Is.False);
            SetGate(gate, false);
            Advance(10f);
            AssertClocks(1.5f, 0.5f);
            Advance(0.5f);
            AssertClocks(1f, 1f);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void HiddenMinimapPausesOnlyItsClock(bool deactivate)
        {
            ShowAt(450f, 325f);
            Advance(0f);
            if (deactivate) _map.gameObject.SetActive(false);
            else _map.Hide();
            Advance(1f);
            AssertClocks(1f, 0f);
            Assert.That(View("_map").Root.gameObject.activeSelf, Is.False);
            if (deactivate) _map.gameObject.SetActive(true);
            else _map.Show();
            Advance(0.5f);
            AssertClocks(0.5f, 0f);
            Advance(0.25f);
            AssertClocks(0.25f, 0.25f);
        }

        [TestCase(78f, 325f, 10f, false)]
        [TestCase(80f, 325f, 10f, true)]
        [TestCase(820f, 325f, 10f, true)]
        [TestCase(822f, 325f, 10f, false)]
        [TestCase(450f, 53f, 10f, false)]
        [TestCase(450f, 55f, 10f, true)]
        [TestCase(450f, 595f, 10f, true)]
        [TestCase(450f, 597f, 10f, false)]
        [TestCase(450f, 325f, -10f, false)]
        [TestCase(450f, 325f, 0.1f, false)]
        [TestCase(450f, 325f, 101f, false)]
        public void ViewportIncludesRenderedOffsetMarginsAndClipPlanes(float x, float y, float depth, bool inside)
        {
            ShowAt(x, y, depth: depth);
            Advance(0f);
            Assert.That(View("_world").Root.gameObject.activeSelf, Is.EqualTo(inside));
            Advance(1f);
            AssertClocks(inside ? 1f : 2f, 1f);
        }

        [Test]
        public void CameraMovementReprojectsAndOffscreenTimeDoesNotConsumeWorldLifetime()
        {
            ShowAt(450f, 325f);
            Advance(0f);
            _camera.transform.position = Vector3.right * 100f;
            Advance(1f);
            AssertClocks(2f, 1f);
            Assert.That(View("_world").Root.gameObject.activeSelf, Is.False);
            _camera.transform.position = Vector3.zero;
            Advance(1f);
            AssertClocks(2f, 2f);
            Assert.That(View("_world").Root.gameObject.activeSelf, Is.True);
            Advance(1f);
            AssertClocks(1f, 3f);
        }

        [Test]
        public void ViewportResizeRechecksEligibilityWithoutAnAnchorChange()
        {
            ShowAt(450f, 325f);
            Advance(0f);
            _camera.pixelRect = new Rect(50f, 25f, 40f, 600f);
            Advance(1f);
            AssertClocks(2f, 1f);
            Assert.That(View("_world").Root.gameObject.activeSelf, Is.False);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void DestroyedIconTracksCameraPanRotationAndZoomOnBattleCanvas(bool perspective)
        {
            _camera.orthographic = !perspective;
            Canvas canvas = _layer.gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = _camera;
            canvas.planeDistance = _camera.nearClipPlane + 0.05f;
            Canvas.ForceUpdateCanvases();
            ShowAt(530f, 365f, CaptureSiteStatusTransition.Destroyed);
            Advance(0f);
            AssertWorldPosition();

            // Advance before the canvas catches up, as in the battle LateTick path.
            _camera.transform.position = new Vector3(0.5f, 0.25f, 0f);
            Advance(0f);
            AssertWorldPosition();
            _camera.transform.rotation = Quaternion.Euler(2f, 3f, 0f);
            Advance(0f);
            AssertWorldPosition();
            _camera.orthographicSize = 4f;
            _camera.fieldOfView = 50f;
            Advance(0f);
            AssertWorldPosition();

            // A stationary frame must retain the correct position after canvas synchronization.
            Advance(0.5f);
            AssertWorldPosition();
            AssertClocks(1.5f, 0.5f);
        }

        [Test]
        public void WorldIconUsesViewportOriginAfterViewportMovesAndResizes()
        {
            ShowAt(530f, 365f);
            Advance(0f);
            Assert.That(View("_world").Root.anchoredPosition.x, Is.EqualTo(480f).Within(0.01f));
            Assert.That(View("_world").Root.anchoredPosition.y, Is.EqualTo(340f).Within(0.01f));
            _camera.pixelRect = new Rect(150f, 75f, 640f, 480f);
            Advance(0f);
            Vector3 screen = _camera.WorldToScreenPoint(_sources.GetPosition(0));
            Assert.That(View("_world").Root.anchoredPosition.x,
                Is.EqualTo(screen.x - 150f).Within(0.01f));
            Assert.That(View("_world").Root.anchoredPosition.y,
                Is.EqualTo(screen.y + 42f - 75f).Within(0.01f));
        }

        private void AssertWorldPosition()
        {
            RectTransform icon = View("_world").Root;
            Assert.That(icon.gameObject.activeSelf, Is.True);
            Vector2 expected = _camera.WorldToScreenPoint(_sources.GetPosition(0));
            expected.y += 42f;
            Vector2 anchored = expected - _camera.pixelRect.min;
            Assert.That(Vector2.Distance(icon.anchoredPosition, anchored), Is.LessThan(0.01f),
                "Placement must not depend on the canvas's previous world transform.");
            Canvas.ForceUpdateCanvases();
            Vector2 rendered = _camera.WorldToScreenPoint(icon.position);
            Assert.That(Vector2.Distance(rendered, expected), Is.LessThan(0.1f),
                "The rendered icon center must remain 42 pixels above its structure.");
        }

        [Test]
        public void NewEventReprojectsAnExpiredSourceAfterCameraMovedWithAnotherIconActive()
        {
            _events.AddSite();
            ShowAt(530f, 365f);
            Vector3 anchor = _sources.GetPosition(0);
            _events.Emit(anchor + Vector3.right, CaptureSiteStatusTransition.Captured, 1);
            Advance(0f);
            Advance(4f);
            Assert.That(_sources.Model.Get(0).WorldActive, Is.False);
            Assert.That(_sources.Model.Get(1).WorldActive, Is.True);
            _camera.transform.position = Vector3.right;
            Advance(0f);
            _events.Emit(anchor, CaptureSiteStatusTransition.Destroyed);
            Advance(0f);
            Vector2 expected = _camera.WorldToScreenPoint(anchor);
            expected.y += 42f;
            Assert.That(View("_world").Root.gameObject.activeSelf, Is.True);
            Assert.That(Vector2.Distance(View("_world").Root.anchoredPosition,
                expected - _camera.pixelRect.min), Is.LessThan(0.01f));
            AssertClocks(2f, 0f);
        }

        [Test]
        public void ExpiredAndExternallyClearedIconsHideEvenWhenNoActiveRecordsRemain()
        {
            ShowAt(450f, 325f);
            Advance(0f);
            Advance(2f);
            Assert.That(View("_world").Root.gameObject.activeSelf, Is.False);
            Assert.That(View("_map").Root.gameObject.activeSelf, Is.True);
            Advance(1.6f);
            Assert.That(_sources.Model.ActiveCount, Is.Zero);
            Assert.That(View("_map").Root.gameObject.activeSelf, Is.False);
            ShowAt(450f, 325f);
            Advance(0f);
            _sources.Model.Clear(0);
            Advance(0f);
            Assert.That(View("_world").Root.gameObject.activeSelf, Is.False);
            Assert.That(View("_map").Root.gameObject.activeSelf, Is.False);
        }

        [Test]
        public void MapRebuildReleasesOldViewsAndDisposalPreventsRecreation()
        {
            ShowAt(450f, 325f);
            Advance(0f);
            RectTransform oldWorld = View("_world").Root;
            RectTransform oldMap = View("_map").Root;
            _events.Rebuild();
            Advance(0f);
            Assert.That(oldWorld == null, Is.True);
            Assert.That(oldMap == null, Is.True);
            Assert.That(View("_world").Root.gameObject.activeSelf, Is.True);
            Assert.That(View("_map").Root.gameObject.activeSelf, Is.False);
            RectTransform world = View("_world").Root;
            RectTransform map = View("_map").Root;
            _presenter.LateDispose();
            Advance(1f);
            Assert.That(world == null, Is.True);
            Assert.That(map == null, Is.True);
            Assert.That(GetField<StructureStatusIconView[]>(_presenter, "_world"), Is.Empty);
        }

        [Test]
        public void MissingMinimapViewDefersPrewarmAndDoesNotConsumeTime()
        {
            ShowAt(450f, 325f);
            SetField(_controller, "_miniMapView", null);
            Advance(10f);
            AssertClocks(2f, 0f);
            Assert.That(GetField<StructureStatusIconView[]>(_presenter, "_world"), Is.Empty);
            SetField(_controller, "_miniMapView", _map);
            Advance(10f);
            AssertClocks(2f, 0f);
        }

        private void ShowAt(float x, float y, CaptureSiteStatusTransition transition = CaptureSiteStatusTransition.Built,
            float depth = 10f) => _events.Emit(_camera.ScreenToWorldPoint(new Vector3(x, y - 42f, depth)), transition);

        [Test]
        public void PresenterAppliesFadeAlphaToWorldAndMinimapGraphics()
        {
            ShowAt(450f, 325f);
            Advance(0f);
            AssertViewAlpha("_world", 0f);
            AssertViewAlpha("_map", 0f);
            Advance(0.1f);
            AssertViewAlpha("_world", 0.5f);
            AssertViewAlpha("_map", 0.5f);
            Advance(0.1f);
            AssertViewAlpha("_world", 1f);
            AssertViewAlpha("_map", 1f);
            _sources.Model.Dismiss(0);
            Advance(0.15f);
            AssertViewAlpha("_world", 0.5f);
            AssertViewAlpha("_map", 0.5f);
            Advance(0.16f);
            Assert.That(View("_world").Root.gameObject.activeSelf, Is.False);
            Assert.That(View("_map").Root.gameObject.activeSelf, Is.False);
        }

        private void AssertViewAlpha(string field, float expected)
        {
            foreach (string graphic in new[] { "_glyph", "_badge", "_background" })
                Assert.That(GetField<Image>(View(field), graphic).color.a,
                    Is.EqualTo(expected).Within(0.00001f));
        }

        private void Advance(float seconds) => typeof(StructureStatusIconPresenter)
            .GetMethod("Advance", PRIVATE_INSTANCE).Invoke(_presenter, new object[] { seconds });

        private StructureStatusIconView View(string field) => GetField<StructureStatusIconView[]>(_presenter, field)[0];

        private void AssertClocks(float world, float map)
        {
            StructureStatusIconRecord record = _sources.Model.Get(0);
            Assert.That(record.WorldSecondsLeft, Is.EqualTo(world).Within(0.0001f));
            Assert.That(record.MinimapSeconds, Is.EqualTo(map).Within(0.0001f));
        }

        private void SetGate(string gate, bool hidden)
        {
            if (gate == "hud") _gates.IsHudVisible = !hidden;
            else if (gate == "cinematic") _gates.IsActive = hidden;
            else _gates.IsLocked = hidden;
        }

        private static void SetField(object target, string name, object value) =>
            target.GetType().GetField(name, PRIVATE_INSTANCE).SetValue(target, value);

        private static T GetField<T>(object target, string name) =>
            (T)target.GetType().GetField(name, PRIVATE_INSTANCE).GetValue(target);

        private sealed class Gates : IHudVisibilityObserver, ICinematicCameraModelObserver, IInputLock
        {
            public bool IsHudVisible { get; set; } = true;
            public bool IsActive { get; set; }
            public bool IsLocked { get; set; }
            public event Action<bool> LockChanged { add { } remove { } }
            public IDisposable Acquire() => throw new NotSupportedException();
            public IDisposable AcquireBattle() => throw new NotSupportedException();
        }

        private sealed class SiteEvents : ICaptureSiteStatusEvents
        {
            private readonly List<Site> _sites = new List<Site> { new Site() };
            public event Action SitesChanged;
            public event Action<CaptureSiteStatusChange> StatusChanged;
            public IReadOnlyList<ICaptureSite> Sites => _sites;
            public void Rebuild() => SitesChanged.Invoke();
            public void AddSite() { _sites.Add(new Site()); Rebuild(); }
            public void Emit(Vector3 position, CaptureSiteStatusTransition transition, int index = 0)
            {
                Site site = _sites[index];
                site.FacilityPosition = position;
                StatusChanged.Invoke(new CaptureSiteStatusChange(site, position, TestPlayers.Human,
                    TestPlayers.Human, transition));
            }
        }

        private sealed class RelayEvents : IRelayStatusEvents
        {
            public event Action<RelayOwnershipChange> RelayOwnershipChanged { add { } remove { } }
            public event Action RelaySourcesChanged { add { } remove { } }
            public IReadOnlyList<ReinforcementZonePresenter> Zones => Array.Empty<ReinforcementZonePresenter>();
        }

        private sealed class Site : ICaptureSite
        {
            public CaptureSiteKind Kind => CaptureSiteKind.Mining;
            public int PointId => 0;
            public PlayerId Owner => TestPlayers.Human;
            public CaptureSiteState State => CaptureSiteState.Owned;
            public SiteFacilityType FacilityType => SiteFacilityType.Mining;
            public float CaptureProgress => 0f;
            public float ConstructionProgress => 0f;
            public Vector3 Center => FacilityPosition;
            public Vector3 FacilityPosition { get; set; }
            public bool IsRevealed => false;
            public bool IsOperational => false;
            public float PointerHitDistance(Ray ray) => throw new NotSupportedException();
            public bool Contains(Vector3 position, float clearance = 0f) => throw new NotSupportedException();
        }
    }
}
