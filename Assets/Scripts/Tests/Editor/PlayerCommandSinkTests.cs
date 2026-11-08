using System;
using System.Collections.Generic;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Entities.UnitActions;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.PlayerCommands;
using EmpireAtWar.Services.Saves;
using EmpireAtWar.Services.ShipAbilities;
using EmpireAtWar.Services.UnitOrders;
using NUnit.Framework;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    [Category("BattleInput")]
    public sealed class PlayerCommandSinkTests
    {
        private EntityLocator _entities;
        private RecordingOrders _orders;
        private PlayerCommandHandlers _handlers;
        private AuthorityCommandSink _sink;
        private FakeEntity _own;
        private FakeEntity _enemy;

        [SetUp]
        public void SetUp()
        {
            _entities = new EntityLocator();
            _orders = new RecordingOrders();
            _handlers = new PlayerCommandHandlers();
            _sink = new AuthorityCommandSink(_entities, _orders, _handlers);
            _own = new FakeEntity(1, TestPlayers.Human);
            _enemy = new FakeEntity(2, TestPlayers.Enemy);
            _entities.AddEntity(_own);
            _entities.AddEntity(_enemy);
        }

        [Test]
        public void UnitOrder_DropsReceiversNotOwnedByIssuer()
        {
            _sink.Submit(new UnitOrderCommand(TestPlayers.Human, UnitActionId.Move, new long[] { 1, 2 }, Vector3.one));

            Assert.That(_orders.LastAction, Is.EqualTo(UnitActionId.Move));
            Assert.That(_orders.LastReceivers, Is.EqualTo(new IEntity[] { _own }));
            Assert.That(_orders.LastPoint, Is.EqualTo(Vector3.one));
        }

        [Test]
        public void UnitOrder_OnlyForeignOrUnknownReceivers_IssuesNothing()
        {
            _sink.Submit(new UnitOrderCommand(TestPlayers.Enemy, UnitActionId.Stop, new long[] { 1, 99 }, Vector3.zero));

            Assert.That(_orders.LastAction, Is.Null);
        }

        [Test]
        public void UnitOrder_ResolvesTargetById_AndDropsOrderForMissingTarget()
        {
            _sink.Submit(new UnitOrderCommand(TestPlayers.Human, UnitActionId.Attack, new long[] { 1 }, Vector3.zero, targetId: 2));
            Assert.That(_orders.LastAction, Is.EqualTo(UnitActionId.Attack));
            Assert.That(_orders.LastTarget, Is.SameAs(_enemy));

            _orders.LastAction = null;
            _sink.Submit(new UnitOrderCommand(TestPlayers.Human, UnitActionId.Attack, new long[] { 1 }, Vector3.zero, targetId: 77));
            Assert.That(_orders.LastAction, Is.Null);
        }

        [Test]
        public void UnitOrderCommand_Create_StoresIdsNotReferences()
        {
            UnitOrderCommand command = UnitOrderCommand.Create(TestPlayers.Human, UnitActionId.WaypointMove,
                new IEntity[] { _own }, waypoints: new[] { Vector3.left, Vector3.right });

            Assert.That(command.ReceiverIds, Is.EqualTo(new long[] { 1 }));
            Assert.That(command.TargetId, Is.EqualTo(UnitOrderCommand.NO_TARGET));
            Assert.That(command.Waypoints, Is.EqualTo(new[] { Vector3.left, Vector3.right }));
        }

        [Test]
        public void Purchase_RoutesToTheIssuersHandlerOnly()
        {
            RecordingProduction human = new RecordingProduction();
            RecordingProduction enemy = new RecordingProduction();
            _handlers.RegisterProduction(TestPlayers.Human, human);
            _handlers.RegisterProduction(TestPlayers.Enemy, enemy);

            _sink.Submit(new PurchaseUnitCommand(TestPlayers.Human, UnitRequestKind.Ship, 3, 0));
            _sink.Submit(new CancelPurchaseCommand(TestPlayers.Human, "Acclamator"));

            Assert.That(human.Purchases, Is.EqualTo(1));
            Assert.That(human.Cancels, Is.EqualTo(new[] { "Acclamator" }));
            Assert.That(enemy.Purchases + enemy.Cancels.Count, Is.Zero);
        }

        [Test]
        public void Purchase_WithoutHandler_FailsFast()
        {
            Assert.Throws<KeyNotFoundException>(() =>
                _sink.Submit(new PurchaseUnitCommand(TestPlayers.Ally, UnitRequestKind.Ship, 0, 0)));
        }

        [Test]
        public void ShipAbility_DropsForeignCastersBeforeTheHandler()
        {
            RecordingAbilities abilities = new RecordingAbilities();
            _handlers.RegisterAbilities(abilities);

            _sink.Submit(new ShipAbilityCommand(TestPlayers.Human, ShipAbilityId.Cloak, new long[] { 1, 2 },
                UnitOrderCommand.NO_TARGET, false));

            Assert.That(abilities.Casters, Is.EqualTo(new IEntity[] { _own }));
            Assert.That(abilities.Target, Is.Null);
        }

        [Test]
        public void SiteConstruction_PassesIssuerAsPayer()
        {
            RecordingConstruction construction = new RecordingConstruction();
            _handlers.RegisterSiteConstruction(construction);

            _sink.Submit(new BuildSiteFacilityCommand(TestPlayers.Human, 4, SiteFacilityType.Mining));

            Assert.That(construction.Payer, Is.EqualTo(TestPlayers.Human));
            Assert.That(construction.SiteIndex, Is.EqualTo(4));
        }

        private sealed class FakeEntity : IEntity
        {
            public FakeEntity(long id, PlayerId owner)
            {
                Id = id;
                Owner = owner;
            }

            public long Id { get; }
            public IHealthModelObserver HealthModel => null;
            public PlayerId Owner { get; }

            public TFacade GetFacade<TFacade>() where TFacade : IEntityFacade => throw new NotSupportedException();

            public bool TryGetFacade<TFacade>(out TFacade facade) where TFacade : IEntityFacade
            {
                facade = default;
                return false;
            }
        }

        private sealed class RecordingOrders : IUnitOrderService
        {
            public event Action<UnitOrder> OrderIssued { add { } remove { } }

            public UnitActionId? LastAction { get; set; }
            public List<IEntity> LastReceivers { get; private set; }
            public Vector3 LastPoint { get; private set; }
            public IEntity LastTarget { get; private set; }

            private void Record(UnitActionId action, IReadOnlyList<IEntity> receivers, Vector3 point = default,
                IEntity target = null)
            {
                LastAction = action;
                LastReceivers = new List<IEntity>(receivers);
                LastPoint = point;
                LastTarget = target;
            }

            public void IssueMove(IReadOnlyList<IEntity> receivers, Vector3 point) => Record(UnitActionId.Move, receivers, point);
            public void IssueMove(IReadOnlyList<IEntity> receivers, IReadOnlyList<Vector3> destinations) => Record(UnitActionId.Move, receivers);
            public void IssueAttack(IReadOnlyList<IEntity> receivers, IEntity target) => Record(UnitActionId.Attack, receivers, target: target);
            public void IssueAttack(IReadOnlyList<IEntity> receivers, IEntity target, IReadOnlyList<Vector3> offsets) => Record(UnitActionId.Attack, receivers, target: target);
            public void IssueHardPointAttack(IReadOnlyList<IEntity> receivers, IEntity target, int hardPointId) => Record(UnitActionId.Attack, receivers, target: target);
            public void IssueAttackMove(IReadOnlyList<IEntity> receivers, Vector3 point) => Record(UnitActionId.AttackMove, receivers, point);
            public void IssueAttackMove(IReadOnlyList<IEntity> receivers, IReadOnlyList<Vector3> destinations) => Record(UnitActionId.AttackMove, receivers);
            public void IssueStop(IReadOnlyList<IEntity> receivers) => Record(UnitActionId.Stop, receivers);
            public void IssueGuard(IReadOnlyList<IEntity> receivers, IEntity friendly) => Record(UnitActionId.Guard, receivers, target: friendly);
            public void IssueGuard(IReadOnlyList<IEntity> receivers, IEntity friendly, IReadOnlyList<Vector3> offsets) => Record(UnitActionId.Guard, receivers, target: friendly);
            public void IssueWaypointMove(IReadOnlyList<IEntity> receivers, IReadOnlyList<Vector3> waypoints) => Record(UnitActionId.WaypointMove, receivers);
            public void IssueHunt(IReadOnlyList<IEntity> receivers) => Record(UnitActionId.Hunt, receivers);
            public void IssueRetreat(IReadOnlyList<IEntity> receivers) => Record(UnitActionId.Retreat, receivers);
            public void IssueRetreat(IReadOnlyList<IEntity> receivers, Vector3 point) => Record(UnitActionId.Retreat, receivers, point);
        }

        private sealed class RecordingProduction : IProductionCommandHandler
        {
            public int Purchases { get; private set; }
            public List<string> Cancels { get; } = new List<string>();
            public void Purchase(PurchaseUnitCommand command) => Purchases++;
            public void Cancel(CancelPurchaseCommand command) => Cancels.Add(command.RequestId);
        }

        private sealed class RecordingAbilities : IShipAbilityCommandHandler
        {
            public List<IEntity> Casters { get; private set; }
            public IEntity Target { get; private set; }

            public void Execute(IReadOnlyList<IEntity> casters, ShipAbilityId id, IEntity target, bool cancel)
            {
                Casters = new List<IEntity>(casters);
                Target = target;
            }
        }

        private sealed class RecordingConstruction : ISiteConstructionCommandHandler
        {
            public PlayerId Payer { get; private set; }
            public int SiteIndex { get; private set; }

            public bool TryStartAlliedConstruction(int siteIndex, PlayerId payer, SiteFacilityType facilityType)
            {
                SiteIndex = siteIndex;
                Payer = payer;
                return true;
            }
        }
    }
}
