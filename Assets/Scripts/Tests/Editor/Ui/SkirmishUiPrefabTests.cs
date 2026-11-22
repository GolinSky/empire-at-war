using System;
using System.Collections;
using System.Collections.Generic;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Entities.MainMenu.Skirmish;
using EmpireAtWar.Entities.Planet;
using EmpireAtWar.Entities.Popups;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Entities.Game;
using MPUIKIT;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Ui")]
    public sealed class SkirmishUiPrefabTests
    {
        private const string PREFAB_PATH =
            "Assets/Prefabs/Ui/MainMenu/SkirmishUi.prefab";
        private const string PALETTE_PATH = "Assets/Settings/Data/Models/Players/TeamColorPalette.asset";

        [Test]
        public void SetupOptions_ArePresentAndExplicitlyBound()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PREFAB_PATH);
            try
            {
                SkirmishUi popup = root.GetComponent<SkirmishUi>();
                Assert.That(popup, Is.Not.Null);

                SerializedObject serializedPopup = new SerializedObject(popup);
                SerializedProperty rows = serializedPopup.FindProperty("slotRows");
                Assert.That(rows.arraySize, Is.EqualTo(MatchRules.MAX_PLAYERS));
                for (int i = 0; i < rows.arraySize; i++)
                {
                    SkirmishSlotRowView row = rows.GetArrayElementAtIndex(i).objectReferenceValue as SkirmishSlotRowView;
                    Assert.That(row, Is.Not.Null, $"Slot row {i} is not bound.");
                    SerializedObject serializedRow = new SerializedObject(row);
                    foreach (string field in new[] { "titleText", "occupantDropdown", "factionDropdown", "teamDropdown", "colorButton", "colorLabel", "rowBackground" })
                    {
                        Assert.That(serializedRow.FindProperty(field).objectReferenceValue, Is.Not.Null,
                            $"Slot row {i} has no {field}.");
                    }

                    var faction = (TMP_Dropdown)serializedRow.FindProperty("factionDropdown").objectReferenceValue;
                    var checkmark = (TMP_Text)faction.template.GetComponentInChildren<Toggle>(true).graphic;
                    Assert.That(checkmark.text, Is.EqualTo("\u2713"));
                    Assert.That(checkmark.color, Is.EqualTo((Color)new Color32(246, 208, 137, 255)));
                }

                var paletteView = (SkirmishColorPaletteView)serializedPopup.FindProperty("colorPalette").objectReferenceValue;
                Assert.That(paletteView, Is.Not.Null);
                Assert.That(paletteView.IsOpen, Is.False);
                using var serializedPalette = new SerializedObject(paletteView);
                foreach (string field in new[] { "bounds", "panel", "dismissButton", "closeButton", "colorName", "selectedMarker" })
                    Assert.That(serializedPalette.FindProperty(field).objectReferenceValue, Is.Not.Null, field);
                SerializedProperty swatches = serializedPalette.FindProperty("swatches");
                Assert.That(swatches.arraySize, Is.EqualTo(MatchRules.MAX_TEAM_COLORS));
                for (int i = 0; i < swatches.arraySize; i++)
                    Assert.That(swatches.GetArrayElementAtIndex(i).objectReferenceValue, Is.Not.Null, $"Swatch {i}");
                GridLayoutGroup grid = paletteView.GetComponentInChildren<GridLayoutGroup>(true);
                Assert.That(grid.constraint, Is.EqualTo(GridLayoutGroup.Constraint.FixedColumnCount));
                Assert.That(grid.constraintCount, Is.EqualTo(4));

                foreach (string field in new[]
                         {
                             "closeButton", "startGameButton", "planetButton", "planetLabel",
                             "planetPreviewButton", "planetPreviewImage", "generationPatternImage", "patternNameText",
                             "mapSettings", "mapSizeDropdown",
                             "victoryConditionDropdown", "startingMoneySlider", "startingMoneyText", "superWeaponsToggle"
                         })
                {
                    Assert.That(serializedPopup.FindProperty(field).objectReferenceValue, Is.Not.Null,
                        $"{field} is not bound.");
                }

                Assert.That(root.transform.Find("Background/PlayersField"), Is.Not.Null);
                Assert.That(root.transform.Find("Background/BattleSettings/VictoryConditionField"), Is.Not.Null);
                Assert.That(root.transform.Find("Background/BattleSettings/MapSizeField"), Is.Not.Null);
                Assert.That(root.transform.Find("Background/BattleSettings/StartingMoneyField/StartingMoneySlider"), Is.Not.Null);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        [Test]
        public void PlanetGallery_BindsEveryPlanetOnceAndKeepsCardsInsideScrollContent()
        {
            GameObject root = PrefabUtility.LoadPrefabContents("Assets/Prefabs/Ui/Popups/PlanetSelectionPopupUi.prefab");
            try
            {
                PlanetSelectionPopupUi gallery = root.GetComponent<PlanetSelectionPopupUi>();
                Assert.That(gallery, Is.Not.Null);
                using var serializedGallery = new SerializedObject(gallery);
                foreach (string field in new[]
                         {
                             "canvasGroup", "closeButton", "searchInput", "scrollRect",
                             "countText", "currentPlanetText", "emptyState", "mapSettings",
                             "cancelButton", "selectButton", "planetPreviewImage", "generationPatternImage", "patternNameText"
                         })
                    Assert.That(serializedGallery.FindProperty(field).objectReferenceValue, Is.Not.Null, field);

                var scroll = (ScrollRect)serializedGallery.FindProperty("scrollRect").objectReferenceValue;
                Assert.That(scroll.viewport, Is.Not.Null);
                Assert.That(scroll.content, Is.Not.Null);
                SerializedProperty cards = serializedGallery.FindProperty("cards");
                Assert.That(cards.arraySize, Is.EqualTo(Enum.GetValues(typeof(PlanetType)).Length));
                var planets = new HashSet<PlanetType>();
                for (int i = 0; i < cards.arraySize; i++)
                {
                    var card = cards.GetArrayElementAtIndex(i).objectReferenceValue as PlanetSelectionCardView;
                    Assert.That(card, Is.Not.Null, $"Card {i}");
                    Assert.That(planets.Add(card.Planet), Is.True, $"Duplicate planet: {card.Planet}");
                    Assert.That(card.transform.parent, Is.EqualTo(scroll.content));
                    using var serializedCard = new SerializedObject(card);
                    foreach (string field in new[]
                             { "button", "previewImage", "nameText", "selectedIndicator", "selectionOutline" })
                        Assert.That(serializedCard.FindProperty(field).objectReferenceValue, Is.Not.Null,
                            $"{card.Planet}: {field}");
                }
                CollectionAssert.AreEquivalent(Enum.GetValues(typeof(PlanetType)), planets);
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
                SkirmishUi popup = root.GetComponent<SkirmishUi>();
                SkirmishModel model = new SkirmishModel();
                popup.SetModel(model);
                popup.AssignPresenter(new SkirmishPresenterStub(model));
                TeamColorPalette teamColors = AssetDatabase.LoadAssetAtPath<TeamColorPalette>(PALETTE_PATH);
                popup.SetData(teamColors);
                popup.Initialize();
                model.Changed += popup.Render;

                SerializedObject serializedPopup = new SerializedObject(popup);
                SerializedProperty rows = serializedPopup.FindProperty("slotRows");
                TMP_Dropdown humanOccupant = GetRowDropdown(rows, 0, "occupantDropdown");
                TMP_Dropdown thirdOccupant = GetRowDropdown(rows, 2, "occupantDropdown");
                TMP_Dropdown thirdFaction = GetRowDropdown(rows, 2, "factionDropdown");
                var thirdRow = (SkirmishSlotRowView)rows.GetArrayElementAtIndex(2).objectReferenceValue;
                using var serializedThird = new SerializedObject(thirdRow);
                var thirdEdge = (Graphic)serializedThird.FindProperty("colorEdge").objectReferenceValue;
                var thirdBackground = (MPImage)serializedThird.FindProperty("rowBackground").objectReferenceValue;
                var thirdOccupantBackground = (MPImage)thirdOccupant.targetGraphic;
                Color normalRowFill = thirdBackground.color;
                Button startGameButton = serializedPopup.FindProperty("startGameButton").objectReferenceValue as Button;

                Assert.That(humanOccupant.interactable, Is.False);
                Assert.That(thirdFaction.interactable, Is.False);
                Assert.That(thirdEdge.enabled, Is.True);
                Assert.That(thirdEdge.color, Is.EqualTo((Color)new Color32(232, 110, 108, 255)));
                Assert.That(thirdOccupantBackground.color, Is.EqualTo((Color)new Color32(91, 49, 58, 255)));
                Assert.That(startGameButton.interactable, Is.True);

                thirdOccupant.value = (int)SkirmishSlotOccupant.AiHard;

                Assert.That(model.Slots[2].Occupant, Is.EqualTo(SkirmishSlotOccupant.AiHard));
                Assert.That(thirdFaction.interactable, Is.True);
                Assert.That(thirdEdge.enabled, Is.True);
                Assert.That(thirdEdge.color, Is.EqualTo(teamColors.GetColor(model.Slots[2].ColorIndex)));
                Color normalOccupantFill = thirdOccupantBackground.color;
                Assert.That(normalOccupantFill, Is.EqualTo((Color)new Color32(52, 67, 87, 255)));

                var humanRow = (SkirmishSlotRowView)rows.GetArrayElementAtIndex(0).objectReferenceValue;
                var enemyRow = (SkirmishSlotRowView)rows.GetArrayElementAtIndex(1).objectReferenceValue;
                var palette = (SkirmishColorPaletteView)serializedPopup.FindProperty("colorPalette").objectReferenceValue;
                using var serializedPalette = new SerializedObject(palette);
                SerializedProperty swatches = serializedPalette.FindProperty("swatches");
                var red = (Button)swatches.GetArrayElementAtIndex(1).objectReferenceValue;
                var silver = (Button)swatches.GetArrayElementAtIndex(15).objectReferenceValue;
                humanRow.ColorButton.onClick.Invoke();
                Assert.That(palette.IsOpen, Is.True);
                red.onClick.Invoke();
                Assert.That(palette.IsOpen, Is.False);

                // Taking a color that another row uses swaps the two colors.
                Assert.That(model.Slots[0].ColorIndex, Is.EqualTo(1));
                Assert.That(model.Slots[1].ColorIndex, Is.EqualTo(0));
                using var serializedEnemy = new SerializedObject(enemyRow);
                var enemyColor = (TMP_Text)serializedEnemy.FindProperty("colorLabel").objectReferenceValue;
                Assert.That(enemyColor.text, Does.Contain("Blue"));

                humanRow.ColorButton.onClick.Invoke();
                Assert.That(popup.CloseColorPalette(), Is.True);
                silver.onClick.Invoke();
                Assert.That(model.Slots[0].ColorIndex, Is.EqualTo(1));
                humanRow.ColorButton.onClick.Invoke();
                silver.onClick.Invoke();
                Assert.That(model.Slots[0].ColorIndex, Is.EqualTo(15));
                using var serializedHuman = new SerializedObject(humanRow);
                var humanColor = (TMP_Text)serializedHuman.FindProperty("colorLabel").objectReferenceValue;
                Assert.That(humanColor.text, Does.Contain("Warm Silver"));

                Toggle superWeapons = (Toggle)serializedPopup.FindProperty("superWeaponsToggle").objectReferenceValue;
                superWeapons.isOn = false;
                Assert.That(model.CreateBattle().HasFeature(BattleFeatures.SuperWeapons), Is.False);

                model.BeginHostLobby(0, "Host");
                model.SelectSlotOccupant(2, SkirmishSlotOccupant.Vacant);
                Assert.That(thirdBackground.color, Is.EqualTo((Color)new Color32(22, 41, 58, 255)));
                Assert.That(thirdEdge.color, Is.EqualTo((Color)new Color32(109, 184, 226, 255)));
                Assert.That(thirdOccupantBackground.color, Is.EqualTo(normalOccupantFill));
                model.SelectSlotOccupant(2, SkirmishSlotOccupant.AiMedium);
                Assert.That(thirdBackground.color, Is.EqualTo(normalRowFill));

                model.Changed -= popup.Render;
                popup.Dispose();
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        [UnityTest]
        [Category("EditorPlayMode"), Explicit("Enters Play Mode; run with --filter-type category --filter EditorPlayMode --include-explicit true")]
        public IEnumerator OccupantPopup_RosterChangeRejectsOldChoicesAndPreservesFreshKick()
        {
            yield return new EnterPlayMode();
            var scenarios = new[]
            {
                (SkirmishSlotOccupant.Closed, false),
                (SkirmishSlotOccupant.Vacant, false),
                (SkirmishSlotOccupant.AiMedium, false),
                (SkirmishSlotOccupant.Human, false),
                (SkirmishSlotOccupant.Human, true)
            };
            foreach (var (initialOccupant, revokeEditing) in scenarios)
            {
                GameObject root = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH));
                try
                {
                    root.AddComponent<Canvas>();
                    SkirmishUi popup = root.GetComponent<SkirmishUi>();
                    using var serializedPopup = new SerializedObject(popup);
                    SerializedProperty rows = serializedPopup.FindProperty("slotRows");
                    var row = (SkirmishSlotRowView)rows.GetArrayElementAtIndex(1).objectReferenceValue;
                    TMP_Dropdown dropdown = GetRowDropdown(rows, 1, "occupantDropdown");
                    row.Initialize(1, new[] { "Republic" }, new[] { "Team 1" });
                    string[] humanOptions = { "Friend", "Kick" };
                    string[] vacantOptions = { "Closed", "AI Easy", "AI Medium", "AI Hard", "AI Ultra Hard", "Open" };
                    bool alreadyHuman = initialOccupant == SkirmishSlotOccupant.Human;
                    var slot = new SkirmishSlotSetup(initialOccupant, FactionType.Republic, 0, 0)
                    {
                        MemberId = alreadyHuman ? 1 : SkirmishModel.NO_MEMBER
                    };
                    int initialIndex = alreadyHuman ? 0
                        : initialOccupant == SkirmishSlotOccupant.Vacant ? 5 : (int)initialOccupant;
                    row.Render(slot, Color.white, "Blue", new SkirmishSlotRowState("Slot", "HUMAN",
                        alreadyHuman ? humanOptions : vacantOptions, initialIndex,
                        true, true, true, null));
                    int commands = 0;
                    int selected = -1;
                    row.OccupantChanged += (index, value) =>
                    {
                        Assert.That(index, Is.EqualTo(1));
                        commands++;
                        selected = value;
                    };
                    yield return null; // TMP_Dropdown.Start initializes the fade runner.
                    dropdown.alphaFadeSpeed = 0f;
                    dropdown.Show();
                    Assert.That(dropdown.IsExpanded, Is.True, initialOccupant.ToString());

                    slot.Occupant = SkirmishSlotOccupant.Human;
                    // Same display name/options must still invalidate a replaced member's popup.
                    slot.MemberId = revokeEditing ? 1 : 2;
                    row.Render(slot, Color.white, "Blue", new SkirmishSlotRowState("Friend", "HUMAN",
                        humanOptions, 0, !revokeEditing, true, true, null));

                    Assert.That(dropdown.interactable, Is.False, "Old popup must not reopen during its fade.");
                    Assert.That(dropdown.transition, Is.EqualTo(revokeEditing
                        ? Selectable.Transition.ColorTint : Selectable.Transition.None),
                        "Only a real permission change should show the disabled tint.");
                    dropdown.value = 1;
                    Assert.That(commands, Is.Zero, "A stale selection must not kick the newly seated player.");
                    Assert.That(dropdown.value, Is.Zero);
                    Assert.That(dropdown.captionText.text, Is.EqualTo("Friend"));

                    // Even a zero-duration TMP fade destroys its list through a deferred coroutine.
                    yield return null;
                    yield return null;
                    Assert.That(dropdown.IsExpanded, Is.False);
                    Assert.That(dropdown.interactable, Is.EqualTo(!revokeEditing));
                    Assert.That(dropdown.transition, Is.EqualTo(Selectable.Transition.ColorTint));
                    if (revokeEditing)
                    {
                        dropdown.value = 1;
                        Assert.That(commands, Is.Zero);
                    }
                    else
                    {
                        dropdown.Show();
                        Assert.That(dropdown.IsExpanded, Is.True);
                        dropdown.value = 1;
                        Assert.That(commands, Is.EqualTo(1), "A fresh explicit Kick must remain available.");
                        Assert.That(selected, Is.EqualTo(1));
                    }

                    row.Dispose();
                }
                finally
                {
                    UnityEngine.Object.Destroy(root);
                }
                yield return null;
            }
        }

        [UnityTearDown]
        public IEnumerator ExitPlayModeAfterDropdownTest()
        {
            if (Application.isPlaying) yield return new ExitPlayMode();
        }

        private static TMP_Dropdown GetRowDropdown(SerializedProperty rows, int index, string field)
        {
            SkirmishSlotRowView row = (SkirmishSlotRowView)rows.GetArrayElementAtIndex(index).objectReferenceValue;
            return (TMP_Dropdown)new SerializedObject(row).FindProperty(field).objectReferenceValue;
        }

        private sealed class SkirmishPresenterStub : ISkirmishPresenter
        {
            private readonly SkirmishModel _model;

            public SkirmishPresenterStub(SkirmishModel model)
            {
                _model = model;
            }

            public void Close() { }

            public void StartGame() { }
            public void SelectSuperWeapons(bool enabled) => _model.SelectSuperWeapons(enabled);

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
