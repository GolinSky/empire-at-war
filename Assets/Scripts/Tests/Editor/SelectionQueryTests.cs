using System;
using System.Collections.Generic;
using EmpireAtWar.Components.Selection.Marquee;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Ship.Selection;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
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

        private static SelectionQuery CreateQuery(IEntityLocator locator)
        {
            return new SelectionQuery(
                new FakeCamera(),
                locator,
                TestPlayers.CreateLocalPlayer(TestPlayers.CreateDuel()));
        }

        private sealed class FakeCamera : ICameraService
        {
            public string Id => nameof(FakeCamera);
            public Vector3 CameraPosition => default;
            public Transform CameraTransform => null;
            public Vector3 CameraForward => Vector3.forward;
            public float FieldOfView => 60f;
            public UnityEngine.Camera SceneCamera => null;
            public float ZoomTargetHeight => 0f;

            public Vector3 GetWorldPoint(Vector2 point, Vector3 origin) => default;

            public RaycastHit ScreenPointToRay(Vector2 point) => default;

            public int ScreenPointRaycastAll(Vector2 point, RaycastHit[] results) => 0;

            public Vector3 WorldToViewportPoint(Vector3 point) => new Vector3(0.5f + point.x, 0.5f, 1f);

            public Vector2 WorldToScreenPoint(Vector3 point) => new Vector2(50f, 50f);

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

        private sealed class FakeEntity : IEntity
        {
            private readonly FakeSelection _selection;
            private readonly IUnitTypeFacade _unitType;

            public long Id { get; }
            public IHealthModelObserver HealthModel { get; } = new FakeHealth();
            public PlayerId Owner => TestPlayers.Human;

            public FakeEntity(long id, SelectionType selectionType, bool isUnit, Vector3 worldPosition = default)
            {
                Id = id;
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

                command = default;
                return false;
            }
        }

        private sealed class FakeHealth : IHealthModelObserver
        {
            public event Action OnDestroy { add { } remove { } }

            public event Action OnValueChanged { add { } remove { } }

            public HardPointModel[] HardPointModels => Array.Empty<HardPointModel>();
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
