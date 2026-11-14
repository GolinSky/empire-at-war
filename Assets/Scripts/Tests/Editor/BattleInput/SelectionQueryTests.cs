using System;
using System.Collections.Generic;
using EmpireAtWar.Components.Selection.Marquee;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Ship.Selection;
using EmpireAtWar.Components.Squadrons.Icon;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.SpaceStation;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Battle;
using EmpireAtWar.Services.Camera;
using EmpireAtWar.Services.Selection;
using NUnit.Framework;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    [Category("BattleInput")]
    public sealed class SelectionQueryTests
    {
        private static readonly MarqueeRectangle WHOLE_SCREEN =
            new MarqueeRectangle(new MarqueePoint(0f, 0f), new MarqueePoint(100f, 100f));

        private static readonly Vector3 OFF_SCREEN_POSITION = new Vector3(1000f, 0f, 0f);

        [TestCase(-502f, 600f, true)]
        [TestCase(0f, 600f, false)]
        [TestCase(-234f, 600f, false)]
        [TestCase(-502f, 50f, false)]
        public void CollectAt_OnlyRanksDeepUnitsBehindStationAsStructures(float shipHitY, float shipDistance,
            bool stationFirst)
        {
            var locator = new HitLocator();
            var ship = new FakeEntity(1, SelectionType.Ship, true);
            var station = new FakeEntity(2, SelectionType.Base, false, extraFacade: new FakeStationHull());
            locator.AddEntity(ship);
            locator.AddEntity(station);
            var camera = new FakeCamera(null)
            {
                Hits = new[] { Hit(ship, shipHitY, shipDistance), Hit(station, -100f, 100f) }
            };
            var query = new SelectionQuery(camera, locator, TestPlayers.CreateLocalPlayer(TestPlayers.CreateDuel()),
                new FakeMarkerLayer());
            var results = new List<SelectionEntry>();

            query.CollectAt(Vector2.zero, results);

            Assert.That(results, Has.Count.EqualTo(2));
            Assert.That(results[0].Entity, Is.SameAs(stationFirst ? station : ship));
        }

        [Test]
        public void CollectAt_IconPrecedesStationAndDeepShip_ExposedShipRemainsSelectable()
        {
            var locator = new HitLocator();
            var ship = new FakeEntity(1, SelectionType.Ship, true);
            var station = new FakeEntity(2, SelectionType.Base, false, extraFacade: new FakeStationHull());
            var squadron = new FakeEntity(3, SelectionType.Ship, true);
            locator.AddEntity(ship);
            locator.AddEntity(station);
            locator.AddEntity(squadron);
            var camera = new FakeCamera(null)
            {
                Hits = new[] { Hit(ship, -502f, 600f), Hit(station, -100f, 100f) }
            };
            var markers = new FakeMarkerLayer();
            markers.Register(new FakeMarker(squadron.Id, new Rect(-0.5f, -0.5f, 1f, 1f)));
            var query = new SelectionQuery(camera, locator, TestPlayers.CreateLocalPlayer(TestPlayers.CreateDuel()), markers);
            var results = new List<SelectionEntry>();

            query.CollectAt(Vector2.zero, results);

            Assert.That(results.ConvertAll(entry => entry.Entity), Is.EqualTo(new IEntity[] { squadron, station, ship }));

            camera.Hits = new[] { Hit(ship, -502f, 600f) };
            Assert.That(query.TryFindAt(Vector2.one, out SelectionEntry exposed), Is.True);
            Assert.That(exposed.Entity, Is.SameAs(ship));
        }

        private static RaycastHit Hit(IEntity entity, float y, float distance) =>
            new RaycastHit { point = new Vector3(entity.Id, y, 0f), distance = distance };

        private sealed class HitLocator : IEntityLocator
        {
            private readonly Dictionary<long, IEntity> _entities = new Dictionary<long, IEntity>();
            public string Id => nameof(HitLocator);
            public event Action<IEntity> EntityAdded { add { } remove { } }
            public event Action<IEntity> EntityRemoved { add { } remove { } }
            public Dictionary<long, IEntity>.ValueCollection Entities => _entities.Values;
            public void AddEntity(IEntity entity) => _entities.Add(entity.Id, entity);
            public void RemoveEntity(IEntity entity) => _entities.Remove(entity.Id);
            public IEntity GetEntity(long entityId) => _entities[entityId];
            public bool TryGetEntity(long entityId, out IEntity entity) => _entities.TryGetValue(entityId, out entity);
            public bool TryGetEntity(RaycastHit hit, out IEntity entity) => TryGetEntity((long)hit.point.x, out entity);
            public bool TryGetEntity(Collider collider, out IEntity entity) => throw new NotSupportedException();
        }

        private sealed class FakeStationHull : IStationHullFacade
        {
            public float HullBottom => -234f;
            public float HullTop => 0f;
            public float GetHullRadius(Vector3 center) => 100f;
        }

        private sealed class FakeMarkerLayer : IBattleMarkerLayer
        {
            private readonly List<IBattleMarker> _markers = new List<IBattleMarker>();
            public IReadOnlyList<IBattleMarker> Markers => _markers;
            public BattleMarker CreateShipMarker(Sprite icon, Transform anchor) => throw new NotSupportedException();
            public void Attach(RectTransform root) => throw new NotSupportedException();
            public void Register(IBattleMarker marker) => _markers.Add(marker);
            public void Unregister(IBattleMarker marker) => _markers.Remove(marker);
            public void InvalidateMembership(long entityId) => throw new NotSupportedException();
        }

        private sealed class FakeMarker : IBattleMarker
        {
            public long EntityId { get; }
            public bool IsSquadron { get; set; } = true;
            public bool IsShown => true;
            public Rect ScreenRect { get; set; }
            public int DrawOrder { get; set; }
            public FakeMarker(long entityId, Rect screenRect)
            {
                EntityId = entityId;
                ScreenRect = screenRect;
            }
            public bool ContainsScreenPoint(Vector2 point) => ScreenRect.Contains(point);
            public bool OverlapsMarquee(MarqueeRectangle rectangle) =>
                ScreenRect.Overlaps(new Rect(rectangle.MinX, rectangle.MinY, rectangle.Width, rectangle.Height));
        }

        [Test]
        public void CollectInside_DropsStructuresWhenUnitsAreInside()
        {
            EntityLocator locator = new EntityLocator();
            FakeEntity ship = new FakeEntity(id: 1, SelectionType.Ship, isUnit: true);
            locator.AddEntity(ship);
            locator.AddEntity(new FakeEntity(id: 2, SelectionType.DefendPlatform, isUnit: false));
            locator.AddEntity(new FakeEntity(id: 3, SelectionType.MiningFacility, isUnit: false));
            List<SelectionEntry> results = new List<SelectionEntry>();

            CreateQuery(locator).CollectInside(WHOLE_SCREEN, results);

            Assert.That(results, Has.Count.EqualTo(1));
            Assert.That(results[0].Entity, Is.SameAs(ship));
        }

        [Test]
        public void CollectInside_BoxAroundOffsetIcon_SelectsSquadronOnceAndPrefersItToStructures()
        {
            var locator = new EntityLocator();
            var squadron = new FakeEntity(1, SelectionType.Ship, isUnit: true);
            locator.AddEntity(squadron);
            locator.AddEntity(new FakeEntity(2, SelectionType.MiningFacility, isUnit: false,
                worldPosition: new Vector3(-30f, 30f, 0f)));
            var results = new List<SelectionEntry>();

            var box = new MarqueeRectangle(new MarqueePoint(25f, 85f), new MarqueePoint(15f, 75f));
            var markers = new FakeMarkerLayer();
            markers.Register(new FakeMarker(squadron.Id, new Rect(15f, 75f, 10f, 10f)));
            SelectionQuery query = CreateQuery(locator, markers: markers);
            query.CollectInside(box, results);
            Assert.That(results, Has.Count.EqualTo(1));
            Assert.That(results[0].Entity, Is.SameAs(squadron));

            results.Clear();
            query.CollectInside(WHOLE_SCREEN, results);
            Assert.That(results, Has.Count.EqualTo(1));
            Assert.That(results[0].Entity, Is.SameAs(squadron));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void MarkerPointAndMarquee_UseCurrentDisplayedBounds(bool concealed)
        {
            var locator = new EntityLocator();
            var fog = new FakeFog();
            var squadron = new FakeEntity(1, SelectionType.Ship, isUnit: true, extraFacade: fog);
            locator.AddEntity(squadron);
            var marker = new FakeMarker(squadron.Id, new Rect(10f, 10f, 20f, 20f));
            var markers = new FakeMarkerLayer();
            markers.Register(marker);
            SelectionQuery query = CreateQuery(locator, markers: markers);
            Vector2 originalCell = marker.ScreenRect.center;
            Assert.That(query.TryFindMarkerAt(originalCell, out SelectionEntry captured), Is.True);
            Assert.That(captured.Entity, Is.SameAs(squadron));

            marker.ScreenRect = new Rect(30f, 10f, 20f, 20f);
            Assert.That(query.TryFindAt(originalCell, out _), Is.False);
            Assert.That(query.IsMarkerAvailable(captured), Is.True);
            Assert.That(query.TryFindAt(marker.ScreenRect.center, out SelectionEntry hit), Is.True);
            Assert.That(hit.Entity, Is.SameAs(squadron));

            var results = new List<SelectionEntry>();
            var oldCell = new MarqueeRectangle(new MarqueePoint(11f, 11f), new MarqueePoint(29f, 29f));
            var newCell = new MarqueeRectangle(new MarqueePoint(31f, 11f), new MarqueePoint(49f, 29f));
            query.CollectInside(oldCell, results);
            Assert.That(results, Is.Empty);
            query.CollectInside(newCell, results);
            Assert.That(results, Has.Count.EqualTo(1));
            Assert.That(results[0].Entity, Is.SameAs(squadron));

            if (concealed)
            {
                fog.IsHiddenByFog = true;
                Assert.That(query.TryFindAt(marker.ScreenRect.center, out _), Is.False);
            }
            else
            {
                markers.Unregister(marker);
                marker.DrawOrder++;
                markers.Register(marker);
                Assert.That(query.IsMarkerAvailable(captured), Is.False);

                Assert.That(query.TryFindAt(marker.ScreenRect.center, out captured), Is.True);
                locator.RemoveEntity(squadron);
                locator.AddEntity(new FakeEntity(squadron.Id, SelectionType.Ship, isUnit: true));
            }
            Assert.That(query.IsMarkerAvailable(captured), Is.False);
        }

        [Test]
        public void MarkerHit_PrioritizesFixedShipOverSquadronInTheSameScreenArea()
        {
            var locator = new EntityLocator();
            var squadron = new FakeEntity(1, SelectionType.Ship, isUnit: true);
            var ship = new FakeEntity(2, SelectionType.Ship, isUnit: true);
            locator.AddEntity(squadron);
            locator.AddEntity(ship);
            var markers = new FakeMarkerLayer();
            var rect = new Rect(10f, 10f, 20f, 20f);
            markers.Register(new FakeMarker(squadron.Id, rect) { DrawOrder = 100 });
            markers.Register(new FakeMarker(ship.Id, rect) { IsSquadron = false });
            SelectionQuery query = CreateQuery(locator, markers: markers);

            Assert.That(query.TryFindAt(rect.center, out SelectionEntry hit), Is.True);
            Assert.That(hit.Entity, Is.SameAs(ship));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void CollectInside_SelectsRotatedCapitalHullTipAndSkipsEmptyBoundingBoxCorner(bool orthographic)
        {
            GameObject cameraObject = new GameObject("Selection camera");
            GameObject ship = new GameObject("Capital ship");
            try
            {
                var camera = cameraObject.AddComponent<UnityEngine.Camera>();
                camera.pixelRect = new Rect(0f, 0f, 200f, 200f);
                camera.orthographic = orthographic;
                camera.orthographicSize = 10f;
                camera.fieldOfView = 90f;
                camera.aspect = 1f;
                ship.transform.SetPositionAndRotation(new Vector3(0f, 0f, 10f), Quaternion.Euler(0f, 0f, 45f));
                var hull = new ShipMarqueeHull(ship.transform, new Bounds(Vector3.zero, new Vector3(8f, 1f, 1f)));
                var capital = new FakeEntity(1, SelectionType.Ship, isUnit: true, extraFacade: hull);
                var locator = new EntityLocator();
                locator.AddEntity(capital);
                SelectionQuery query = CreateQuery(locator, camera);
                var results = new List<SelectionEntry>();

                // At 10 pixels per world unit the tip is near (128,128); the opposite top corner is empty.
                var tip = new MarqueeRectangle(new MarqueePoint(130f, 130f), new MarqueePoint(125f, 125f));
                query.CollectInside(tip, results);
                Assert.That(results, Has.Count.EqualTo(1));
                Assert.That(results[0].Entity, Is.SameAs(capital));

                results.Clear();
                var empty = new MarqueeRectangle(new MarqueePoint(70f, 125f), new MarqueePoint(75f, 130f));
                query.CollectInside(empty, results);
                Assert.That(results, Is.Empty);

                ship.transform.position = new Vector3(0f, 0f, -10f);
                query.CollectInside(tip, results);
                Assert.That(results, Is.Empty);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(ship);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        [Test]
        public void CollectInside_SelectsStructuresWhenNoUnitIsInside()
        {
            EntityLocator locator = new EntityLocator();
            locator.AddEntity(new FakeEntity(id: 2, SelectionType.DefendPlatform, isUnit: false));
            locator.AddEntity(new FakeEntity(id: 3, SelectionType.MiningFacility, isUnit: false));
            List<SelectionEntry> results = new List<SelectionEntry>();

            CreateQuery(locator).CollectInside(WHOLE_SCREEN, results);

            Assert.That(results, Has.Count.EqualTo(2));
        }

        [Test]
        public void CollectSameShipType_SkipsUnitsOutsideTheScreen()
        {
            EntityLocator locator = new EntityLocator();
            FakeEntity tapped = new FakeEntity(id: 1, SelectionType.Ship, isUnit: true);
            FakeEntity visible = new FakeEntity(id: 2, SelectionType.Ship, isUnit: true);
            FakeEntity offScreen = new FakeEntity(id: 3, SelectionType.Ship, isUnit: true, OFF_SCREEN_POSITION);
            locator.AddEntity(tapped);
            locator.AddEntity(visible);
            locator.AddEntity(offScreen);
            List<SelectionEntry> results = new List<SelectionEntry>();

            CreateQuery(locator).CollectSameShipType(
                new SelectionEntry(tapped, tapped.GetFacade<IEntitySelectionFacade>()),
                results);

            Assert.That(results, Has.Count.EqualTo(2));
            Assert.That(results[0].Entity, Is.SameAs(tapped));
            Assert.That(results[1].Entity, Is.SameAs(visible));
        }

        private static SelectionQuery CreateQuery(IEntityLocator locator, UnityEngine.Camera camera = null,
            FakeMarkerLayer markers = null)
        {
            return new SelectionQuery(
                new FakeCamera(camera),
                locator,
                TestPlayers.CreateLocalPlayer(TestPlayers.CreateDuel()),
                markers ?? new FakeMarkerLayer());
        }

        private sealed class FakeCamera : ICameraService
        {
            public RaycastHit[] Hits { get; set; } = Array.Empty<RaycastHit>();
            public string Id => nameof(FakeCamera);
            public Vector3 CameraPosition => default;
            public Transform CameraTransform => null;
            public Vector3 CameraForward => Vector3.forward;
            public float FieldOfView => 60f;
            public UnityEngine.Camera SceneCamera { get; }
            public FakeCamera(UnityEngine.Camera camera) => SceneCamera = camera;
            public float ZoomTargetHeight => 0f;

            public Vector3 GetWorldPoint(Vector2 point, Vector3 origin) => default;

            public RaycastHit ScreenPointToRay(Vector2 point) => default;

            public int ScreenPointRaycastAll(Vector2 point, RaycastHit[] results)
            {
                int count = Math.Min(Hits.Length, results.Length);
                Array.Copy(Hits, results, count);
                return count;
            }

            public Vector3 WorldToViewportPoint(Vector3 point) => new Vector3(0.5f + point.x, 0.5f, 1f);

            public Vector2 WorldToScreenPoint(Vector3 point) => new Vector2(50f + point.x, 50f + point.y);

            public IReadOnlyList<Vector3> GetGroundFootprint(Vector2 min, Vector2 max) =>
                Array.Empty<Vector3>();

            public void MoveTo(Vector3 point) { }

            public void SetPose(Vector3 position, Quaternion rotation) { }
            public void RestorePose(Vector3 position, Quaternion rotation, float zoomTargetHeight) { }

            public void SetCullingMask(LayerMask mask) { }
        }

        private sealed class FakeSelection : IEntitySelectionFacade, ISelectionPositionProvider
        {
            public SelectionType SelectionType { get; set; }
            public Vector3 WorldPosition { get; }


            public FakeSelection(SelectionType selectionType, Vector3 worldPosition)
            {
                SelectionType = selectionType;
                WorldPosition = worldPosition;
            }

            public void Select(bool isSelected) { }
        }

        private sealed class FakeFog : IFogVisibilityFacade
        {
            public event Action<bool> VisibilityChanged { add { } remove { } }
            public bool IsHiddenByFog { get; set; }
        }

        private sealed class FakeEntity : IEntity
        {
            private readonly FakeSelection _selection;
            private readonly IUnitTypeFacade _unitType;
            private readonly IEntityFacade _extraFacade;

            public long Id { get; }
            public IHealthModelObserver HealthModel { get; } = new FakeHealth();
            public PlayerId Owner => TestPlayers.Human;

            public FakeEntity(long id, SelectionType selectionType, bool isUnit, Vector3 worldPosition = default,
                IEntityFacade extraFacade = null)
            {
                Id = id;
                _extraFacade = extraFacade;
                _selection = new FakeSelection(selectionType, worldPosition);
                _unitType = isUnit ? new UnitTypeFacade(UnitTypeId.Ship(ShipType.Venator)) : null;
            }

            public TCommand GetFacade<TCommand>() where TCommand : IEntityFacade
            { TryGetFacade(out TCommand facade); return facade; }

            public bool TryGetFacade<TCommand>(out TCommand command)
                where TCommand : IEntityFacade
            {
                if (_selection is TCommand selection)
                {
                    command = selection;
                    return true;
                }

                if (_unitType is TCommand unitType)
                {
                    command = unitType;
                    return true;
                }

                if (_extraFacade is TCommand extra)
                {
                    command = extra;
                    return true;
                }

                command = default;
                return false;
            }
        }

        private sealed class FakeHealth : IHealthModelObserver
        {
            public event Action OnDestroy { add { } remove { } }

            public event Action OnValueChanged { add { } remove { } }

            public IReadOnlyList<IReadOnlyHardPointModel> HardPointModels => Array.Empty<HardPointModel>();
            public float Hull => 1f;
            public ShipClass ShipClass => ShipClass.Capital;
            public bool HasLiveHardPoints => true;
            public float HullPercentage => 1f;
            public float Shields => 1f;
            public float ShieldPercentage => 1f;
            public bool IsDestroyed => false;
            public bool IsLostShieldGenerator => false;
            public bool HasUnits => true;
            public PlayerId Owner => TestPlayers.Human;
            public bool HasShields => true;

            public IHardPointModel[] GetShipUnits(HardPointType type) =>
                Array.Empty<IHardPointModel>();
        }
    }
}
