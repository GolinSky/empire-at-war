using System;
using System.Collections.Generic;
using EmpireAtWar.Components.Hangar;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Controllers.Game;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.Popups;
using EmpireAtWar.Entities.ShipUi;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Input;
using EmpireAtWar.Services.PlayerCommands;
using EmpireAtWar.Services.Popups;
using EmpireAtWar.Ui.Base;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Ui")]
    public sealed class ShipDestroyFlowTests
    {
        private const long FIRST_SHIP_ID = 17;
        private const long SECOND_SHIP_ID = 18;
        private static readonly Vector2 MENU_POSITION = new Vector2(80f, 120f);

        private GameObject _canvas;
        private ShipUi _shipCard;
        private ShipDestroyMenuUi _menu;
        private InputActionsProvider _input;
        private InputLockService _inputLock;
        private PopupService _popups;
        private CancelInputStub _cancel;
        private UiCancelRouter _router;
        private ShipDestroyFlow _flow;
        private EntityLocator _entities;
        private SkirmishStateModel _battle;
        private RecordingCommands _commands;
        private RecordingConfirmation _confirmation;
        private ShipHealth _firstShip;

        [SetUp]
        public void SetUp()
        {
            _canvas = new GameObject("Ship destruction test canvas", typeof(Canvas));
            _canvas.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            // Keep layout independent of the Editor Game view and large enough for the menu.
            ((RectTransform)_canvas.transform).sizeDelta = new Vector2(1920f, 1080f);
            var ui = new FixtureUi(_canvas.transform);
            ShipUi prefab = AssetDatabase.LoadAssetAtPath<ShipUi>("Assets/Prefabs/Ui/Ship/ShipUi.prefab");
            _shipCard = Object.Instantiate(prefab, _canvas.transform);
            _menu = Reference<ShipDestroyMenuUi>(_shipCard, "destroyMenu");
            _input = new InputActionsProvider();
            _inputLock = new InputLockService(_input);
            _inputLock.Initialize();
            _cancel = new CancelInputStub();
            _router = new UiCancelRouter(_cancel);
            _router.Initialize();
            _popups = new PopupService(ui, _router);
            _popups.Initialize();
            _confirmation = new RecordingConfirmation(new ConfirmPopupService(ui, _popups));
            _entities = new EntityLocator();
            _commands = new RecordingCommands();
            _battle = new SkirmishStateModel();
            _battle.SetState(BattleState.Running);
            _firstShip = AddShip(FIRST_SHIP_ID);
            AddShip(SECOND_SHIP_ID);
            var roster = TestPlayers.CreateDuel();
            var session = OfflineSession.ForHuman(roster);
            _flow = new ShipDestroyFlow(_entities, new LocalPlayer(roster, session), _commands,
                _battle, session, _inputLock, _router, _confirmation, ui);
            _flow.Initialize(_menu);
        }

        [TearDown]
        public void TearDown()
        {
            _flow.Dispose();
            _popups.LateDispose();
            _router.Dispose();
            _inputLock.Dispose();
            _input.Dispose();
            Object.DestroyImmediate(_canvas);
        }

        [Test]
        public void Yes_SubmitsTheOpenedTargetExactlyOnce()
        {
            _shipCard.SetDestroyTarget(FIRST_SHIP_ID, "Venator I", _flow.Open);
            ShipCardContextInput input = Reference<ShipCardContextInput>(_shipCard, "contextInput");
            Assert.That(input.gameObject, Is.SameAs(Reference<Button>(_shipCard, "disableSelectionButton").gameObject));
            var pointer = new PointerEventData(null) { position = MENU_POSITION, button = PointerEventData.InputButton.Left };
            input.OnPointerDown(pointer);
            input.OnPointerClick(pointer);
            Assert.That(_menu.gameObject.activeSelf, Is.False);
            pointer.button = PointerEventData.InputButton.Right;
            input.OnPointerDown(pointer);
            input.OnPointerClick(pointer);

            AcceptPopupUi popup = ConfirmMenu();
            var yes = Reference<Button>(popup, "acceptButton").onClick;
            Assert.That(Reference<TMP_Text>(popup, "titleText").text, Is.EqualTo("Destroy ship?"));
            Assert.That(Reference<TMP_Text>(popup, "messageText").text,
                Is.EqualTo("Destroy Venator I? This cannot be undone."));
            Assert.That(Reference<TMP_Text>(popup, "acceptLabel").text, Is.EqualTo("Yes"));
            Assert.That(Reference<TMP_Text>(popup, "declineLabel").text, Is.EqualTo("No"));

            _flow.Open(SECOND_SHIP_ID, "Venator II", MENU_POSITION);
            yes.Invoke();
            yes.Invoke();
            _confirmation.Answer(ConfirmPopupResult.Accepted);

            Assert.That(_commands.Destroyed, Has.Count.EqualTo(1));
            Assert.That(_commands.Destroyed[0].Issuer, Is.EqualTo(TestPlayers.Human));
            Assert.That(_commands.Destroyed[0].TargetId, Is.EqualTo(FIRST_SHIP_ID));
            Assert.That(_firstShip.IsDestroyed, Is.False, "The command sink owns destruction.");
            AssertClosed();
        }

        [TestCase("No")]
        [TestCase("Escape")]
        [TestCase("Popup dismissal")]
        [TestCase("Selection or route change")]
        [TestCase("Target death")]
        [TestCase("Target removal")]
        [TestCase("Battle exit")]
        public void CancelOrInvalidate_ClosesConfirmationAndIgnoresLateYes(string reason)
        {
            AcceptPopupUi popup = OpenConfirmation(FIRST_SHIP_ID, "Venator I");
            Action<ConfirmPopupResult> staleAnswer = _confirmation.Answer;
            int unhandledEscapes = 0;
            _router.CancelUnhandled += () => unhandledEscapes++;

            switch (reason)
            {
                case "No":
                    Reference<Button>(popup, "declineButton").onClick.Invoke();
                    break;
                case "Escape":
                    _cancel.Escape();
                    break;
                case "Popup dismissal":
                    _confirmation.CloseCurrent();
                    break;
                case "Selection or route change":
                    _flow.Invalidate();
                    break;
                case "Target death":
                    _firstShip.Destroy();
                    break;
                case "Target removal":
                    _entities.RemoveEntity(_entities.GetEntity(FIRST_SHIP_ID));
                    break;
                case "Battle exit":
                    _battle.SetState(BattleState.Ended);
                    break;
            }
            AssertClosed();

            _battle.SetState(BattleState.Running);
            AcceptPopupUi newPopup = OpenConfirmation(SECOND_SHIP_ID, "Venator II");
            staleAnswer(ConfirmPopupResult.Accepted);

            Assert.That(_commands.Destroyed, Is.Empty);
            Assert.That(newPopup.IsVisible, Is.True);
            Assert.That(_inputLock.IsLocked, Is.True);
            Assert.That(unhandledEscapes, Is.Zero);
        }

        [Test]
        public void Menu_ClampsToCanvasAndConsumesOutsideClickAndEscape()
        {
            Canvas.ForceUpdateCanvases();
            var area = (RectTransform)_menu.transform;
            var action = (RectTransform)Reference<Button>(_menu, "destroyButton").transform;
            var corners = new Vector3[4];
            var backdrop = _menu.transform.Find("DismissArea").GetComponent<Image>();
            var cornerPoints = new[] { area.rect.min, area.rect.max };
            foreach (Vector2 corner in cornerPoints)
            {
                Vector2 screenCorner = RectTransformUtility.WorldToScreenPoint(null, area.TransformPoint(corner));
                _flow.Open(FIRST_SHIP_ID, "Venator I", screenCorner);
                action.GetWorldCorners(corners);
                foreach (Vector3 worldPoint in corners)
                {
                    Vector3 local = area.InverseTransformPoint(worldPoint);
                    Assert.That(local.x, Is.InRange(area.rect.xMin - 0.01f, area.rect.xMax + 0.01f));
                    Assert.That(local.y, Is.InRange(area.rect.yMin - 0.01f, area.rect.yMax + 0.01f));
                }
                Assert.That(_input.Actions.Battle.enabled, Is.False);
                _flow.Invalidate();
            }

            _flow.Open(FIRST_SHIP_ID, "Venator I", MENU_POSITION);
            var pointer = new PointerEventData(null)
            {
                button = PointerEventData.InputButton.Right,
                position = RectTransformUtility.WorldToScreenPoint(null, area.TransformPoint(area.rect.center))
            };
            Assert.That(backdrop.raycastTarget, Is.True);
            Assert.That(backdrop.Raycast(pointer.position, null), Is.True);
            ExecuteEvents.ExecuteHierarchy(backdrop.gameObject, pointer, ExecuteEvents.pointerClickHandler);
            Assert.That(pointer.used, Is.True);
            AssertClosed();

            int unhandledEscapes = 0;
            _router.CancelUnhandled += () => unhandledEscapes++;
            _flow.Open(FIRST_SHIP_ID, "Venator I", MENU_POSITION);
            _cancel.Escape();
            Assert.That(unhandledEscapes, Is.Zero);
            AssertClosed();
            Assert.That(_commands.Destroyed, Is.Empty);
        }

        private AcceptPopupUi OpenConfirmation(long id, string name)
        {
            _flow.Open(id, name, MENU_POSITION);
            return ConfirmMenu();
        }

        private AcceptPopupUi ConfirmMenu()
        {
            Assert.That(_menu.gameObject.activeInHierarchy, Is.True);
            Reference<Button>(_menu, "destroyButton").onClick.Invoke();
            Assert.That(_menu.gameObject.activeSelf, Is.False);
            Assert.That(_input.Actions.Battle.enabled, Is.False);
            Assert.That(_input.Actions.Camera.enabled, Is.False);
            Assert.That(_input.Actions.Pointer.enabled, Is.True);
            Assert.That(_battle.State, Is.EqualTo(BattleState.Running));

            PopupBlockerUi blocker = _canvas.GetComponentInChildren<PopupBlockerUi>();
            Assert.That(blocker.IsVisible, Is.True);
            Assert.That(blocker.GetComponent<CanvasGroup>().blocksRaycasts, Is.True);
            Graphic blockerGraphic = blocker.GetComponentInChildren<Graphic>();
            Assert.That(blockerGraphic.raycastTarget, Is.True);
            Assert.That(blockerGraphic.Raycast(MENU_POSITION, null), Is.True);
            AcceptPopupUi popup = _canvas.GetComponentInChildren<AcceptPopupUi>();
            Assert.That(popup.IsVisible, Is.True);
            Assert.That(popup.transform.GetSiblingIndex(), Is.GreaterThan(blocker.transform.GetSiblingIndex()));
            return popup;
        }

        private void AssertClosed()
        {
            Assert.That(_menu.gameObject.activeSelf, Is.False);
            Assert.That(_canvas.GetComponentsInChildren<AcceptPopupUi>(true), Is.Empty);
            Assert.That(_input.Actions.Battle.enabled, Is.True);
            Assert.That(_input.Actions.Camera.enabled, Is.True);
        }

        private ShipHealth AddShip(long id)
        {
            var health = new ShipHealth();
            _entities.AddEntity(new Entity(health, _entities,
                new IEntityFacade[] { new UnitTypeFacade(UnitTypeId.Ship(default)) }, health.Owner, id,
                Array.Empty<IPooledEntityEvents>(), Array.Empty<IHangarLaunchEvents>()));
            return health;
        }

        private static T Reference<T>(Object owner, string field) where T : Object
        {
            var reference = (T)new SerializedObject(owner).FindProperty(field).objectReferenceValue;
            Assert.That(reference, Is.Not.Null, field);
            return reference;
        }

        private sealed class FixtureUi : IUiService, IUiPlacement
        {
            public FixtureUi(Transform canvas) => ContextualSlot = canvas;

            public Transform ContextualSlot { get; }
            public UiContentLayout FactionLayout => throw new NotSupportedException();
            public Transform GetParent<T>() where T : BaseUi => ContextualSlot;
            public T CreateUi<T>() where T : BaseUi => Object.Instantiate(
                AssetDatabase.LoadAssetAtPath<T>($"Assets/Prefabs/Ui/Popups/{typeof(T).Name}.prefab"), GetParent<T>());
            public void SetHudVisible(bool isVisible) { }
            public void SetContentLayout(bool isFaction, bool isGroup) { }
            public void SetContentVisible(bool isVisible) { }
        }

        private sealed class RecordingConfirmation : IConfirmPopupService
        {
            private readonly IConfirmPopupService _service;
            private PopupHandle _handle;

            public RecordingConfirmation(IConfirmPopupService service) => _service = service;

            public Action<ConfirmPopupResult> Answer { get; private set; }

            public PopupHandle ShowAccept(AcceptPopupContent content, Action<ConfirmPopupResult> answered)
            {
                Answer = answered;
                _handle = _service.ShowAccept(content, answered);
                return _handle;
            }

            public void Close(PopupHandle handle) => _service.Close(handle);
            public void CloseCurrent() => Close(_handle);
        }

        private sealed class RecordingCommands : IPlayerCommandSink
        {
            public List<DestroyShipCommand> Destroyed { get; } = new List<DestroyShipCommand>();

            public void Submit(DestroyShipCommand command) => Destroyed.Add(command);
            public void Submit(UnitOrderCommand command) => throw new NotSupportedException();
            public void Submit(PurchaseUnitCommand command) => throw new NotSupportedException();
            public void Submit(CancelPurchaseCommand command) => throw new NotSupportedException();
            public void Submit(BuildSiteFacilityCommand command) => throw new NotSupportedException();
            public void Submit(FireSuperWeaponCommand command) => throw new NotSupportedException();
            public void Submit(ShipAbilityCommand command) => throw new NotSupportedException();
            public void Submit(DeployReinforcementCommand command) => throw new NotSupportedException();
            public void Submit(PlaceMarkerCommand command) => throw new NotSupportedException();
        }

        private sealed class CancelInputStub : ICancelInput
        {
            public event Action CancelPressed;
            public void Escape() => CancelPressed?.Invoke();
        }

        private sealed class ShipHealth : IHealthModelObserver
        {
            public event Action OnDestroy;
            public event Action OnValueChanged { add { } remove { } }

            public ShipClass ShipClass => ShipClass.Capital;
            public IReadOnlyList<IReadOnlyHardPointModel> HardPointModels => Array.Empty<HardPointModel>();
            public float Hull => IsDestroyed ? 0f : 100f;
            public float HullPercentage => IsDestroyed ? 0f : 1f;
            public float Shields => 0f;
            public float ShieldPercentage => 0f;
            public bool IsDestroyed { get; private set; }
            public bool IsLostShieldGenerator => false;
            public bool HasUnits => !IsDestroyed;
            public bool HasLiveHardPoints => false;
            public bool HasShields => false;
            public PlayerId Owner => TestPlayers.Human;
            public IHardPointModel[] GetShipUnits(HardPointType type) => Array.Empty<IHardPointModel>();

            public void Destroy()
            {
                IsDestroyed = true;
                OnDestroy?.Invoke();
            }
        }
    }
}
