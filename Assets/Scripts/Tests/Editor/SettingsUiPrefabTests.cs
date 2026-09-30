using System.Collections.Generic;
using EmpireAtWar.Components.Ui.Tooltip;
using EmpireAtWar.Entities.MainMenu.Settings;
using EmpireAtWar.Ui.Base;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class SettingsUiPrefabTests
    {
        private const string PREFAB_PATH = "Assets/Prefabs/Ui/MainMenu/SettingsUi.prefab";

        private static readonly string[] VOLUME_ROWS =
            { "masterVolumeRow", "musicVolumeRow", "voiceVolumeRow", "sfxVolumeRow" };

        [Test]
        public void PrefabImplementsSettingsContract()
        {
            GameObject prefab = LoadPrefab();

            Assert.That(prefab.GetComponent<BaseUi>(), Is.InstanceOf<ISettingsUi>());
            Assert.That(prefab.GetComponent<CanvasGroup>(), Is.Not.Null);
        }

        // Replaces a runtime guard: every view and row reference must be assigned in the prefab.
        [Test]
        public void AllSettingsViewsHaveAssignedSerializedReferences()
        {
            GameObject prefab = LoadPrefab();
            foreach (MonoBehaviour component in prefab.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (component.GetType().Namespace != typeof(SettingsUi).Namespace)
                {
                    continue;
                }

                SerializedProperty property = new SerializedObject(component).GetIterator();
                while (property.NextVisible(true))
                {
                    if (property.propertyType == SerializedPropertyType.ObjectReference)
                    {
                        Assert.That(property.objectReferenceValue, Is.Not.Null,
                            $"{component.name}/{component.GetType().Name}.{property.propertyPath}");
                    }
                }
            }
        }

        [Test]
        public void VolumeRowsUseZeroToOnePercentSliders()
        {
            SerializedObject ui = new SerializedObject(LoadPrefab().GetComponent<SettingsUi>());
            foreach (string field in VOLUME_ROWS)
            {
                SerializedObject row = new SerializedObject(ui.FindProperty(field).objectReferenceValue);
                Slider slider = (Slider)row.FindProperty("slider").objectReferenceValue;

                Assert.That(row.FindProperty("showAsPercent").boolValue, Is.True, field);
                Assert.That(slider.minValue, Is.EqualTo(0f), field);
                Assert.That(slider.maxValue, Is.EqualTo(1f), field);
            }
        }

        // Keyed triggers are fixed rows; each needs its own key and must be registered with the hover view.
        [Test]
        public void KeyedTooltipTriggersAreUniqueAndRegistered()
        {
            GameObject prefab = LoadPrefab();
            SerializedProperty registered =
                new SerializedObject(prefab.GetComponentInChildren<TooltipHoverView>(true)).FindProperty("triggers");
            HashSet<Object> registeredTriggers = new HashSet<Object>();
            for (int i = 0; i < registered.arraySize; i++)
            {
                registeredTriggers.Add(registered.GetArrayElementAtIndex(i).objectReferenceValue);
            }

            HashSet<string> keys = new HashSet<string>();
            foreach (TooltipTrigger trigger in prefab.GetComponentsInChildren<TooltipTrigger>(true))
            {
                string key = new SerializedObject(trigger).FindProperty("key").stringValue;
                if (string.IsNullOrEmpty(key))
                {
                    continue;
                }

                Assert.That(keys.Add(key), Is.True, $"Tooltip key '{key}' is used twice ({trigger.name}).");
                Assert.That(registeredTriggers.Contains(trigger), Is.True,
                    $"Tooltip trigger '{key}' ({trigger.name}) is not registered with the hover view.");
            }
        }

        private static GameObject LoadPrefab()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
            Assert.That(prefab, Is.Not.Null);
            return prefab;
        }
    }
}
