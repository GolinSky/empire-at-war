using EmpireAtWar.Models.Factions;
using EmpireAtWar.Entities.MenuUi.Popups;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Ui.Popups;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class SkirmishPopupPrefabTests
    {
        private const string PREFAB_PATH =
            "Assets/Prefabs/Ui/Popups/SkirmishGameSetUpPopupUi.prefab";
        private const string PALETTE_PATH = "Assets/Settings/Data/Models/Players/TeamColorPalette.asset";

        [Test]
        public void SetupOptions_ArePresentAndExplicitlyBound()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PREFAB_PATH);
            try
            {
                SkirmishPopupUi popup = root.GetComponent<SkirmishPopupUi>();
                Assert.That(popup, Is.Not.Null);

                SerializedObject serializedPopup = new SerializedObject(popup);
                SerializedProperty rows = serializedPopup.FindProperty("slotRows");
                Assert.That(rows.arraySize, Is.EqualTo(MatchRules.MAX_PLAYERS));
                for (int i = 0; i < rows.arraySize; i++)
                {
                    SkirmishSlotRowView row = rows.GetArrayElementAtIndex(i).objectReferenceValue as SkirmishSlotRowView;
                    Assert.That(row, Is.Not.Null, $"Slot row {i} is not bound.");
                    SerializedObject serializedRow = new SerializedObject(row);
                    foreach (string field in new[] { "titleText", "occupantDropdown", "factionDropdown", "teamDropdown", "colorDropdown" })
                    {
                        Assert.That(serializedRow.FindProperty(field).objectReferenceValue, Is.Not.Null,
                            $"Slot row {i} has no {field}.");
                    }
                }

                foreach (string field in new[]
                         {
                             "closeButton", "startGameButton", "planetsDropdown", "mapSizeDropdown",
                             "victoryConditionDropdown", "startingMoneySlider", "startingMoneyText"
                         })
                {
                    Assert.That(serializedPopup.FindProperty(field).objectReferenceValue, Is.Not.Null,
                        $"{field} is not bound.");
                }

                Assert.That(root.transform.Find("Background/PlayersField"), Is.Not.Null);
                Assert.That(root.transform.Find("Background/VictoryConditionField"), Is.Not.Null);
                Assert.That(root.transform.Find("Background/MapSizeField"), Is.Not.Null);
                Assert.That(root.transform.Find("Background/StartingMoneyField/StartingMoneySlider"), Is.Not.Null);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        [Test]
        public void Initialize_DefaultDuel_OpensHumanAndOneAiRow()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PREFAB_PATH);
            try
            {
                SkirmishPopupUi popup = root.GetComponent<SkirmishPopupUi>();
                SkirmishPopupModel model = new SkirmishPopupModel();
                popup.SetModel(model);
                popup.SetPresenter(new SkirmishPopupPresenterStub(model));
                popup.SetData(AssetDatabase.LoadAssetAtPath<TeamColorPalette>(PALETTE_PATH));
                popup.Initialize();

                SerializedObject serializedPopup = new SerializedObject(popup);
                SerializedProperty rows = serializedPopup.FindProperty("slotRows");
                TMP_Dropdown humanOccupant = GetRowDropdown(rows, 0, "occupantDropdown");
                TMP_Dropdown thirdOccupant = GetRowDropdown(rows, 2, "occupantDropdown");
                TMP_Dropdown thirdFaction = GetRowDropdown(rows, 2, "factionDropdown");
                Button startGameButton = serializedPopup.FindProperty("startGameButton").objectReferenceValue as Button;

                Assert.That(humanOccupant.interactable, Is.False);
                Assert.That(thirdFaction.interactable, Is.False);
                Assert.That(startGameButton.interactable, Is.True);

                thirdOccupant.value = (int)SkirmishSlotOccupant.AiHard;

                Assert.That(model.Slots[2].Occupant, Is.EqualTo(SkirmishSlotOccupant.AiHard));
                Assert.That(thirdFaction.interactable, Is.True);

                TMP_Dropdown humanColor = GetRowDropdown(rows, 0, "colorDropdown");
                TMP_Dropdown enemyColor = GetRowDropdown(rows, 1, "colorDropdown");
                humanColor.value = enemyColor.value;

                // Taking a color that another row uses swaps the two colors.
                Assert.That(model.Slots[0].ColorIndex, Is.EqualTo(1));
                Assert.That(model.Slots[1].ColorIndex, Is.EqualTo(0));
                Assert.That(enemyColor.value, Is.EqualTo(0));

                popup.Dispose();
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static TMP_Dropdown GetRowDropdown(SerializedProperty rows, int index, string field)
        {
            SkirmishSlotRowView row = (SkirmishSlotRowView)rows.GetArrayElementAtIndex(index).objectReferenceValue;
            return (TMP_Dropdown)new SerializedObject(row).FindProperty(field).objectReferenceValue;
        }

        private sealed class SkirmishPopupPresenterStub : ISkirmishPopupPresenter
        {
            private readonly SkirmishPopupModel _model;

            public SkirmishPopupPresenterStub(SkirmishPopupModel model)
            {
                _model = model;
            }

            public void CloseSkirmish() { }
            public void StartGame() { }
            public void SelectSlotOccupant(int slotIndex, int occupantIndex) =>
                _model.SelectSlotOccupant(slotIndex, (SkirmishSlotOccupant)occupantIndex);
            public void SelectSlotFaction(int slotIndex, int factionIndex) =>
                _model.SelectSlotFaction(slotIndex, (FactionType)factionIndex);
            public void SelectSlotTeam(int slotIndex, int teamIndex) =>
                _model.SelectSlotTeam(slotIndex, teamIndex);
            public void SelectSlotColor(int slotIndex, int colorIndex) =>
                _model.SelectSlotColor(slotIndex, colorIndex);
            public void SelectPlanet(int index) { }
            public void SelectMapSize(int index) { }
            public void SelectVictoryCondition(int index) { }
            public void SelectStartingMoney(float amount) =>
                _model.SelectStartingMoney(amount);
        }
    }
}
