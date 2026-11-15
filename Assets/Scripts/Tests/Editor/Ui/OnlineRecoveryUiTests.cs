using EmpireAtWar.Entities.MainMenu.Online;
using EmpireAtWar.Services.Networking.Online;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Ui")]
    public sealed class OnlineRecoveryUiTests
    {
        private const string PREFAB_PATH = "Assets/Prefabs/Ui/MainMenu/OnlineUi.prefab";

        [Test]
        public void Recovery_OffersStartupAndConnectionLossActionsAndRestoresHostingAfterDismissal()
        {
            OnlineUi view = Object.Instantiate(AssetDatabase.LoadAssetAtPath<OnlineUi>(PREFAB_PATH));
            try
            {
                var fields = new SerializedObject(view);
                var presenter = new Presenter();
                view.AssignPresenter(presenter);
                view.Initialize();
                view.Initialize();
                var reconnect = (Button)fields.FindProperty("reconnectButton").objectReferenceValue;
                var dismiss = (Button)fields.FindProperty("dismissRecoveryButton").objectReferenceValue;
                var reconnectLabel = (TMP_Text)fields.FindProperty("reconnectLabel").objectReferenceValue;
                var dismissLabel = (TMP_Text)fields.FindProperty("dismissRecoveryLabel").objectReferenceValue;
                var host = (Button)fields.FindProperty("hostSteamButton").objectReferenceValue;

                view.Render("Checking session", false, string.Empty, true, true, true);
                Assert.That(reconnect.gameObject.activeSelf, Is.True);
                Assert.That(dismiss.gameObject.activeSelf, Is.True);
                Assert.That(host.gameObject.activeSelf, Is.False);
                Assert.That(reconnectLabel.text, Is.EqualTo("REJOIN PREVIOUS MATCH"));
                Assert.That(dismissLabel.text, Is.EqualTo("DISMISS"));
                reconnect.onClick.Invoke();
                dismiss.onClick.Invoke();
                Assert.That(presenter.ReconnectCount, Is.EqualTo(1));
                Assert.That(presenter.DismissCount, Is.EqualTo(1));

                view.Render("Connection lost", false, string.Empty, true, true);
                Assert.That(reconnectLabel.text, Is.EqualTo("RECONNECT"));
                Assert.That(dismissLabel.text, Is.EqualTo("LEAVE"));
                view.Render("Ready", false, string.Empty, true);
                Assert.That(reconnect.gameObject.activeSelf, Is.False);
                Assert.That(dismiss.gameObject.activeSelf, Is.False);
                Assert.That(host.gameObject.activeSelf, Is.True);

                view.Dispose();
                reconnect.onClick.Invoke();
                dismiss.onClick.Invoke();
                Assert.That(presenter.ReconnectCount, Is.EqualTo(1));
                Assert.That(presenter.DismissCount, Is.EqualTo(1));
            }
            finally { Object.DestroyImmediate(view.gameObject); }
        }

        [Test]
        public void RecoveryAttempt_DisablesDuplicateClicksAndOtherJoinsButAllowsDismissal()
        {
            OnlineUi view = Object.Instantiate(AssetDatabase.LoadAssetAtPath<OnlineUi>(PREFAB_PATH));
            try
            {
                var fields = new SerializedObject(view);
                var presenter = new Presenter();
                view.AssignPresenter(presenter);
                view.Initialize();
                view.Render("Connecting", true, string.Empty, true, true);
                var reconnect = (Button)fields.FindProperty("reconnectButton").objectReferenceValue;
                reconnect.OnPointerClick(new PointerEventData(null) { button = PointerEventData.InputButton.Left });
                reconnect.OnPointerClick(new PointerEventData(null) { button = PointerEventData.InputButton.Left });
                Assert.That(presenter.ReconnectCount, Is.Zero);
                foreach (string name in new[] { "reconnectButton", "joinGameButton", "acceptInviteButton" })
                    Assert.That(((Button)fields.FindProperty(name).objectReferenceValue).interactable, Is.False, name);
                Assert.That(((Button)fields.FindProperty("dismissRecoveryButton").objectReferenceValue).interactable,
                    Is.True);
                view.Render("Try reconnecting again", false, string.Empty, true, true);
                Assert.That(reconnect.interactable, Is.True);
            }
            finally { Object.DestroyImmediate(view.gameObject); }
        }

        [TestCase(OnlineError.ConnectionLost, "Connection lost")]
        [TestCase(OnlineError.ReconnectExpired, "expired")]
        [TestCase(OnlineError.ReconnectRejected, "cannot rejoin")]
        [TestCase(OnlineError.MatchEnded, "ended")]
        [TestCase(OnlineError.RecoveryRecordInvalid, "discarded")]
        public void RecoveryErrors_HaveActionablePlayerFacingText(OnlineError error, string text)
        {
            Assert.That(OnlineErrorText.Describe(error), Does.Contain(text));
        }

        private sealed class Presenter : IOnlinePresenter
        {
            public int ReconnectCount { get; private set; }
            public int DismissCount { get; private set; }
            public void Reconnect() => ReconnectCount++;
            public void DismissRecovery() => DismissCount++;
            public void HostSteamLobby() { }
            public void HostLanGame() { }
            public void OpenServerBrowser() { }
            public void AcceptInvite() { }
            public void RetrySteam() { }
            public void Close() { }
        }
    }
}
