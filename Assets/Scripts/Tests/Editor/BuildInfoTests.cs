using EmpireAtWar.Entities.MainMenu.Main;
using EmpireAtWar.Services.Versioning;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class BuildInfoTests
    {
        private const string PREFAB_PATH = "Assets/Prefabs/Ui/MainMenu/MainMenuUi.prefab";

        [Test]
        public void Json_RoundTripsFieldNamesReadByJenkins()
        {
            BuildInfo info = new BuildInfo("0.2.0-beta.1", "187", "a1b2c3d4e5f60718293a4b5c6d7e8f9012345678", "2026-10-24T08:00:00Z");

            string json = info.ToJson();
            BuildInfo read = BuildInfo.FromJson(json);

            StringAssert.Contains("\"version\": \"0.2.0-beta.1\"", json);
            StringAssert.Contains("\"build\": \"187\"", json);
            StringAssert.Contains("\"commit\": \"a1b2c3d4e5f60718293a4b5c6d7e8f9012345678\"", json);
            Assert.That(read.Version, Is.EqualTo(info.Version));
            Assert.That(read.Build, Is.EqualTo(info.Build));
            Assert.That(read.Commit, Is.EqualTo(info.Commit));
            Assert.That(read.BuiltAtUtc, Is.EqualTo(info.BuiltAtUtc));
        }

        [Test]
        public void DisplayText_ShowsVersionBuildAndShortCommit()
        {
            BuildInfo info = new BuildInfo("0.2.0", "187", "a1b2c3d4e5f60718293a4b5c6d7e8f9012345678", "");

            Assert.That(info.DisplayText, Is.EqualTo("v0.2.0 | Build 187 | a1b2c3d"));
        }

        [Test]
        public void LoadCurrent_InEditor_UsesBundleVersion()
        {
            Assert.That(BuildInfo.LoadCurrent().DisplayText, Is.EqualTo($"v{Application.version} | Build Editor | dev"));
        }

        [Test]
        public void MainMenuPrefab_AssignsVersionText()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
            Object versionText = new SerializedObject(prefab.GetComponent<MainMenuUi>())
                .FindProperty("versionText").objectReferenceValue;

            Assert.That(versionText, Is.InstanceOf<TMP_Text>());
            Assert.That(((TMP_Text)versionText).raycastTarget, Is.False);
        }
    }
}
