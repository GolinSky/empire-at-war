using System;
using System.Collections.Generic;
using EmpireAtWar.Components.Selection.Marquee;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Ship.Health.HardPointOverlay;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.SpaceStation;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Battle;
using EmpireAtWar.Services.Input;
using EmpireAtWar.Ui.Base;
using NUnit.Framework;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    [Category("BattleInput")]
    public sealed class UnitHoverServiceTests
    {
        private InputStub _input;
        private QueryStub _query;
        private EntityLocator _entities;
        private UnitHoverService _service;
        private EntityStub _first;
        private EntityStub _second;

        [SetUp]
        public void SetUp()
        {
            _input = new InputStub();
            _query = new QueryStub();
            _entities = new EntityLocator();
            _first = new EntityStub(1);
            _second = new EntityStub(2);
            _entities.AddEntity(_first);
            _entities.AddEntity(_second);
            _service = new UnitHoverService(_query, _input, _input, _input, _input, _input, _entities);
            _service.Initialize();
        }

        [TearDown]
        public void TearDown() => _service.LateDispose();

        [Test]
        public void HoverTransition_ClearsPreviousAndDoesNotRepeatUnchangedState()
        {
            _query.Entity = _first;
            _service.Tick();
            _service.Tick();
            Assert.That(_first.Changes, Is.EqualTo(new[] { true }));
            _query.Entity = _second;
            _service.Tick();
            Assert.That(_first.Changes, Is.EqualTo(new[] { true, false }));
            Assert.That(_second.Changes, Is.EqualTo(new[] { true }));
            Assert.That(_service.TryGetHovered(out IEntity hovered), Is.True);
            Assert.That(hovered, Is.SameAs(_second));
        }

        [Test]
        public void HardPointHover_HighlightsOwnerAndSharesSnapshotWithoutUnitPick()
        {
            _query.Entity = _first;
            _input.HardPointOwner = _second;
            _service.Tick();
            Assert.That(_query.Picks, Is.Zero);
            Assert.That(_service.TryGetHardPoint(out IEntity owner, out int id), Is.True);
            Assert.That(owner, Is.SameAs(_second));
            Assert.That(id, Is.EqualTo(3));
            Assert.That(_second.Changes, Is.EqualTo(new[] { true }));
        }

        [TestCase("hud")]
        [TestCase("ui")]
        [TestCase("drag")]
        [TestCase("fog")]
        [TestCase("dead")]
        public void HoverGate_ClearsHighlight(string gate)
        {
            _query.Entity = _first;
            _service.Tick();
            switch (gate)
            {
                case "hud": _input.IsHudVisible = false; break;
                case "ui": _input.OverUi = true; break;
                case "drag": _input.StartDrag(); break;
                case "fog": _first.IsHiddenByFog = true; break;
                case "dead": _first.Health.IsDestroyed = true; break;
            }
            _service.Tick();
            Assert.That(_first.Changes, Is.EqualTo(new[] { true, false }));
            Assert.That(_service.TryGetHovered(out _), Is.False);
        }

        [Test]
        public void RemovedEntity_ClearsImmediatelyAndStaleHardPointCannotReenter()
        {
            _input.HardPointOwner = _first;
            _service.Tick();
            _entities.RemoveEntity(_first);
            Assert.That(_first.Changes, Is.EqualTo(new[] { true, false }));
            _service.Tick();
            Assert.That(_service.TryGetHovered(out _), Is.False);
            Assert.That(_first.Changes.Count, Is.EqualTo(2));
        }

        [Test]
        public void Dispose_ClearsHighlightAndUnsubscribesDragEvents()
        {
            _query.Entity = _first;
            _service.Tick();
            _service.LateDispose();
            _input.StartDrag();
            Assert.That(_first.Changes, Is.EqualTo(new[] { true, false }));
            Assert.That(_service.CanHover, Is.True);
            Assert.That(_service.TryGetHovered(out _), Is.False);
        }

        private sealed class InputStub : IPointerInput, IPointerGestures, IUiHitTest,
            IHudVisibilityObserver, IHardPointHoverObserver
        {
            public bool IsHudVisible { get; set; } = true;
            public bool OverUi { get; set; }
            public IEntity HardPointOwner { get; set; }
            public Vector2 Position => Vector2.one;
            public bool HasPosition => true;
            public int ClickCount => 1;
            public event Action<Vector2> PrimaryPressed { add { } remove { } }
            public event Action<Vector2> PrimaryReleased { add { } remove { } }
            public event Action<Vector2> WorldPressed { add { } remove { } }
            public event Action<Vector2> WorldClicked { add { } remove { } }
            public event Action<Vector2> WorldCommanded { add { } remove { } }
            public event Action<Vector2> DragStarted;
            public event Action<Vector2> DragChanged { add { } remove { } }
            public event Action<Vector2> DragEnded { add { } remove { } }
            public event Action DragCanceled { add { } remove { } }
            public void StartDrag() => DragStarted?.Invoke(Position);
            public bool IsOverUi(Vector2 point) => OverUi;
            public bool TryGetHovered(out IEntity owner, out int id)
            { owner = HardPointOwner; id = 3; return owner != null; }
        }

        private sealed class QueryStub : ISelectionQuery
        {
            public IEntity Entity { get; set; }
            public int Picks { get; private set; }
            public bool TryFindAt(Vector2 point, out SelectionEntry selection)
            { Picks++; selection = new SelectionEntry(Entity, null); return Entity != null; }
            public void CollectAt(Vector2 point, IList<SelectionEntry> results) => throw new NotSupportedException();
            public void CollectSameShipType(SelectionEntry entry, ICollection<SelectionEntry> results) => throw new NotSupportedException();
            public void CollectAllPlayerUnits(ICollection<SelectionEntry> results) => throw new NotSupportedException();
            public void CollectVisiblePlayerUnits(ICollection<SelectionEntry> results) => throw new NotSupportedException();
            public void CollectInside(MarqueeRectangle rectangle, ICollection<SelectionEntry> results) => throw new NotSupportedException();
        }

        private sealed class EntityStub : IEntity, IEntityHighlightFacade, IFogVisibilityFacade
        {
            public event System.Action<bool> VisibilityChanged { add { } remove { } }
            public long Id { get; }
            public PlayerId Owner => TestPlayers.Human;
            public HealthStub Health { get; } = new HealthStub();
            public IHealthModelObserver HealthModel => Health;
            public bool IsHiddenByFog { get; set; }
            public List<bool> Changes { get; } = new List<bool>();
            public EntityStub(long id) => Id = id;
            public void SetHovered(bool value) => Changes.Add(value);
            public void SetAttackTargeted(bool value) { }
            public T GetFacade<T>() where T : IEntityFacade
            { TryGetFacade(out T facade); return facade; }
            public bool TryGetFacade<T>(out T facade) where T : IEntityFacade
            { if (this is T result) { facade = result; return true; } facade = default; return false; }
        }

        private sealed class HealthStub : IHealthModelObserver
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
            public bool IsDestroyed { get; set; }
            public bool IsLostShieldGenerator => false;
            public bool HasUnits => true;
            public PlayerId Owner => TestPlayers.Human;
            public bool HasShields => true;
            public IHardPointModel[] GetShipUnits(HardPointType type) => Array.Empty<IHardPointModel>();
        }
    }
}
