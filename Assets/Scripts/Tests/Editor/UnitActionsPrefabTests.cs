using System;
using System.Collections.Generic;
using EmpireAtWar.Entities.UnitActions;
using EmpireAtWar.Entities.UnitActions.Ui;
using EmpireAtWar.Views.Game;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class UnitActionsPrefabTests
    {
        [Test]
        public void CoreGamePrefab_HasOneFullyWiredButtonPerAction()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/Ui/SkirmishGame/CoreGameUi.prefab");
            CoreGameUi core = prefab.GetComponent<CoreGameUi>();
            UnitActionsView view = (UnitActionsView)core.UnitActionsView;
            Assert.That(view, Is.Not.Null);
            SerializedObject data = new SerializedObject(view);
            SerializedProperty buttons = data.FindProperty("buttons");
            Assert.That(buttons.arraySize,
                Is.EqualTo(Enum.GetValues(typeof(UnitActionId)).Length));
            Assert.That(data.FindProperty("retreatCountdownText").objectReferenceValue,
                Is.Not.Null);
            HashSet<UnitActionId> seen = new HashSet<UnitActionId>();
            for (int i = 0; i < buttons.arraySize; i++)
            {
                UnitActionButton action =
                    (UnitActionButton)buttons.GetArrayElementAtIndex(i).objectReferenceValue;
                Assert.That(action, Is.Not.Null);
                Assert.That(seen.Add(action.ActionId), Is.True,
                    $"Duplicate action: {action.ActionId}");
                SerializedObject buttonData = new SerializedObject(action);
                Button button = (Button)buttonData.FindProperty("button").objectReferenceValue;
                Image icon = (Image)buttonData.FindProperty("icon").objectReferenceValue;
                Assert.That(button, Is.Not.Null);
                Assert.That(icon, Is.Not.Null);
                Assert.That(icon.sprite, Is.Not.Null);
                Assert.That(buttonData.FindProperty("pendingHighlight").objectReferenceValue,
                    Is.Not.Null);
                Assert.That(button.transition, Is.EqualTo(Selectable.Transition.ColorTint));
                Assert.That(button.spriteState.highlightedSprite, Is.Null);
            }
        }

        [Test]
        public void SceneInstaller_HasUnitOrderSettingsAssigned()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/Installers/SceneContext.prefab");
            SkirmishMainInstaller installer =
                prefab.GetComponentInChildren<SkirmishMainInstaller>(true);
            SerializedObject data = new SerializedObject(installer);
            Assert.That(data.FindProperty("unitOrderSettings").objectReferenceValue,
                Is.Not.Null);
        }
    }
}
