using System;
using System.Reflection;
using EmpireAtWar.Entities.EndGameUi;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.Planet;
using EmpireAtWar.Models.Factions;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Ui")]
    public sealed class EndGameUiLifecycleTests
    {
        [TestCase(BattleOutcome.PlayerVictory, "VICTORY")]
        [TestCase(BattleOutcome.EnemyVictory, "DEFEAT")]
        public void ShowResult_RendersBattleDetailsAndEachSidesLosses(BattleOutcome outcome, string title)
        {
            EndGameUi prefab = AssetDatabase.LoadAssetAtPath<EndGameUi>(
                "Assets/Prefabs/Ui/SkirmishGame/EndGameUi.prefab");
            EndGameUi ui = UnityEngine.Object.Instantiate(prefab);
            Sprite icon = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.zero);
            try
            {
                var result = new BattleResult(Array.Empty<FactionType>(), outcome,
                    BattleVictoryCondition.DestroyEnemyFleet, PlanetType.MonCalamari, FactionType.Republic,
                    0, 0, false, false, durationSeconds: 1013);
                ui.ShowResult(result, new[] { ("Venator", icon, 2) }, Array.Empty<(string, Sprite, int)>());

                var serialized = new SerializedObject(ui);
                Assert.That(((TMP_Text)serialized.FindProperty("outcomeText").objectReferenceValue).text,
                    Is.EqualTo(title));
                Assert.That(((TMP_Text)serialized.FindProperty("battlefieldText").objectReferenceValue).text,
                    Is.EqualTo("MON CALAMARI"));
                Assert.That(((TMP_Text)serialized.FindProperty("battleDurationText").objectReferenceValue).text,
                    Does.Contain("0:16:53"));

                var friendly = (ScrollRect)serialized.FindProperty("playerLosses").objectReferenceValue;
                var hostile = (ScrollRect)serialized.FindProperty("enemyLosses").objectReferenceValue;
                var row = friendly.content.GetChild(0);
                Assert.That(row.Find("UnitName").GetComponent<TMP_Text>().text, Is.EqualTo("Venator"));
                Assert.That(row.Find("UnitCount").GetComponent<TMP_Text>().text, Is.EqualTo("×2"));
                Assert.That(row.Find("UnitIcon").GetComponent<Image>().sprite, Is.SameAs(icon));
                Assert.That(hostile.content.GetChild(0).Find("UnitName").GetComponent<TMP_Text>().text,
                    Is.EqualTo("No units lost"));
                Assert.That(hostile.content.GetChild(0).Find("UnitIcon").GetComponent<Image>().enabled, Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(ui.gameObject);
                UnityEngine.Object.DestroyImmediate(icon);
            }
        }

        [Test]
        public void ReturnButton_ForwardsOnceAfterSetup_AndStopsAfterDisposal()
        {
            EndGameUi prefab = AssetDatabase.LoadAssetAtPath<EndGameUi>(
                "Assets/Prefabs/Ui/SkirmishGame/EndGameUi.prefab");
            Assert.That(prefab, Is.Not.Null);
            EndGameUi ui = UnityEngine.Object.Instantiate(prefab);
            try
            {
                Button button = (Button)typeof(EndGameUi).GetField("returnToMenuButton",
                    BindingFlags.Instance | BindingFlags.NonPublic).GetValue(ui);
                Presenter presenter = new Presenter();
                button.onClick.Invoke();
                Assert.That(presenter.ReturnCount, Is.Zero);

                ui.AssignPresenter(presenter);
                Assert.Throws<InvalidOperationException>(() => ui.AssignPresenter(presenter));
                ui.Initialize();
                button.onClick.Invoke();
                Assert.That(presenter.ReturnCount, Is.EqualTo(1));

                ui.Dispose();
                ui.Dispose();
                button.onClick.Invoke();
                Assert.That(presenter.ReturnCount, Is.EqualTo(1));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(ui.gameObject);
            }
        }

        private sealed class Presenter : IEndGamePresenter
        {
            public int ReturnCount { get; private set; }
            public void ReturnToMenu() => ReturnCount++;
        }
    }
}
