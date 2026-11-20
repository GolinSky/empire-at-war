using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EmpireAtWar.Entities.MainMenu.Online;
using EmpireAtWar.Entities.MainMenu.Skirmish;
using EmpireAtWar.Entities.MainMenu.Survival;
using EmpireAtWar.Entities.Popups;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Input;
using EmpireAtWar.Services.Networking.Online;
using EmpireAtWar.Services.Popups;
using EmpireAtWar.Services.Tooltip;
using EmpireAtWar.Ui.Base;
using NUnit.Framework;
using Steamworks;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Ui")]
    public sealed class PopupServiceTests
    {
        private const string LOBBY_PREFABS = "Assets/Prefabs/Ui/MainMenu/";
        private const string POPUP_PREFABS = "Assets/Prefabs/Ui/Popups/";

        private GameObject _root;
        private CancelInputStub _input;
        private UiCancelRouter _router;
        private PrefabUiService _ui;
        private PopupService _popups;
        private ConfirmPopupService _confirm;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("Popup Test Canvas", typeof(Canvas));
            _input = new CancelInputStub();
            _router = new UiCancelRouter(_input);
            _router.Initialize();
            _ui = new PrefabUiService(_root.transform);
            _popups = new PopupService(_ui, _router);
            _popups.Initialize();
            _confirm = new ConfirmPopupService(_ui, _popups);
        }

        [TearDown]
        public void TearDown()
        {
            try { _popups.LateDispose(); }
            finally
            {
                _router.Dispose();
                UnityEngine.Object.DestroyImmediate(_root);
            }
        }

        [Test]
        public void ConfirmPopups_OpenTogetherAndEscapeReachesTopBeforeNewlyFocusedScreen()
        {
            var firstResults = new List<ConfirmPopupResult>();
            var secondResults = new List<ConfirmPopupResult>();
            _confirm.ShowAccept(new AcceptPopupContent("First", "Message"), firstResults.Add);
            AcceptPopupUi first = _root.GetComponentInChildren<AcceptPopupUi>();
            _confirm.ShowAccept(new AcceptPopupContent("Second", "Message"), secondResults.Add);
            AcceptPopupUi second = _root.transform.GetChild(_root.transform.childCount - 1)
                .GetComponent<AcceptPopupUi>();
            PopupBlockerUi blocker = _root.GetComponentInChildren<PopupBlockerUi>(true);
            var screen = new ScreenCancelHandler();
            _router.Focus(screen);

            Assert.That(first.IsVisible, Is.True);
            Assert.That(second.IsVisible, Is.True);
            Assert.That(first.GetComponent<CanvasGroup>().interactable, Is.False);
            Assert.That(second.GetComponent<CanvasGroup>().interactable, Is.True);
            Assert.That(blocker.IsVisible, Is.True);
            Assert.That(blocker.transform.GetSiblingIndex(), Is.EqualTo(second.transform.GetSiblingIndex() - 1));
            Assert.That(_root.GetComponentsInChildren<PopupBlockerUi>(true).Length, Is.EqualTo(1));
            Assert.That(blocker.GetComponentsInChildren<Button>(true), Is.Empty);

            _input.Escape();

            Assert.That(secondResults, Is.EqualTo(new[] { ConfirmPopupResult.Declined }));
            Assert.That(firstResults, Is.Empty);
            Assert.That(first.GetComponent<CanvasGroup>().interactable, Is.True);
            Assert.That(blocker.transform.GetSiblingIndex(), Is.EqualTo(first.transform.GetSiblingIndex() - 1));
            Assert.That(screen.CancelCount, Is.Zero);

            Click(first, "acceptButton");
            _input.Escape();

            Assert.That(firstResults, Is.EqualTo(new[] { ConfirmPopupResult.Accepted }));
            Assert.That(secondResults, Is.EqualTo(new[] { ConfirmPopupResult.Declined }));
            Assert.That(blocker.IsVisible, Is.False);
            Assert.That(screen.CancelCount, Is.EqualTo(1));
        }

        [Test]
        public void ScopeTeardown_DismissesOpenRequestsTopToBottomExactlyOnce()
        {
            var completions = new List<(string Name, ConfirmPopupResult Result)>();
            _confirm.ShowAccept(new AcceptPopupContent("First", "Message"),
                result => completions.Add(("First", result)));
            _confirm.ShowAccept(new AcceptPopupContent("Second", "Message"),
                result => completions.Add(("Second", result)));

            _popups.LateDispose();
            _popups.LateDispose();

            Assert.That(completions, Is.EqualTo(new[]
            {
                ("Second", ConfirmPopupResult.Dismissed),
                ("First", ConfirmPopupResult.Dismissed)
            }));
        }

        [Test]
        public void ClosingCoveredPopup_IsIdempotentAndKeepsTopInteractive()
        {
            var lowerResults = new List<ConfirmPopupResult>();
            var upperResults = new List<ConfirmPopupResult>();
            PopupHandle lower = _confirm.ShowAccept(new AcceptPopupContent("First", "Message"), lowerResults.Add);
            _confirm.ShowAccept(new AcceptPopupContent("Second", "Message"), upperResults.Add);
            AcceptPopupUi upper = _root.transform.GetChild(_root.transform.childCount - 1)
                .GetComponent<AcceptPopupUi>();

            _confirm.Close(lower);
            _confirm.Close(lower);

            Assert.That(_popups.IsOpen(lower), Is.False);
            Assert.That(lowerResults, Is.EqualTo(new[] { ConfirmPopupResult.Dismissed }));
            Assert.That(upperResults, Is.Empty);
            Assert.That(upper.GetComponent<CanvasGroup>().interactable, Is.True);
            Assert.That(upper.transform.GetSiblingIndex(), Is.EqualTo(_root.transform.childCount - 1));
            _input.Escape();
            Assert.That(upperResults, Is.EqualTo(new[] { ConfirmPopupResult.Declined }));
        }

        [Test]
        public void SteamInviteOverPicker_EscapeRestoresSelectionAndDuplicateOpenReusesPicker()
        {
            var steam = new SteamPopupService(_ui, _popups, _confirm, new OnlineMatchService(null, new OnlineRecoveryProgress(), new SteamPlatform(apiEnabled: false)), new TooltipServiceStub());
            PopupHandle picker = steam.OpenInvitePicker();
            Assert.That(steam.OpenInvitePicker(), Is.SameAs(picker));
            Assert.That(_root.GetComponentsInChildren<SteamInvitePopupUi>(true).Length, Is.EqualTo(1));
            SteamInvitePopupUi popup = _root.GetComponentInChildren<SteamInvitePopupUi>();
            popup.InviteView.Render(new List<string> { "First friend", "Second friend" }, "Choose a friend.");
            var dropdown = (TMPro.TMP_Dropdown)new SerializedObject(popup.InviteView)
                .FindProperty("friends").objectReferenceValue;
            dropdown.SetValueWithoutNotify(1);
            var results = new List<ConfirmPopupResult>();

            steam.ShowInviteReceived("Friend", results.Add);
            Assert.That(popup.IsVisible, Is.True);
            Assert.That(dropdown.enabled, Is.False);
            Assert.That(_root.GetComponentInChildren<AcceptPopupUi>().transform.GetSiblingIndex(),
                Is.GreaterThan(popup.transform.GetSiblingIndex()));
            _input.Escape();

            Assert.That(results, Is.EqualTo(new[] { ConfirmPopupResult.Declined }));
            Assert.That(_popups.IsOpen(picker), Is.True);
            Assert.That(dropdown.enabled, Is.True);
            Assert.That(dropdown.value, Is.EqualTo(1));
            steam.CloseInvitePicker(picker);
            Assert.That(_popups.IsOpen(picker), Is.False);
            Assert.That(steam.OpenInvitePicker(), Is.Not.SameAs(picker));
        }

        [Test]
        public void RepeatedPickerOpenClose_BalancesAvatarSubscriptions()
        {
            var online = new OnlineMatchService(null, new OnlineRecoveryProgress(), new SteamPlatform(apiEnabled: false));
            var steam = new SteamPopupService(_ui, _popups, _confirm, online, new TooltipServiceStub());
            FieldInfo avatarLoaded = typeof(SteamPlatform).GetField("AvatarLoaded",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(avatarLoaded, Is.Not.Null);
            for (int index = 0; index < 3; index++)
            {
                PopupHandle picker = steam.OpenInvitePicker();
                var subscribers = (Action)avatarLoaded.GetValue(online.Steam);
                Assert.That(subscribers.GetInvocationList().Length, Is.EqualTo(1));

                steam.CloseInvitePicker(picker);

                Assert.That(avatarLoaded.GetValue(online.Steam), Is.Null);
                Assert.That(_root.GetComponentsInChildren<SteamInvitePopupUi>(true), Is.Empty);
            }
        }

        [Test]
        public void OfflineSkirmish_SavedUltraHardOpensAsMediumAndCanBeSelectedAgain()
        {
            var settings = new FakeSettingsService();
            settings.SetLobbyOccupant(1, (int)SkirmishSlotOccupant.AiUltraHard);
            settings.SetLobbyOccupant(2, (int)SkirmishSlotOccupant.AiHard);
            var model = new SkirmishModel();
            var palette = AssetDatabase.LoadAssetAtPath<TeamColorPalette>(
                "Assets/Settings/Data/Models/Players/TeamColorPalette.asset");
            var route = new SkirmishRouteUiController(_ui, _router, null, new TooltipServiceStub(),
                model, palette, null, _popups, settings);
            try
            {
                route.Open();
                var view = _root.GetComponentInChildren<SkirmishUi>();
                using var serializedView = new SerializedObject(view);
                var enemyRow = serializedView.FindProperty("slotRows").GetArrayElementAtIndex(1).objectReferenceValue;
                using var serializedRow = new SerializedObject(enemyRow);
                var dropdown = (TMP_Dropdown)serializedRow.FindProperty("occupantDropdown").objectReferenceValue;

                Assert.That(dropdown.value, Is.EqualTo((int)SkirmishSlotOccupant.AiMedium));
                Assert.That(model.Slots[1].Occupant, Is.EqualTo(SkirmishSlotOccupant.AiMedium));
                Assert.That(model.Slots[2].Occupant, Is.EqualTo(SkirmishSlotOccupant.AiHard));
                Assert.That(model.Slots[3].Occupant, Is.EqualTo(SkirmishSlotOccupant.Closed));

                dropdown.value = (int)SkirmishSlotOccupant.AiUltraHard;
                Assert.That(model.Slots[1].Occupant, Is.EqualTo(SkirmishSlotOccupant.AiUltraHard));
            }
            finally { route.LateDispose(); }
        }

        [TestCase(LobbyMode.Skirmish, true)]
        [TestCase(LobbyMode.Skirmish, false)]
        [TestCase(LobbyMode.Survival, true)]
        [TestCase(LobbyMode.Survival, false)]
        public void JoinedLobby_ShowsInviteButtonOnlyForSteamMembers(LobbyMode mode, bool inSteamLobby)
        {
            var platform = new SteamPlatform(apiEnabled: false);
            var online = new OnlineMatchService(null, new OnlineRecoveryProgress(), platform);
            typeof(OnlineMatchService).GetProperty(nameof(OnlineMatchService.Role)).SetValue(online, OnlineRole.Client);
            typeof(OnlineMatchService).GetProperty(nameof(OnlineMatchService.Mode)).SetValue(online, mode);
            typeof(OnlineMatchService).GetField("_inLobby", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(online, true);
            if (inSteamLobby)
                typeof(SteamPlatform).GetField("_lobby", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(platform, new CSteamID(10));

            var steam = new SteamPopupService(_ui, _popups, _confirm, online, new TooltipServiceStub());
            IOnlineLobbyScreen lobby = CreateLobby(mode, online, steam);
            try
            {
                lobby.Show();
                var inviteButton = (Button)new SerializedObject(GetLobbyView(mode))
                    .FindProperty("inviteButton").objectReferenceValue;
                Assert.That(inviteButton.gameObject.activeInHierarchy, Is.EqualTo(inSteamLobby));
            }
            finally { ((ILateDisposable)lobby).LateDispose(); }
        }

        [TestCase(LobbyMode.Skirmish)]
        [TestCase(LobbyMode.Survival)]
        public void LobbyLeave_ClosesCoveredPickerAndKeepsTopInvite(LobbyMode mode)
        {
            var online = new OnlineMatchService(null, new OnlineRecoveryProgress(), new SteamPlatform(apiEnabled: false));
            var steam = new SteamPopupService(_ui, _popups, _confirm, online, new TooltipServiceStub());
            IOnlineLobbyScreen lobby = CreateLobby(mode, online, steam);
            try
            {
                lobby.Show();
                BaseUi lobbyView = GetLobbyView(mode);
                Click(lobbyView, "inviteButton");
                SteamInvitePopupUi pickerView = _root.GetComponentInChildren<SteamInvitePopupUi>();
                Assert.That(pickerView, Is.Not.Null);
                PopupHandle picker = steam.OpenInvitePicker();
                var results = new List<ConfirmPopupResult>();
                steam.ShowInviteReceived("Friend", results.Add);
                AcceptPopupUi top = _root.GetComponentInChildren<AcceptPopupUi>();

                Click(lobbyView, "closeButton");
                AcceptPopupUi leave = _root.GetComponentsInChildren<AcceptPopupUi>().Single(view => view != top);
                Assert.That(_popups.IsOpen(picker), Is.True);
                Click(leave, "acceptButton");

                Assert.That(_popups.IsOpen(picker), Is.False);
                Assert.That(_root.GetComponentsInChildren<SteamInvitePopupUi>(true), Is.Empty);
                Assert.That(top.IsVisible, Is.True);
                Assert.That(top.GetComponent<CanvasGroup>().interactable, Is.True);
                Assert.That(results, Is.Empty);
                _input.Escape();
                Assert.That(results, Is.EqualTo(new[] { ConfirmPopupResult.Declined }));
            }
            finally { ((ILateDisposable)lobby).LateDispose(); }
        }

        private IOnlineLobbyScreen CreateLobby(LobbyMode mode, OnlineMatchService online, SteamPopupService steam)
        {
            IOnlineLobbyScreen lobby = mode == LobbyMode.Skirmish
                ? new OnlineSkirmishLobbyUiController(_ui, _router, online,
                    AssetDatabase.LoadAssetAtPath<TeamColorPalette>(
                        "Assets/Settings/Data/Models/Players/TeamColorPalette.asset"), null, new TooltipServiceStub(),
                    steam, _popups, _confirm, new FakeSettingsService())
                : new OnlineSurvivalLobbyUiController(_ui, _router, online, steam, _confirm, new TooltipServiceStub());
            ((IInitializable)lobby).Initialize();
            return lobby;
        }

        private BaseUi GetLobbyView(LobbyMode mode) => mode == LobbyMode.Skirmish
            ? _root.GetComponentInChildren<SkirmishUi>()
            : _root.GetComponentInChildren<SurvivalUi>();

        private static void Click(BaseUi view, string property)
        {
            var button = (Button)new SerializedObject(view).FindProperty(property).objectReferenceValue;
            button.onClick.Invoke();
        }

        private sealed class CancelInputStub : ICancelInput
        {
            public event Action CancelPressed;
            public void Escape() => CancelPressed?.Invoke();
        }

        private sealed class ScreenCancelHandler : IUiCancelHandler
        {
            public int CancelCount { get; private set; }

            public bool TryCancel()
            {
                CancelCount++;
                return true;
            }
        }

        private sealed class PrefabUiService : IUiService
        {
            private readonly Transform _parent;

            public PrefabUiService(Transform parent) => _parent = parent;

            public TUi CreateUi<TUi>() where TUi : BaseUi
            {
                string folder = typeof(TUi) == typeof(SkirmishUi) || typeof(TUi) == typeof(SurvivalUi)
                    ? LOBBY_PREFABS : POPUP_PREFABS;
                var prefab = AssetDatabase.LoadAssetAtPath<TUi>(folder + typeof(TUi).Name + ".prefab");
                Assert.That(prefab, Is.Not.Null, typeof(TUi).Name);
                return UnityEngine.Object.Instantiate(prefab, _parent);
            }

            public void SetHudVisible(bool isVisible) { }
            public void SetContentLayout(bool isFaction, bool isGroup) { }
            public void SetContentVisible(bool isVisible) { }
        }

        private sealed class TooltipServiceStub : ITooltipService
        {
            public TooltipHandle Show(ITooltipContentProvider provider, TooltipAnchor anchor) =>
                throw new InvalidOperationException("No tooltip is expected in this test.");
            public void Hide(TooltipHandle handle) { }
            public void HideAll() { }
        }
    }
}
