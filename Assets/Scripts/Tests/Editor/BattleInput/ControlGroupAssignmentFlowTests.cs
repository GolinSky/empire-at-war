using System;
using System.Collections.Generic;
using EmpireAtWar.Controllers.Game;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Battle;
using EmpireAtWar.Services.BattleNotifications;
using EmpireAtWar.Services.ControlGroups;
using EmpireAtWar.Services.Input;
using EmpireAtWar.Services.Popups;
using EmpireAtWar.Services.Selection;
using EmpireAtWar.Tests.Selection;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Editor
{
    [Category("BattleInput")]
    public sealed class ControlGroupAssignmentFlowTests
    {
        private EntityLocator _entities;
        private SelectionService _selection;
        private ControlGroupService _groups;
        private ControlGroupAssignmentFlow _flow;
        private SkirmishStateModel _battle;
        private InputActionsProvider _actions;
        private InputLockService _inputLock;
        private GroupInput _input;
        private Confirmation _confirmation;
        private Notifications _notifications;
        private SelectionInputTests.FakeEntity _first;
        private SelectionInputTests.FakeEntity _second;

        [SetUp]
        public void SetUp()
        {
            var local = TestPlayers.CreateLocalPlayer(TestPlayers.CreateDuel());
            var pointer = new SelectionInputTests.FakeInputService();
            _entities = new EntityLocator();
            _selection = new SelectionService(pointer, pointer, pointer, _entities,
                new SelectionInputTests.FakeSelectionQuery(), new SelectionInputTests.FakeMarqueeSelectionPresenter(), local);
            _first = CreateShip(1, local.Id);
            _second = CreateShip(2, local.Id);
            _entities.AddEntity(_first);
            _entities.AddEntity(_second);
            _groups = new ControlGroupService(new ControlGroupModel(), _entities, _selection, local);
            _groups.Initialize();
            _groups.Assign(3, new long[] { 1 });
            _selection.SelectEntity(_second);
            _battle = new SkirmishStateModel();
            _battle.SetState(BattleState.Running);
            _actions = new InputActionsProvider();
            _inputLock = new InputLockService(_actions);
            _inputLock.Initialize();
            _input = new GroupInput();
            _confirmation = new Confirmation();
            _notifications = new Notifications();
            _flow = new ControlGroupAssignmentFlow(_groups, _input, _inputLock, _confirmation, _notifications, _battle);
            _flow.Initialize();
        }

        [TearDown]
        public void TearDown()
        {
            _flow.LateDispose();
            _groups.LateDispose();
            _selection.LateDispose();
            _inputLock.Dispose();
            _actions.Dispose();
        }

        [Test]
        public void AcceptUsesCapturedSelectionOnceAndHoldsOneGameplayLock()
        {
            _input.Request(3, true);
            Assert.That(_inputLock.IsLocked, Is.True);
            Assert.That(_actions.Actions.Battle.enabled, Is.False);
            Assert.That(_confirmation.Content.Title, Is.EqualTo("Replace Squad 3?"));
            _input.Request(4, true);
            Assert.That(_confirmation.OpenCount, Is.EqualTo(1));
            _selection.SelectEntity(_first);
            Action<ConfirmPopupResult> answer = _confirmation.Answer;
            answer(ConfirmPopupResult.Accepted);
            answer(ConfirmPopupResult.Accepted);
            AssertMembers(3, _second);
            Assert.That(_inputLock.IsLocked, Is.False);
            Assert.That(_groups.HasMembers(4), Is.False);
        }

        [TestCase(ConfirmPopupResult.Declined)]
        [TestCase(ConfirmPopupResult.Dismissed)]
        public void CancelLeavesExistingSquadAndReleasesLock(ConfirmPopupResult result)
        {
            _input.Request(3, true);
            _confirmation.Answer(result);
            AssertMembers(3, _first);
            Assert.That(_inputLock.IsLocked, Is.False);
        }

        [Test]
        public void AllCapturedCandidatesDie_PreservesOldGroupAndNotifies()
        {
            _input.Request(3, true);
            _second.Health.Destroy();
            _confirmation.Answer(ConfirmPopupResult.Accepted);
            AssertMembers(3, _first);
            Assert.That(_notifications.Messages, Has.Count.EqualTo(1));
            Assert.That(_notifications.Messages[0], Does.Contain("unchanged"));
            Assert.That(_inputLock.IsLocked, Is.False);
        }

        [Test]
        public void BattleExitClosesPopupAndLateAcceptanceCannotChangeGroups()
        {
            _input.Request(3, true);
            Action<ConfirmPopupResult> stale = _confirmation.Answer;
            _battle.SetState(BattleState.Ended);
            Assert.That(_confirmation.CloseCount, Is.EqualTo(1));
            Assert.That(_inputLock.IsLocked, Is.False);
            _battle.SetState(BattleState.Running);
            stale(ConfirmPopupResult.Accepted);
            AssertMembers(3, _first);
        }

        [Test]
        public void CandidateDespawnAndRespawnUnderSameId_DoesNotAssignReplacementLife()
        {
            var survivor = CreateShip(3, _first.Owner);
            _entities.AddEntity(survivor);
            _selection.SelectEntities(new[] { _second, survivor });
            _input.Request(3, true);
            _entities.RemoveEntity(_second);
            _entities.AddEntity(_second);
            _confirmation.Answer(ConfirmPopupResult.Accepted);
            AssertMembers(3, survivor);
            Assert.That(_inputLock.IsLocked, Is.False);
        }

        [Test]
        public void EmptyRecallNotifiesAndPreservesSelection_IdenticalAssignSkipsPopup()
        {
            _input.Request(0, false);
            Assert.That(_notifications.Messages[0], Is.EqualTo("Squad 0 is empty."));
            Assert.That(_selection.PlayerSelectionContext.Entity, Is.SameAs(_second));
            _selection.SelectEntity(_first);
            _input.Request(3, true);
            Assert.That(_confirmation.OpenCount, Is.Zero);
            Assert.That(_inputLock.IsLocked, Is.False);
        }

        [Test]
        public void DifferentErrorsForSameSlot_DoNotReuseNotificationText()
        {
            _input.Request(0, false);
            _input.Request(0, false);
            _selection.SelectEntity(_first);
            _first.Health.Destroy();
            _input.Request(0, true);
            Assert.That(_notifications.Keys[1], Is.EqualTo(_notifications.Keys[0]));
            Assert.That(_notifications.Keys[2], Is.Not.EqualTo(_notifications.Keys[0]));
            Assert.That(_notifications.Messages[2], Is.EqualTo("Select your units first."));
        }

        private void AssertMembers(int slot, params IEntity[] expected)
        {
            var members = new List<IEntity>();
            _groups.GetMembers(slot, members);
            CollectionAssert.AreEqual(expected, members);
        }

        private static SelectionInputTests.FakeEntity CreateShip(long id, PlayerId owner) =>
            new SelectionInputTests.FakeEntity(new SelectionInputTests.FakeSelectionCommand(SelectionType.Ship), owner,
                id, new UnitTypeFacade(UnitTypeId.Ship(ShipType.Venator)));

        private sealed class GroupInput : IControlGroupInput
        {
            public event Action<int, bool> SlotRequested;
            public bool IsPreviewHeld => true;
            public void Request(int slot, bool assign) => SlotRequested(slot, assign);
        }

        private sealed class Confirmation : IConfirmPopupService
        {
            public Action<ConfirmPopupResult> Answer { get; private set; }
            public AcceptPopupContent Content { get; private set; }
            public int OpenCount { get; private set; }
            public int CloseCount { get; private set; }
            public PopupHandle ShowAccept(AcceptPopupContent content, Action<ConfirmPopupResult> answered)
            {
                Content = content;
                Answer = answered;
                OpenCount++;
                return (PopupHandle)Activator.CreateInstance(typeof(PopupHandle), true);
            }
            public void Close(PopupHandle handle)
            {
                CloseCount++;
                Answer(ConfirmPopupResult.Dismissed);
            }
        }

        private sealed class Notifications : IBattleNotifications
        {
            public List<string> Messages { get; } = new List<string>();
            public List<string> Keys { get; } = new List<string>();
            public void Post(BattleNotificationPriority priority, string text, string groupKey = null,
                string groupFormat = null, string iconKey = null, Action onActivate = null)
            {
                Messages.Add(text);
                Keys.Add(groupKey);
            }
            public void Hold(BattleNotificationPriority priority, string text, string key, string iconKey = null) =>
                throw new NotSupportedException();
        }
    }
}
