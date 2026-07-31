using EmpireAtWar.Views.Menu;
using MPUIKIT;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class PauseMenuUiPrefabTests
    {
        private const string PREFAB_PATH =
            "Assets/Prefabs/Ui/SkirmishMenu/PauseMenuUi.prefab";

        [Test]
        public void PauseMenu_UsesSingleProceduralPanelWithCompactButtons()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PREFAB_PATH);
            try
            {
                PauseMenuUi menuUi = root.GetComponent<PauseMenuUi>();
                Assert.That(menuUi, Is.Not.Null);

                SerializedObject serializedMenuUi = new SerializedObject(menuUi);
                GameObject menuPanel = serializedMenuUi
                    .FindProperty("menuPanel")
                    .objectReferenceValue as GameObject;

                Assert.That(menuPanel, Is.Not.Null);
                Assert.That(menuPanel.name, Is.EqualTo("MenuPanel"));
                Assert.That(menuPanel.activeSelf, Is.False);
                Assert.That(menuPanel.GetComponent<Canvas>(), Is.Null);
                Assert.That(menuPanel.GetComponent<MPImage>(), Is.Not.Null);
                Assert.That(root.transform.Find("RoundButtonCyan"), Is.Null);

                AssertCompactProceduralButton(menuPanel.transform, "ResumeButton");
                AssertCompactProceduralButton(menuPanel.transform, "OptionsButton");
                AssertCompactProceduralButton(menuPanel.transform, "ExitButton");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void AssertCompactProceduralButton(
            Transform menuPanel,
            string buttonName)
        {
            RectTransform button = menuPanel.Find(buttonName) as RectTransform;

            Assert.That(button, Is.Not.Null);
            Assert.That(button.sizeDelta, Is.EqualTo(new Vector2(340f, 68f)));
            Assert.That(button.GetComponent<MPImage>(), Is.Not.Null);
        }
    }
}
