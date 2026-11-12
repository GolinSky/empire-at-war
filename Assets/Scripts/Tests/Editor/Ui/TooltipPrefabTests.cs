using System.Linq;
using EmpireAtWar.Entities.Tooltip;
using EmpireAtWar.Services.Tooltip;
using EmpireAtWar.Ui.Base;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Ui")]
    public sealed class TooltipPrefabTests
    {
        private const string PREFAB_PATH = "Assets/Prefabs/Ui/Tooltip/TooltipUi.prefab";
        private const string MATCHUP_ROW_PATH = "Assets/Prefabs/Ui/Tooltip/TooltipMatchupRow.prefab";
        private const string ICON_DATA_PATH = "Assets/Settings/Data/Tooltip/TooltipIconData.asset";

        [Test]
        public void PrefabHasConcreteTooltipViewAndCanvasGroup()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
            Assert.That(prefab, Is.Not.Null);
            Assert.That(prefab.GetComponent<BaseUi>(), Is.InstanceOf<TooltipUi>());
            Assert.That(prefab.GetComponent<CanvasGroup>(), Is.Not.Null);
        }

        [Test]
        public void PanelAndReusableRowsHaveNoRaycastTargets()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/Ui/Tooltip" }))
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                foreach (Graphic graphic in prefab.GetComponentsInChildren<Graphic>(true))
                    Assert.That(graphic.raycastTarget, Is.False, prefab.name + "/" + graphic.name);
            }
        }

        [Test]
        public void MatchupIconsFitRowSlotsAndResolve()
        {
            var icons = AssetDatabase.LoadAssetAtPath<TooltipIconData>(ICON_DATA_PATH);
            int slots = new SerializedObject(AssetDatabase.LoadAssetAtPath<TooltipMatchupRowView>(MATCHUP_ROW_PATH))
                .FindProperty("icons").arraySize;
            foreach (string guid in AssetDatabase.FindAssets("t:" + nameof(UnitMatchupData)))
            {
                var data = AssetDatabase.LoadAssetAtPath<UnitMatchupData>(AssetDatabase.GUIDToAssetPath(guid));
                foreach (TooltipIcon entry in data.StrongAgainst.Concat(data.WeakAgainst))
                {
                    Assert.That(entry.IconKeys.Count, Is.InRange(1, slots), data.name + "/" + entry.Label);
                    foreach (string key in entry.IconKeys)
                        Assert.That(icons.Resolve(key), Is.Not.Null, data.name + "/" + entry.Label);
                }
            }
        }

        [Test]
        public void AllTooltipWidgetsHaveAssignedSerializedReferences()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/Ui/Tooltip" }))
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                foreach (MonoBehaviour component in prefab.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (component.GetType().Namespace != typeof(TooltipUi).Namespace) continue;
                    var serialized = new SerializedObject(component);
                    SerializedProperty property = serialized.GetIterator();
                    while (property.NextVisible(true))
                        if (property.propertyType == SerializedPropertyType.ObjectReference)
                            Assert.That(property.objectReferenceValue, Is.Not.Null,
                                component.GetType().Name + "." + property.propertyPath);
                }
            }
        }
    }
}
