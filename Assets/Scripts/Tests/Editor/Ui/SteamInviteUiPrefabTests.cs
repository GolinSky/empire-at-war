using System.Collections;
using System.Collections.Generic;
using EmpireAtWar.Entities.MainMenu.Online;
using EmpireAtWar.Entities.Popups;
using EmpireAtWar.Ui.Base;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Ui")]
    public sealed class SteamInviteUiPrefabTests
    {
        private const string POPUP_PATH = "Assets/Prefabs/Ui/Popups/SteamInvitePopupUi.prefab";

        [TestCase("Assets/Prefabs/Ui/MainMenu/SkirmishUi.prefab")]
        [TestCase("Assets/Prefabs/Ui/MainMenu/SurvivalUi.prefab")]
        public void Lobby_HasInviteButtonAndNoEmbeddedPicker(string path)
        {
            BaseUi root = AssetDatabase.LoadAssetAtPath<BaseUi>(path);
            Assert.That(root.GetComponentInChildren<SteamInviteView>(true), Is.Null);
            Assert.That(new SerializedObject(root).FindProperty("inviteButton").objectReferenceValue,
                Is.Not.Null);
        }

        [Test]
        public void FriendPicker_BindsControlsForwardsSelectedFriendAndDisposesListeners()
        {
            SteamInvitePopupUi root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<SteamInvitePopupUi>(POPUP_PATH));
            try
            {
                SteamInviteView view = root.InviteView;
                Assert.That(view, Is.Not.Null);
                var fields = new SerializedObject(view);
                foreach (string name in new[] { "friends", "status", "inviteButton", "refreshButton", "closeButton" })
                    Assert.That(fields.FindProperty(name).objectReferenceValue, Is.Not.Null, name);
                var presenter = new InvitePresenter();
                view.Initialize(presenter);
                view.Render(new List<string> { "First friend", "<b>Second friend</b>" }, "Choose a friend.");
                var friends = (TMP_Dropdown)fields.FindProperty("friends").objectReferenceValue;
                Assert.That(friends.captionText.richText, Is.False);
                Assert.That(friends.itemText.richText, Is.False);
                friends.SetValueWithoutNotify(1);
                var invite = (Button)fields.FindProperty("inviteButton").objectReferenceValue;
                invite.onClick.Invoke();
                Assert.That(presenter.InvitedIndex, Is.EqualTo(1));
                Assert.That(presenter.InviteCount, Is.EqualTo(1));
                ((Button)fields.FindProperty("refreshButton").objectReferenceValue).onClick.Invoke();
                ((Button)fields.FindProperty("closeButton").objectReferenceValue).onClick.Invoke();
                Assert.That(presenter.RefreshCount, Is.EqualTo(1));
                Assert.That(presenter.CloseCount, Is.EqualTo(1));
                view.ShowStatus("Invite sent to Second friend");
                Assert.That(((TMP_Text)fields.FindProperty("status").objectReferenceValue).text,
                    Is.EqualTo("Invite sent to Second friend"));
                view.Dispose();
                invite.onClick.Invoke();
                Assert.That(presenter.InviteCount, Is.EqualTo(1), "Disposal must remove invite listeners.");
            }
            finally { Object.DestroyImmediate(root.gameObject); }
        }

        [Test]
        public void FriendPicker_EmptyFriendsDisablesInviteAndHideHidesPopup()
        {
            SteamInvitePopupUi root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<SteamInvitePopupUi>(POPUP_PATH));
            try
            {
                SteamInviteView view = root.InviteView;
                view.Initialize(new InvitePresenter());
                view.Render(new List<string>(), "No friends are online.");
                var fields = new SerializedObject(view);
                Assert.That(((TMP_Dropdown)fields.FindProperty("friends").objectReferenceValue).interactable, Is.False);
                Assert.That(((Button)fields.FindProperty("inviteButton").objectReferenceValue).interactable, Is.False);
                root.Hide();
                Assert.That(root.IsVisible, Is.False);
            }
            finally { Object.DestroyImmediate(root.gameObject); }
        }

        [Test]
        public void FriendPicker_DisposeReleasesGeneratedAvatarSprites()
        {
            SteamInvitePopupUi root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<SteamInvitePopupUi>(POPUP_PATH));
            var avatar = new Texture2D(2, 2);
            try
            {
                SteamInviteView view = root.InviteView;
                view.Initialize(new InvitePresenter());
                view.Render(new List<string> { "Friend" }, "Choose a friend.");
                var friends = (TMP_Dropdown)new SerializedObject(view).FindProperty("friends").objectReferenceValue;
                Assert.That(view.SetAvatar(0, avatar), Is.True);
                Assert.That(view.SetAvatar(0, avatar), Is.False);
                Sprite sprite = friends.options[0].image;

                view.Dispose();

                Assert.That(sprite == null, Is.True);
                Assert.That(friends.options[0].image, Is.Null);
                Assert.That(avatar != null, Is.True, "The caller owns the source texture.");
            }
            finally
            {
                Object.DestroyImmediate(root.gameObject);
                Object.DestroyImmediate(avatar);
            }
        }

        [UnityTest]
        public IEnumerator FriendPicker_CoveringCollapsesDropdownImmediatelyAndPreservesSelection()
        {
            yield return new EnterPlayMode();
            var canvas = new GameObject("Invite Popup Test Canvas", typeof(Canvas));
            try
            {
                var root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<SteamInvitePopupUi>(POPUP_PATH),
                    canvas.transform);
                SteamInviteView view = root.InviteView;
                view.Initialize(new InvitePresenter());
                view.Render(new List<string> { "First friend", "Second friend" }, "Choose a friend.");
                var friends = (TMP_Dropdown)new SerializedObject(view).FindProperty("friends").objectReferenceValue;
                friends.SetValueWithoutNotify(1);
                yield return null; // TMP_Dropdown.Start initializes the list's fade runner.
                friends.Show();
                Assert.That(friends.IsExpanded, Is.True);
                view.RefreshAvatars();
                Assert.That(friends.IsExpanded, Is.True);

                view.SetCovered(true);
                Assert.That(friends.IsExpanded, Is.False);
                view.RefreshAvatars();
                friends.Show();
                Assert.That(friends.IsExpanded, Is.False);

                view.SetCovered(false);
                Assert.That(friends.value, Is.EqualTo(1));
                Assert.That(friends.IsExpanded, Is.False);
                friends.Show();
                Assert.That(friends.IsExpanded, Is.True);
            }
            finally { Object.Destroy(canvas); }
        }

        [UnityTearDown]
        public IEnumerator ExitPlayModeAfterDropdownTest()
        {
            if (Application.isPlaying) yield return new ExitPlayMode();
        }

        private sealed class InvitePresenter : ISteamInvitePresenter
        {
            public int InvitedIndex { get; private set; } = -1;
            public int InviteCount { get; private set; }
            public int RefreshCount { get; private set; }
            public int CloseCount { get; private set; }
            public void Refresh() => RefreshCount++;
            public void Close() => CloseCount++;
            public void Invite(int index)
            {
                InvitedIndex = index;
                InviteCount++;
            }
        }
    }
}
