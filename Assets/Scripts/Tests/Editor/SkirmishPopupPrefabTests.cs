using EmpireAtWar.Models.Factions;
using EmpireAtWar.Ui.Popups;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class SkirmishPopupPrefabTests
    {
        private const string PREFAB_PATH =
            "Assets/Prefabs/Ui/Popups/SkirmishGameSetUpPopupUi.prefab";

        [Test]
        public void SetupOptions_ArePresentAndExplicitlyBound()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PREFAB_PATH);
            try
            {
                SkirmishPopupUi popup = root.GetComponent<SkirmishPopupUi>();
                Assert.That(popup, Is.Not.Null);

                SerializedObject serializedPopup = new SerializedObject(popup);
                TMP_Dropdown playerFactionDropdown =
                    serializedPopup.FindProperty("playerFactionDropdown").objectReferenceValue
                        as TMP_Dropdown;
                TMP_Dropdown enemyFactionDropdown =
                    serializedPopup.FindProperty("enemyFactionDropdown").objectReferenceValue
                        as TMP_Dropdown;
                Assert.That(playerFactionDropdown, Is.Not.Null);
                Assert.That(enemyFactionDropdown, Is.Not.Null);
                Assert.That(
                    serializedPopup.FindProperty("victoryConditionDropdown").objectReferenceValue,
                    Is.Not.Null);
                Assert.That(
                    serializedPopup.FindProperty("enemyDifficultyDropdown").objectReferenceValue,
                    Is.Not.Null);
                Assert.That(
                    serializedPopup.FindProperty("startingMoneySlider").objectReferenceValue,
                    Is.Not.Null);
                Assert.That(
                    serializedPopup.FindProperty("startingMoneyText").objectReferenceValue,
                    Is.Not.Null);

                Assert.That(root.transform.Find("Background/VictoryConditionField"), Is.Not.Null);
                Assert.That(root.transform.Find("Background/EnemyDifficultyField"), Is.Not.Null);
                Assert.That(root.transform.Find("Background/StartingMoneyField"), Is.Not.Null);
                Assert.That(root.transform.Find("Background/StartingMoneyField/StartingMoneySlider"), Is.Not.Null);

                popup.Initialize();
                Assert.That(
                    playerFactionDropdown.value,
                    Is.EqualTo((int)FactionType.Republic));
                Assert.That(
                    enemyFactionDropdown.value,
                    Is.EqualTo((int)FactionType.Separatist));

                playerFactionDropdown.value = (int)FactionType.Separatist;
                Assert.That(
                    enemyFactionDropdown.value,
                    Is.EqualTo((int)FactionType.Republic));

                enemyFactionDropdown.value = (int)FactionType.Separatist;
                Assert.That(
                    playerFactionDropdown.value,
                    Is.EqualTo((int)FactionType.Republic));

                popup.LateDispose();
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
