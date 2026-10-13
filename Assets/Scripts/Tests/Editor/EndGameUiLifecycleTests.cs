using System;
using System.Reflection;
using EmpireAtWar.Entities.EndGameUi;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class EndGameUiLifecycleTests
    {
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
