using System;
using System.Collections.Generic;
using EmpireAtWar.Components.Combat;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.UnitActions;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Battle;
using EmpireAtWar.Services.BattleNotifications;
using EmpireAtWar.Services.Selection;
using EmpireAtWar.Services.UnitOrders;
using NUnit.Framework;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Ui")]
    public sealed class GravityWellOrderNotifierTests
    {
        [Test]
        public void LocalMovementOrderWithAnchoredSelection_PostsOneFeedbackCard()
        {
            var notifications = new Notifications();
            var orders = new Orders();
            var interdictor = new Ship(TestPlayers.Human);
            var selection = new Selection(interdictor, new Ship(TestPlayers.Human));
            var notifier = new GravityWellOrderNotifier(notifications, orders, selection,
                TestPlayers.CreateLocalPlayer(TestPlayers.CreateDuel()));
            notifier.Initialize();
            try
            {
                orders.Raise(UnitActionId.Move, TestPlayers.Human);
                Assert.That(notifications.Keys, Is.Empty, "No card while nothing is anchored.");

                interdictor.Modifiers.AddMovementAnchor();
                orders.Raise(UnitActionId.Move, TestPlayers.Human);
                orders.Raise(UnitActionId.WaypointMove, TestPlayers.Human);
                orders.Raise(UnitActionId.AttackMove, TestPlayers.Human);
                orders.Raise(UnitActionId.Guard, TestPlayers.Human);
                orders.Raise(UnitActionId.Hunt, TestPlayers.Human);
                Assert.That(notifications.Keys.Count, Is.EqualTo(5));
                Assert.That(notifications.Keys, Is.All.EqualTo("gravity-well-anchored"));

                notifications.Keys.Clear();
                orders.Raise(UnitActionId.Attack, TestPlayers.Human);
                orders.Raise(UnitActionId.Retreat, TestPlayers.Human);
                orders.Raise(UnitActionId.Stop, TestPlayers.Human);
                orders.Raise(UnitActionId.Move, TestPlayers.Enemy);
                Assert.That(notifications.Keys, Is.Empty, "Accepted orders and other players' orders stay silent.");
            }
            finally
            {
                notifier.LateDispose();
            }
            Assert.That(orders.HasListeners, Is.False);
        }

        private sealed class Notifications : IBattleNotifications
        {
            public List<string> Keys { get; } = new List<string>();

            public void Post(BattleNotificationPriority priority, string text, string groupKey = null,
                string groupFormat = null, string iconKey = null, Action onActivate = null) => Keys.Add(groupKey);

            public void Hold(BattleNotificationPriority priority, string text, string key, string iconKey = null) =>
                throw new NotSupportedException();
        }

        private sealed class Orders : IUnitOrderService
        {
            public event Action<UnitOrder> OrderIssued;
            public bool HasListeners => OrderIssued != null;

            public void Raise(UnitActionId action, PlayerId issuer) =>
                OrderIssued?.Invoke(new UnitOrder(action, issuer, Vector3.zero));

            public void IssueMove(IReadOnlyList<IEntity> receivers, Vector3 point) => throw new NotSupportedException();
            public void IssueMove(IReadOnlyList<IEntity> receivers, IReadOnlyList<Vector3> destinations) =>
                throw new NotSupportedException();
            public void IssueAttack(IReadOnlyList<IEntity> receivers, IEntity target) => throw new NotSupportedException();
            public void IssueAttack(IReadOnlyList<IEntity> receivers, IEntity target, IReadOnlyList<Vector3> offsets) =>
                throw new NotSupportedException();
            public void IssueHardPointAttack(IReadOnlyList<IEntity> receivers, IEntity target, int hardPointId) =>
                throw new NotSupportedException();
            public void IssueAttackMove(IReadOnlyList<IEntity> receivers, Vector3 point) => throw new NotSupportedException();
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

        private sealed class Selection : ISelectionService, ISelectionContext
        {
            public Selection(params IEntity[] entities) => Entities = entities;

            public string Id => nameof(Selection);
            public ISelectionContext PlayerSelectionContext => this;
            public ISelectionContext OtherSelectionContext => throw new NotSupportedException();
            public IEntity Entity => Entities[0];
            public IReadOnlyList<IEntity> Entities { get; }
            public IEntitySelectionFacade SelectionFacade => throw new NotSupportedException();
            public SelectionType SelectionType => SelectionType.Ship;
            public bool HasSelectable => Entities.Count > 0;
            public int Count => Entities.Count;
            public SelectionScope Scope => SelectionScope.Local;
            public bool Contains(IEntity entity) => throw new NotSupportedException();
            public void RemoveSelectable(ISelectionContext context) => throw new NotSupportedException();
            public void SelectEntity(IEntity entity) => throw new NotSupportedException();
            public void SelectEntities(IReadOnlyList<IEntity> entities) => throw new NotSupportedException();
            public void SelectCurrentUnitsByType(UnitTypeId unitTypeId) => throw new NotSupportedException();
            public void AddObserver(IObserver<ISelectionSubject> observer) => throw new NotSupportedException();
            public void RemoveObserver(IObserver<ISelectionSubject> observer) => throw new NotSupportedException();
        }

        private sealed class Ship : IEntity, ICombatModifiersFacade
        {
            public Ship(PlayerId owner) => Owner = owner;

            public long Id => 1;
            public IHealthModelObserver HealthModel => throw new NotSupportedException();
            public PlayerId Owner { get; }
            public CombatModifiers Modifiers { get; } = new CombatModifiers();

            public bool TryGetFacade<TFacade>(out TFacade entityFacade) where TFacade : IEntityFacade
            {
                entityFacade = this is TFacade facade ? facade : default;
                return entityFacade != null;
            }

            public TFacade GetFacade<TFacade>() where TFacade : IEntityFacade => (TFacade)(IEntityFacade)this;
        }
    }
}
