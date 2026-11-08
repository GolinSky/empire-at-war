using System.Collections.Generic;
using EmpireAtWar.Entities.MainMenu.Online;
using EmpireAtWar.Ui.Base;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Ui")]
    public sealed class SteamInviteUiPrefabTests
    {
        [TestCase("Assets/Prefabs/Ui/MainMenu/SkirmishUi.prefab")]
        [TestCase("Assets/Prefabs/Ui/MainMenu/SurvivalUi.prefab")]
        public void FriendPicker_BindsControlsForwardsSelectedFriendAndDisposesListeners(string path)
        {
            BaseUi root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<BaseUi>(path));
            try
            {
                var owner = new SerializedObject(root);
                var view = (SteamInviteView)owner.FindProperty("steamInvites").objectReferenceValue;
                Assert.That(view, Is.Not.Null);
                var fields = new SerializedObject(view);
                foreach (string name in new[] { "friends", "status", "inviteButton", "refreshButton", "closeButton" })
                    Assert.That(fields.FindProperty(name).objectReferenceValue, Is.Not.Null, name);
                var presenter = new InvitePresenter();
                view.Initialize(presenter);
                Assert.That(view.IsOpen, Is.False);
                view.Show(new List<string> { "First friend", "<b>Second friend</b>" }, "Choose a friend.");
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

        [TestCase("Assets/Prefabs/Ui/MainMenu/SkirmishUi.prefab")]
        [TestCase("Assets/Prefabs/Ui/MainMenu/SurvivalUi.prefab")]
        public void FriendPicker_EmptyFriendsDisablesInviteAndCloseHidesPicker(string path)
        {
            BaseUi root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<BaseUi>(path));
            try
            {
                var view = (SteamInviteView)new SerializedObject(root).FindProperty("steamInvites").objectReferenceValue;
                view.Initialize(new InvitePresenter());
                view.Show(new List<string>(), "No friends are online.");
                var fields = new SerializedObject(view);
                Assert.That(view.IsOpen, Is.True);
                Assert.That(((TMP_Dropdown)fields.FindProperty("friends").objectReferenceValue).interactable, Is.False);
                Assert.That(((Button)fields.FindProperty("inviteButton").objectReferenceValue).interactable, Is.False);
                view.Hide();
                Assert.That(view.IsOpen, Is.False);
            }
            finally { Object.DestroyImmediate(root.gameObject); }
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
