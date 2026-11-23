using System;
using System.Collections.Generic;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Selection.Marquee;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Ship.Health.HardPointOverlay;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.BaseEntity.Orders;
using EmpireAtWar.Entities.CinematicCamera.Model;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Battle;
using EmpireAtWar.Services.Camera;
using EmpireAtWar.Services.Input;
using EmpireAtWar.Services.UnitOrders;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Health")]
    public sealed class HardPointOverlayPresenterTests
    {
        private readonly List<UnityEngine.Object> _created = new List<UnityEngine.Object>();
        private FakeQuery _query;
        private FakePointer _pointer;
        private HardPointOverlayModel _model;
        private HardPointOverlayPresenter _presenter;

        [SetUp]
        public void SetUp()
        {
            HardPointOverlayData data = ScriptableObject.CreateInstance<HardPointOverlayData>();
            _created.Add(data);
            SerializedObject serialized = new SerializedObject(data);
            SerializedProperty entries = serialized.FindProperty("entries");
            entries.arraySize = 1;
            entries.GetArrayElementAtIndex(0).FindPropertyRelative("hardPointType").intValue = (int)HardPointType.Weapon;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            _query = new FakeQuery();
            _pointer = new FakePointer();
            _model = new HardPointOverlayModel();
            _presenter = new HardPointOverlayPresenter(new FakeView(), _query, _pointer, new FakeCamera(),
                new FakeOrders(), new InactiveCinematicCamera(),
                TestPlayers.CreateLocalPlayer(TestPlayers.CreateDuel()), _model, data);
            _presenter.Initialize();
        }

        [TearDown]
        public void TearDown()
        {
            _presenter.LateDispose();
            foreach (UnityEngine.Object created in _created) UnityEngine.Object.DestroyImmediate(created);
            _created.Clear();
        }

        [Test]
        public void ShipInFrontOfAnInspectedHullTakesTheHoverFromItsIcons()
        {
            // The big hull's icon sits under the cursor; the ship flying above it is nearer the camera.
            FakeEntity big = CreateShip(1, Vector3.zero);
            FakeEntity above = CreateShip(2, new Vector3(500f, 500f, 0f));
            _query.Target = big;
            _presenter.Tick();
            Assert.That(_model.TryGetHovered(out IEntity first, out _), Is.True);
            Assert.That(first, Is.SameAs(big));

            _query.Target = above;
            _presenter.Tick();

            Assert.That(_model.InspectedShip, Is.SameAs(above));
            Assert.That(_model.TryGetHovered(out _, out _), Is.False);
        }

        [Test]
        public void IconBeyondTheHullKeepsItsShipInspectedOverEmptySpace()
        {
            FakeEntity ship = CreateShip(1, Vector3.zero);
            _query.Target = ship;
            _presenter.Tick();

            _query.Target = null;
            _presenter.Tick();

            Assert.That(_model.TryGetHovered(out IEntity hovered, out int hardPointId), Is.True);
            Assert.That(hovered, Is.SameAs(ship));
            Assert.That(hardPointId, Is.Zero);
        }

        private FakeEntity CreateShip(long id, Vector3 hardPointPosition)
        {
            GameObject view = new GameObject("Hardpoint overlay test ship");
            _created.Add(view);
            return new FakeEntity(id, view.transform, new FakeHardPoint(hardPointPosition));
        }

        private sealed class FakeEntity : IEntity, IHardPointsFacade, IEntityTransformFacade
        {
            private readonly IHardPointStatus[] _hardPoints;

            public FakeEntity(long id, Transform transform, IHardPointStatus hardPoint)
            {
                Id = id;
                Transform = transform;
                _hardPoints = new[] { hardPoint };
            }

            public long Id { get; }
            public PlayerId Owner => TestPlayers.Enemy;
            public IHealthModelObserver HealthModel { get; } = new FakeHealth();
            public IReadOnlyList<IHardPointStatus> HardPoints => _hardPoints;
            public Transform Transform { get; }

            public TFacade GetFacade<TFacade>() where TFacade : IEntityFacade
            {
                if (!TryGetFacade(out TFacade facade)) throw new InvalidOperationException(typeof(TFacade).Name);
                return facade;
            }

            public bool TryGetFacade<TFacade>(out TFacade facade) where TFacade : IEntityFacade
            {
                if (this is TFacade match)
                {
                    facade = match;
                    return true;
                }

                facade = default;
                return false;
            }
        }

        private sealed class FakeHardPoint : IHardPointStatus
        {
            public FakeHardPoint(Vector3 position) => Position = position;

            public event Action OnHardPointHealthChanged { add { } remove { } }
            public event Action OnDestroyed { add { } remove { } }

            public HardPointType HardPointType => HardPointType.Weapon;
            public float HealthPercentage => 1f;
            public int Id => 0;
            public int Generation => 0;
            public bool IsDestroyed => false;
            public Vector3 Position { get; }
            public Transform Transform => null;
            public Transform Pivot => null;
            public float Health => 1f;
            public float MaxHealth => 1f;
            public bool IsInstalled => true;

            public bool TryGetWeaponType(out WeaponType weaponType)
            {
                weaponType = default;
                return false;
            }

            public bool TryGetWeaponProfile(out WeaponProfile profile)
            {
                profile = null;
                return false;
            }
        }

        private sealed class FakeHealth : IHealthModelObserver
        {
            public event Action OnDestroy { add { } remove { } }
            public event Action OnValueChanged { add { } remove { } }

            public IReadOnlyList<IReadOnlyHardPointModel> HardPointModels => Array.Empty<HardPointModel>();
            public float Hull => 1f;
            public ShipClass ShipClass => ShipClass.Dreadnought;
            public bool HasLiveHardPoints => true;
            public float HullPercentage => 1f;
            public float Shields => 0f;
            public float ShieldPercentage => 0f;
            public bool IsDestroyed => false;
            public bool IsLostShieldGenerator => false;
            public bool HasUnits => true;
            public PlayerId Owner => TestPlayers.Enemy;
            public bool HasShields => false;

            public IHardPointModel[] GetShipUnits(HardPointType type) => Array.Empty<IHardPointModel>();
        }

        private sealed class FakeQuery : ISelectionQuery
        {
            public IEntity Target { get; set; }

            public bool TryFindAt(Vector2 screenPosition, out SelectionEntry selection)
            {
                selection = Target == null ? default : new SelectionEntry(Target, null);
                return Target != null;
            }

            public bool TryFindMarkerAt(Vector2 screenPosition, out SelectionEntry selection) =>
                throw new NotSupportedException();

            public bool IsMarkerAvailable(SelectionEntry selection) => throw new NotSupportedException();
            public void CollectAt(Vector2 screenPosition, IList<SelectionEntry> results) => throw new NotSupportedException();

            public void CollectSameShipType(SelectionEntry selected, ICollection<SelectionEntry> results) =>
                throw new NotSupportedException();

            public void CollectAllPlayerUnits(ICollection<SelectionEntry> results) => throw new NotSupportedException();
            public void CollectVisiblePlayerUnits(ICollection<SelectionEntry> results) => throw new NotSupportedException();

            public void CollectInside(MarqueeRectangle rectangle, ICollection<SelectionEntry> results) =>
                throw new NotSupportedException();
        }

        private sealed class FakePointer : IPointerInput
        {
            public event Action<Vector2> PrimaryPressed { add { } remove { } }
            public event Action<Vector2> PrimaryReleased { add { } remove { } }

            public Vector2 Position => Vector2.zero;
            public bool HasPosition => true;
            public int ClickCount => 0;
        }

        private sealed class FakeView : IHardPointOverlayView
        {
            public float ScaleFactor => 1f;
            public void ShowMarker(int slot, HardPointMarkerData data) { }
            public void HideMarkersFrom(int slot) { }
        }

        private sealed class InactiveCinematicCamera : ICinematicCameraModelObserver
        {
            public bool IsActive => false;
        }

        private sealed class FakeCamera : ICameraService
        {
            public string Id => nameof(FakeCamera);
            public Vector3 CameraPosition => Vector3.zero;
            public Transform CameraTransform => null;
            public Vector3 CameraForward => Vector3.forward;
            public float FieldOfView => 60f;
            public UnityEngine.Camera SceneCamera => null;
            public float ZoomTargetHeight => 0f;

            public Vector3 GetWorldPoint(Vector2 screenPoint, Vector3 position) => throw new NotSupportedException();
            public RaycastHit ScreenPointToRay(Vector2 screenPoint) => throw new NotSupportedException();
            public int ScreenPointRaycastAll(Vector2 screenPoint, RaycastHit[] results) => throw new NotSupportedException();

            // Every point lies in front of the camera at a fixed depth and projects onto its own x/y.
            public Vector3 WorldToViewportPoint(Vector3 position) => new Vector3(position.x, position.y, 100f);
            public Vector2 WorldToScreenPoint(Vector3 position) => new Vector2(position.x, position.y);

            public IReadOnlyList<Vector3> GetGroundFootprint(Vector2 mapMin, Vector2 mapMax) =>
                throw new NotSupportedException();

            public void MoveTo(Vector3 worldPoint) => throw new NotSupportedException();
            public void SetPose(Vector3 position, Quaternion rotation) => throw new NotSupportedException();

            public void RestorePose(Vector3 position, Quaternion rotation, float zoomTargetHeight) =>
                throw new NotSupportedException();

            public void SetCullingMask(LayerMask mask) => throw new NotSupportedException();
        }

        private sealed class FakeOrders : IUnitOrderService
        {
            public event Action<UnitOrder> OrderIssued { add { } remove { } }

            public void IssueMove(IReadOnlyList<IEntity> receivers, Vector3 point, MovePace pace = MovePace.Formation) =>
                throw new NotSupportedException();

            public void IssueMove(IReadOnlyList<IEntity> receivers, IReadOnlyList<Vector3> destinations) =>
                throw new NotSupportedException();

            public void IssueAttack(IReadOnlyList<IEntity> receivers, IEntity target, MovePace pace = MovePace.Formation) =>
                throw new NotSupportedException();

            public void IssueAttack(IReadOnlyList<IEntity> receivers, IEntity target, IReadOnlyList<Vector3> offsets) =>
                throw new NotSupportedException();

            public void IssueHardPointAttack(IReadOnlyList<IEntity> receivers, IEntity target, int hardPointId,
                MovePace pace = MovePace.Formation) => throw new NotSupportedException();

            public void IssueAttackMove(IReadOnlyList<IEntity> receivers, Vector3 point, MovePace pace = MovePace.Formation) =>
                throw new NotSupportedException();

            public void IssueAttackMove(IReadOnlyList<IEntity> receivers, IReadOnlyList<Vector3> destinations) =>
                throw new NotSupportedException();

            public void IssueStop(IReadOnlyList<IEntity> receivers) => throw new NotSupportedException();
            public void IssueGuard(IReadOnlyList<IEntity> receivers, IEntity friendly) => throw new NotSupportedException();

            public void IssueGuard(IReadOnlyList<IEntity> receivers, IEntity friendly, IReadOnlyList<Vector3> offsets) =>
                throw new NotSupportedException();

            public void IssueWaypointMove(IReadOnlyList<IEntity> receivers, IReadOnlyList<Vector3> waypoints) =>
                throw new NotSupportedException();

            public void IssueHunt(IReadOnlyList<IEntity> receivers) => throw new NotSupportedException();
            public void IssueRetreat(IReadOnlyList<IEntity> receivers) => throw new NotSupportedException();
            public void IssueRetreat(IReadOnlyList<IEntity> receivers, Vector3 point) => throw new NotSupportedException();
        }
    }
}
