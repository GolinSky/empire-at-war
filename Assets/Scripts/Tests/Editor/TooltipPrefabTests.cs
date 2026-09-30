using EmpireAtWar.Entities.Tooltip;
using EmpireAtWar.Ui.Base;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class TooltipPrefabTests
    {
        private const string PREFAB_PATH = "Assets/Prefabs/Ui/Tooltip/TooltipUi.prefab";

        [Test]
        public void PrefabImplementsTooltipContractAndHasCanvasGroup()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
            Assert.That(prefab, Is.Not.Null);
            Assert.That(prefab.GetComponent<BaseUi>(), Is.InstanceOf<ITooltipUi>());
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
