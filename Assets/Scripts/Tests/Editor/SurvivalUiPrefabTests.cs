using System;
using SurvivalModel = EmpireAtWar.Entities.MainMenu.Survival.SurvivalModel;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.MainMenu.Survival;
using EmpireAtWar.Models.Factions;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class SurvivalUiPrefabTests
    {
        [Test]
        public void Setup_BindsChoicesAndToggleAndEnablesGameplay()
        {
            GameObject root = PrefabUtility.LoadPrefabContents("Assets/Prefabs/Ui/MainMenu/SurvivalUi.prefab");
            try
            {
                SurvivalUi view = root.GetComponent<SurvivalUi>();
                SurvivalModel model = new SurvivalModel();
                SurvivalRouteUiController presenter = new SurvivalRouteUiController(null, null, model, null, null);
                view.SetModel(model);
                view.AssignPresenter(presenter);
                view.Initialize();
                SerializedObject fields = new SerializedObject(view);
                foreach (string field in new[] { "closeButton", "startButton", "playerFaction", "allyFaction",
                             "enemyFaction", "mapSize", "difficulty", "superWeapons" })
                    Assert.That(fields.FindProperty(field).objectReferenceValue, Is.Not.Null, field);
                Assert.That(((Button)fields.FindProperty("startButton").objectReferenceValue).interactable, Is.True);
                TMP_Dropdown allies = (TMP_Dropdown)fields.FindProperty("allyFaction").objectReferenceValue;
                Assert.That(allies.options.Count, Is.EqualTo(Enum.GetValues(typeof(FactionType)).Length + 1));
                allies.value = 1;
                Assert.That(model.AllyFaction.HasValue, Is.True);
                Toggle toggle = (Toggle)fields.FindProperty("superWeapons").objectReferenceValue;
                toggle.isOn = false;
                Assert.That(model.CreateBattle().HasFeature(BattleFeatures.SuperWeapons), Is.False);
                view.Dispose();
                toggle.isOn = true;
                Assert.That(model.SuperWeapons, Is.False, "Disposal removes the input listener.");
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
    }
}
